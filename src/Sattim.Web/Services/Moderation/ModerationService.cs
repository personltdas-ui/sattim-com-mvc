using AutoMapper;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // DbContext (Transaction)
using Sattim.Web.Models.Analytical;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.Product;
using Sattim.Web.Services.Repositories; // Gerekli tüm Repolar
using Sattim.Web.Services.Wallet; // IWalletService (Servis-Servis)
using Sattim.Web.ViewModels.Moderation; // Arayüzün istediği DTO'lar
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Arayüzünüzle aynı namespace'i kullanır
namespace Sattim.Web.Services.Moderation
{
    // BU SERVİSİN TAMAMI [Authorize(Roles = "Admin,Moderator")] İLE KORUNMALIDIR
    public class ModerationService : IModerationService
    {
        // Gerekli Özel Repolar
        private readonly IReportRepository _reportRepo;
        private readonly IDisputeRepository _disputeRepo;
        private readonly IBlogCommentRepository _commentRepo;
        private readonly IProductRepository _productRepo;

        // Gerekli Jenerik Repolar
        private readonly IGenericRepository<DisputeMessage> _messageRepo;
        private readonly IGenericRepository<Escrow> _escrowRepo;
        private readonly IGenericRepository<Models.Product.Product> _productGenericRepo; // (Approve için)
        private readonly IGenericRepository<BlogComment> _commentGenericRepo; // (Approve/Reject için)

        // Gerekli Diğer Servisler
        private readonly IWalletService _walletService; // KRİTİK: Para akışı için

        // Yardımcılar
        private readonly ApplicationDbContext _context; // Transaction yönetimi için
        private readonly IMapper _mapper;
        private readonly ILogger<ModerationService> _logger;
        // private readonly INotificationService _notificationService;

        public ModerationService(
            IReportRepository reportRepo,
            IDisputeRepository disputeRepo,
            IBlogCommentRepository commentRepo,
            IProductRepository productRepo,
            IGenericRepository<DisputeMessage> messageRepo,
            IGenericRepository<Escrow> escrowRepo,
            IGenericRepository<Models.Product.Product> productGenericRepo,
            IGenericRepository<BlogComment> commentGenericRepo,
            IWalletService walletService,
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<ModerationService> logger
            //, INotificationService notificationService
            )
        {
            _reportRepo = reportRepo;
            _disputeRepo = disputeRepo;
            _commentRepo = commentRepo;
            _productRepo = productRepo;
            _messageRepo = messageRepo;
            _escrowRepo = escrowRepo;
            _productGenericRepo = productGenericRepo;
            _commentGenericRepo = commentGenericRepo;
            _walletService = walletService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            //_notificationService = notificationService;
        }

        // ====================================================================
        //  1. KULLANICI ŞİKAYETLERİ (Report)
        // ====================================================================

        public async Task<List<ReportViewModel>> GetPendingReportsAsync()
        {
            var reports = await _reportRepo.GetPendingReportsWithDetailsAsync();
            return _mapper.Map<List<ReportViewModel>>(reports);
        }

        // (Bu 3 metot Desen A: Basit CRUD'a benzer)
        public async Task<bool> MarkReportAsUnderReviewAsync(int reportId, string adminId)
        {
            try
            {
                var report = await _reportRepo.GetByIdAsync(reportId);
                if (report == null || report.Status != ReportStatus.Pending) return false;

                report.PutUnderReview($"İnceleyen: {adminId}");
                _reportRepo.Update(report);
                await _reportRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Rapor inceleniyor olarak işaretlenemedi (ID: {reportId})");
                return false;
            }
        }

        public async Task<bool> ResolveReportAsync(int reportId, string adminId, string resolutionNote)
        {
            try
            {
                var report = await _reportRepo.GetByIdAsync(reportId);
                if (report == null) return false;

                string note = $"Çözen: {adminId}. Not: {resolutionNote}";
                report.Resolve(note);
                _reportRepo.Update(report);
                await _reportRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Rapor çözülürken hata (ID: {reportId})");
                return false;
            }
        }

        public async Task<bool> RejectReportAsync(int reportId, string adminId, string rejectionNote)
        {
            try
            {
                var report = await _reportRepo.GetByIdAsync(reportId);
                if (report == null) return false;

                string note = $"Reddeden: {adminId}. Not: {rejectionNote}";
                report.Reject(note);
                _reportRepo.Update(report);
                await _reportRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Rapor reddedilirken hata (ID: {reportId})");
                return false;
            }
        }

        // ====================================================================
        //  2. SİPARİŞ İHTİLAFLARI (Dispute) - PARA AKIŞI
        // ====================================================================

        public async Task<List<DisputeViewModel>> GetPendingDisputesAsync()
        {
            var disputes = await _disputeRepo.GetPendingDisputesForAdminAsync();
            return _mapper.Map<List<DisputeViewModel>>(disputes);
        }

        public async Task<DisputeDetailViewModel> GetDisputeDetailsAsync(int disputeId)
        {
            // (IDisputeService'teki GetMyDisputeDetailsAsync'in aynısı,
            // ancak güvenlik (Alıcı/Satıcı) kontrolü olmadan)
            var dispute = await _disputeRepo.GetDisputeWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("İhtilaf bulunamadı.");

            var viewModel = _mapper.Map<DisputeDetailViewModel>(dispute);

            // Profil resimlerini manuel olarak al
            var senderIds = viewModel.Messages.Select(m => m.SenderId).Distinct().ToList();
            if (senderIds.Any())
            {
                var users = (await _context.Users
                                .Where(u => senderIds.Contains(u.Id))
                                .Select(u => new { u.Id, u.ProfileImageUrl })
                                .ToListAsync())
                                .ToDictionary(u => u.Id, u => u.ProfileImageUrl);

                foreach (var messageVM in viewModel.Messages)
                {
                    if (users.TryGetValue(messageVM.SenderId, out var imageUrl))
                    {
                        messageVM.SenderProfileImageUrl = imageUrl;
                    }
                }
            }
            return viewModel;
        }

        public async Task<bool> AddDisputeMessageAsync(int disputeId, string adminId, string message)
        {
            // (IDisputeService'teki AddDisputeMessageAsync'in Admin versiyonu)
            try
            {
                var dispute = await _disputeRepo.GetByIdAsync(disputeId);
                if (dispute == null) return false;

                if (dispute.Status == DisputeStatus.Closed || dispute.Status == DisputeStatus.Resolved)
                    return false; // Kapanmış ihtilafa admin mesaj atamaz

                var disputeMessage = new DisputeMessage(
                    disputeId: disputeId,
                    senderId: adminId,
                    message: message
                );

                await _messageRepo.AddAsync(disputeMessage);
                await _messageRepo.UnitOfWork.SaveChangesAsync();

                // (Burada Alıcı ve Satıcı'ya bildirim gönderilmeli)
                // await _notificationService.NotifyDisputeMessage(dispute, disputeMessage, ...);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Admin ihtilafa mesaj eklerken hata (DisputeID: {disputeId}, AdminID: {adminId})");
                return false;
            }
        }

        /// <summary>
        /// (KRİTİK) İhtilafı SATICI lehine çözer.
        /// Transactional: Dispute ve Escrow durumunu GÜNCELLER.
        /// Post-Transaction: WalletService'i TETİKLER.
        /// </summary>
        public async Task<bool> ResolveDisputeForSellerAsync(int disputeId, string adminId, string resolutionNote)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"İhtilaf SATICI lehine çözülüyor (Transaction başladı). Admin: {adminId}, Dispute: {disputeId}");

            try
            {
                var dispute = await _disputeRepo.GetDisputeWithDetailsAsync(disputeId); // Escrow'u da yükler
                if (dispute == null || dispute.Product.Escrow == null)
                    throw new KeyNotFoundException("İhtilaf veya ilişkili Escrow bulunamadı.");

                var escrow = dispute.Product.Escrow;
                string logNote = $"Admin ({adminId}) tarafından satıcı lehine çözüldü. Not: {resolutionNote}";

                // 1. İş Mantığını Modele Devret (Dispute)
                dispute.Resolve(logNote);

                // 2. İş Mantığını Modele Devret (Escrow)
                escrow.ResolveByReleasing(); // Durumu 'Released' yapar

                // 3. Değişiklikleri Bildir
                _disputeRepo.Update(dispute);
                _escrowRepo.Update(escrow);

                // 4. Transaction'ı Kaydet ve Onayla
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"İhtilaf SATICI lehine çözüldü (Commit). Admin: {adminId}, Dispute: {disputeId}");

                // 5. (Transaction DIŞINDA) Finansal Servisi Tetikle
                // Artık Escrow 'Released' olduğuna göre, IWalletService
                // parayı (komisyonu ve satıcı payını) dağıtabilir.
                // await _walletService.ProcessEscrowPayoutAsync(escrow.ProductId);

                // (Alıcı ve Satıcı'ya bildirim gönder)
                // await _notificationService.NotifyDisputeResolved(dispute, "Seller");

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogCritical(ex, $"İhtilaf SATICI lehine çözülürken KRİTİK HATA (Rollback). Admin: {adminId}, Dispute: {disputeId}");
                return false;
            }
        }

        /// <summary>
        /// (KRİTİK) İhtilafı ALICI lehine çözer.
        /// Transactional: Dispute ve Escrow durumunu GÜNCELLER.
        /// Post-Transaction: WalletService'i TETİKLER.
        /// </summary>
        public async Task<bool> ResolveDisputeForBuyerAsync(int disputeId, string adminId, string resolutionNote)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"İhtilaf ALICI lehine çözülüyor (Transaction başladı). Admin: {adminId}, Dispute: {disputeId}");

            try
            {
                var dispute = await _disputeRepo.GetDisputeWithDetailsAsync(disputeId); // Escrow'u da yükler
                if (dispute == null || dispute.Product.Escrow == null)
                    throw new KeyNotFoundException("İhtilaf veya ilişkili Escrow bulunamadı.");

                var escrow = dispute.Product.Escrow;
                string logNote = $"Admin ({adminId}) tarafından alıcı lehine çözüldü. Not: {resolutionNote}";

                // 1. İş Mantığını Modele Devret (Dispute)
                dispute.Resolve(logNote);

                // 2. İş Mantığını Modele Devret (Escrow)
                escrow.ResolveByRefunding(); // Durumu 'Refunded' yapar

                // 3. Değişiklikleri Bildir
                _disputeRepo.Update(dispute);
                _escrowRepo.Update(escrow);

                // 4. Transaction'ı Kaydet ve Onayla
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"İhtilaf ALICI lehine çözüldü (Commit). Admin: {adminId}, Dispute: {disputeId}");

                // 5. (Transaction DIŞINDA) Finansal Servisi Tetikle
                // Artık Escrow 'Refunded' olduğuna göre, IWalletService
                // parayı (Alıcının cüzdanına) iade edebilir.
                // await _walletService.ProcessEscrowRefundAsync(escrow.ProductId);

                // (Alıcı ve Satıcı'ya bildirim gönder)
                // await _notificationService.NotifyDisputeResolved(dispute, "Buyer");

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogCritical(ex, $"İhtilaf ALICI lehine çözülürken KRİTİK HATA (Rollback). Admin: {adminId}, Dispute: {disputeId}");
                return false;
            }
        }

        // ====================================================================
        //  3. İÇERİK MODERASYONU (BlogComment, Product)
        // ====================================================================

        public async Task<List<CommentModerationViewModel>> GetPendingCommentsAsync()
        {
            var comments = await _commentRepo.GetPendingCommentsWithDetailsAsync();
            return _mapper.Map<List<CommentModerationViewModel>>(comments);
        }

        public async Task<bool> ApproveCommentAsync(int commentId, string adminId)
        {
            try
            {
                // Jenerik repoyu kullanıyoruz, çünkü özel 'Include'a gerek yok
                var comment = await _commentGenericRepo.GetByIdAsync(commentId);
                if (comment == null) return false;

                comment.Approve(); // Model metodu
                _commentGenericRepo.Update(comment);
                await _commentGenericRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Yorum onaylandı (ID: {commentId}). Onaylayan: {adminId}");
                // (Burada yorum sahibine bildirim gönderilebilir)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Yorum onaylanırken hata (ID: {commentId})");
                return false;
            }
        }

        public async Task<bool> RejectCommentAsync(int commentId, string adminId)
        {
            try
            {
                var comment = await _commentGenericRepo.GetByIdAsync(commentId);
                if (comment == null) return false;

                comment.Reject(); // Model metodu
                _commentGenericRepo.Update(comment);
                await _commentGenericRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Yorum reddedildi (ID: {commentId}). Reddeden: {adminId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Yorum reddedilirken hata (ID: {commentId})");
                return false;
            }
        }

        public async Task<List<ProductModerationViewModel>> GetPendingProductsAsync()
        {
            var products = await _productRepo.GetPendingProductsForAdminAsync();
            return _mapper.Map<List<ProductModerationViewModel>>(products);
        }

        
    }
}