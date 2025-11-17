using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // DbContext (Transaction)
using Sattim.Web.Models.Analytical;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Escrow; // EscrowStatus için
using Sattim.Web.Models.User; // UserProfile için eklendi
using Sattim.Web.Services.Notification;
using Sattim.Web.Services.Repositories; // Gerekli tüm Repolar
using Sattim.Web.ViewModels.Dispute; // Arayüzün istediği DTO'lar
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Arayüzünüzle aynı namespace'i kullanır
namespace Sattim.Web.Services.Dispute
{
    public class DisputeService : IDisputeService
    {
        // Gerekli Özel Repo
        private readonly IDisputeRepository _disputeRepo;
        // Gerekli Jenerik Repolar
        private readonly IGenericRepository<DisputeMessage> _messageRepo;
        private readonly IGenericRepository<Escrow> _escrowRepo;

        // DİKKAT: UserProfile'ı (Bio vb.) almak için IGenericRepository<UserProfile>
        // kullanmıyoruz, çünkü profil resmi ApplicationUser'dadır.
        // Doğrudan DbContext üzerinden sorgulayacağız.

        // Diğer Servisler
        private readonly ApplicationDbContext _context; // Transaction yönetimi ve özel sorgular için
        private readonly IMapper _mapper;
        private readonly ILogger<DisputeService> _logger;
        private readonly INotificationService _notificationService; // (Bildirim göndermek için)

        public DisputeService(
            IDisputeRepository disputeRepo,
            IGenericRepository<DisputeMessage> messageRepo,
            IGenericRepository<Escrow> escrowRepo,
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<DisputeService> logger,
            INotificationService notificationService
            )
        {
            _disputeRepo = disputeRepo;
            _messageRepo = messageRepo;
            _escrowRepo = escrowRepo;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
        }

        // ====================================================================
        //  QUERIES (Okuma İşlemleri)
        // ====================================================================

        public async Task<List<DisputeSummaryViewModel>> GetMyDisputesAsync(string userId)
        {
            // 1. Özel Repo'dan veriyi al
            var disputes = await _disputeRepo.GetDisputesForUserAsync(userId);

            // 2. DTO'ya dönüştür
            var viewModels = _mapper.Map<List<DisputeSummaryViewModel>>(disputes);

            // 3. (Manuel) Rolü ata: Bu ihtilafta ben "Alıcı" mıyım, "Satıcı" mı?
            foreach (var vm in viewModels)
            {
                var originalDispute = disputes.First(d => d.Id == vm.DisputeId);
                vm.RoleInDispute = (originalDispute.Product.Escrow.BuyerId == userId) ? "Alıcı" : "Satıcı";
            }

            return viewModels;
        }

        public async Task<DisputeDetailViewModel> GetMyDisputeDetailsAsync(int disputeId, string userId)
        {
            // 1. Repo'dan veriyi al (Artık Profile'ı İÇERMİYOR)
            var dispute = await _disputeRepo.GetDisputeWithDetailsAsync(disputeId);

            if (dispute == null)
                throw new KeyNotFoundException("İhtilaf bulunamadı.");

            // 2. Güvenlik Kontrolü: Bu kişi Alıcı veya Satıcı mı?
            if (dispute.Product.Escrow.BuyerId != userId && dispute.Product.Escrow.SellerId != userId)
            {
                _logger.LogWarning($"Yetkisiz ihtilaf detayı erişim denemesi. Kullanıcı: {userId}, İhtilaf: {disputeId}");
                throw new UnauthorizedAccessException("Bu ihtilafı görüntüleme yetkiniz yok.");
            }

            // 3. AutoMapper ile DTO'ya dönüştür
            // (DisputeProfile, SenderProfileImageUrl OLMADAN eşleme yaptı)
            var viewModel = _mapper.Map<DisputeDetailViewModel>(dispute);

            // 4. DÜZELTME: Profil resimlerini MANUEL OLARAK al

            // 4a. Mesaj gönderen tüm kullanıcıların ID'lerini topla (DISTINCT)
            var senderIds = viewModel.Messages.Select(m => m.SenderId).Distinct().ToList();

            if (senderIds.Any())
            {
                // 4b. Tek bir veritabanı sorgusu ile tüm kullanıcıların
                //     ID ve ProfileImageUrl'larını çek (performanslı)
                var userImageDictionary = (await _context.Users
                                        .Where(u => senderIds.Contains(u.Id))
                                        .Select(u => new { u.Id, u.ProfileImageUrl })
                                        .ToListAsync())
                                        .ToDictionary(u => u.Id, u => u.ProfileImageUrl);

                // 5. Profil resimlerini ViewModel'a ata
                foreach (var messageVM in viewModel.Messages)
                {
                    if (userImageDictionary.TryGetValue(messageVM.SenderId, out var imageUrl))
                    {
                        messageVM.SenderProfileImageUrl = imageUrl;
                    }
                }
            }

            return viewModel;
        }

        // ====================================================================
        //  COMMANDS (Yazma İşlemleri)
        // ====================================================================

        /// <summary>
        /// Bu, sisteminizdeki en karmaşık Transactional metotlardan biridir.
        /// Dispute, DisputeMessage ve Escrow varlıklarını aynı anda değiştirir.
        /// </summary>
        public async Task<(bool Success, int? DisputeId, string ErrorMessage)> OpenDisputeAsync(OpenDisputeViewModel model, string buyerId)
        {
            // 1. Transaction'ı Başlat
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"İhtilaf açma işlemi (Transaction) başlatıldı: Alıcı: {buyerId}, Ürün: {model.ProductId}");

            try
            {
                // 2. Varlıkları Al (Takip et - 'AsNoTracking' KULLANMA)
                // Escrow'un PK'si ProductId'dir, bu yüzden GetByIdAsync kullanabiliriz.
                var escrow = await _escrowRepo.GetByIdAsync(model.ProductId);

                // 3. İş Kurallarını (Service Layer) Kontrol Et
                if (escrow == null)
                    return (false, null, "Bu ihtilafın ilgili olduğu sipariş (Escrow) kaydı bulunamadı.");

                // Güvenlik: İhtilafı sadece ALICI açabilir
                if (escrow.BuyerId != buyerId)
                    return (false, null, "Sadece kendi siparişiniz için ihtilaf açabilirsiniz.");

                // Statü: Sadece 'Ödendi' veya 'Kargolandı' durumundaysa...
                if (escrow.Status != EscrowStatus.Funded && escrow.Status != EscrowStatus.Shipped)
                    return (false, null, "Sadece 'Ödendi' veya 'Kargolandı' durumundaki siparişler için ihtilaf açılabilir.");

                // Kural: Bu ürün için zaten 'Open' veya 'UnderReview' bir ihtilaf var mı?
                if (await _disputeRepo.AnyAsync(d => d.ProductId == model.ProductId &&
                    (d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview)))
                {
                    return (false, null, "Bu ürün için zaten açık bir ihtilaf başvurunuz bulunuyor.");
                }

                // 4. Yeni Varlık (Dispute) Oluştur (Model Constructor ile)
                var dispute = new Models.Dispute.Dispute(
                    productId: model.ProductId,
                    initiatorId: buyerId,
                    reason: model.Reason,
                    description: model.Description // (Bu açıklama ilk mesaj olarak da kullanılacak)
                );

                await _disputeRepo.AddAsync(dispute);

                // 5. ÖNEMLİ: Dispute ID'sini almak için ÖNCE kaydet
                await _context.SaveChangesAsync();

                // 6. Yeni Varlık (İlk Mesaj) Oluştur
                var message = new DisputeMessage(
                    disputeId: dispute.Id,
                    senderId: buyerId,
                    message: model.Description // İlk mesaj, ana açıklamadır
                );
                await _messageRepo.AddAsync(message);

                // 7. Varlığı Güncelle (Escrow) (Model Metodu ile)
                escrow.OpenDispute(model.Description);
                _escrowRepo.Update(escrow);

                // 8. Tüm değişiklikleri (DisputeMessage, Escrow Update) kaydet
                await _context.SaveChangesAsync();

                // 9. Transaction'ı Onayla
                await transaction.CommitAsync();

                _logger.LogInformation($"İhtilaf BAŞARILI (Commit). Alıcı: {buyerId}, İhtilaf ID: {dispute.Id}");

                // 10. Bildirimleri Gönder (Transaction DIŞINDA)
                // (Satıcıyı ve Adminleri bilgilendir)
                //await _notificationService.NotifyDisputeOpened(dispute);

                return (true, dispute.Id, null); // Başarılı
            }
            catch (Exception ex)
            {
                // (Modelin 'OpenDispute' metodundan fırlatılan
                // 'InvalidOperationException' veya 'ArgumentException' dahil)
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"İhtilaf açma sırasında KRİTİK HATA (Rollback). Alıcı: {buyerId}, Ürün: {model.ProductId}");
                return (false, null, "İhtilaf açılırken beklenmedik bir sistem hatası oluştu.");
            }
        }


        public async Task<(bool Success, string ErrorMessage)> AddDisputeMessageAsync(AddDisputeMessageViewModel model, string userId)
        {
            try
            {
                // 1. Varlığı Al (Güvenlik kontrolü için Escrow'u da içeren özel repo metodu)
                var dispute = await _disputeRepo.GetDisputeWithDetailsAsync(model.DisputeId);

                if (dispute == null)
                    return (false, "Mesaj göndermek istediğiniz ihtilaf bulunamadı.");

                // 2. Güvenlik Kontrolü: Bu kişi Alıcı veya Satıcı mı?
                if (dispute.Product.Escrow.BuyerId != userId && dispute.Product.Escrow.SellerId != userId)
                    return (false, "Bu ihtilafa mesaj gönderme yetkiniz yok.");

                // 3. Statü Kontrolü
                if (dispute.Status == DisputeStatus.Closed || dispute.Status == DisputeStatus.Resolved)
                    return (false, "Bu ihtilaf zaten kapatılmış/çözülmüş.");

                // 4. Yeni Varlık Oluştur (Model Constructor ile)
                var message = new DisputeMessage(
                    disputeId: model.DisputeId,
                    senderId: userId,
                    message: model.Message
                );

                // 5. Veritabanına Ekle
                await _messageRepo.AddAsync(message);
                await _messageRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"İhtilafa yeni mesaj eklendi (MessageID: {message.Id}, DisputeID: {dispute.Id})");

                // 6. Bildirim Gönder (Karşı tarafa)
                // string receiverId = (dispute.Product.Escrow.BuyerId == userId) 
                //     ? dispute.Product.Escrow.SellerId 
                //     : dispute.Product.Escrow.BuyerId;
                // await _notificationService.NotifyDisputeMessage(dispute, message, receiverId);

                return (true, null); // Başarılı
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"İhtilafa mesaj eklenirken kritik hata (DisputeID: {model.DisputeId}, UserID: {userId})");
                return (false, "Mesajınız gönderilirken beklenmedik bir sistem hatası oluştu.");
            }
        }
    }
}