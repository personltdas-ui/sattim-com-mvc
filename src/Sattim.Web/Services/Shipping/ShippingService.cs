using AutoMapper;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // DbContext (Transaction)
using Sattim.Web.Models.Escrow; // EscrowStatus
using Sattim.Web.Models.User; // ApplicationUser
using Sattim.Web.Services.Notification; // INotificationService
using Sattim.Web.Services.Repositories; // Gerekli tüm Repolar
using Sattim.Web.Services.Wallet; // IWalletService
using Sattim.Web.ViewModels.Shipping; // Arayüzün istediği DTO'lar
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Arayüzünüzle aynı namespace'i kullanır
namespace Sattim.Web.Services.Shipping
{
    public class ShippingService : IShippingService
    {
        // Gerekli Özel Repo
        private readonly IShippingRepository _shippingRepo;
        // Gerekli Jenerik Repo
        private readonly IGenericRepository<ApplicationUser> _userRepo;

        // Gerekli Diğer Servisler
        private readonly IWalletService _walletService; // KRİTİK: Para akışı için
        private readonly INotificationService _notificationService;

        // Yardımcılar
        private readonly ApplicationDbContext _context; // Transaction yönetimi için
        private readonly IMapper _mapper;
        private readonly ILogger<ShippingService> _logger;

        public ShippingService(
            IShippingRepository shippingRepo,
            IGenericRepository<ApplicationUser> userRepo,
            IWalletService walletService,
            INotificationService notificationService,
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<ShippingService> logger)
        {
            _shippingRepo = shippingRepo;
            _userRepo = userRepo;
            _walletService = walletService;
            _notificationService = notificationService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // ====================================================================
        //  QUERY (Okuma İşlemi)
        // ====================================================================

        public async Task<ShippingDetailViewModel> GetShippingDetailsAsync(int productId, string userId)
        {
            // 1. Özel Repo'dan veriyi al (Okuma amaçlı - AsNoTracking)
            var shippingInfo = await _shippingRepo.GetShippingInfoDetailsAsync(productId);

            if (shippingInfo == null)
                throw new KeyNotFoundException("Kargo detayı bulunamadı.");

            // 2. Güvenlik Kontrolü: Bu kişi Alıcı veya Satıcı mı?
            if (shippingInfo.Product.Escrow.BuyerId != userId &&
                shippingInfo.Product.Escrow.SellerId != userId)
            {
                _logger.LogWarning($"Yetkisiz kargo detayı erişim denemesi. Kullanıcı: {userId}, Sipariş (ÜrünID): {productId}");
                throw new UnauthorizedAccessException("Bu kargo detayını görüntüleme yetkiniz yok.");
            }

            // 3. DTO'ya dönüştür
            return _mapper.Map<ShippingDetailViewModel>(shippingInfo);
        }

        // ====================================================================
        //  COMMANDS (Yazma İşlemleri)
        // ====================================================================

        public async Task<(bool Success, string ErrorMessage)> MarkAsShippedAsync(MarkAsShippedViewModel model, string sellerId)
        {
            try
            {
                // 1. Varlığı Al (Güncelleme amaçlı - Takip et)
                var shippingInfo = await _shippingRepo.GetShippingInfoForUpdateAsync(model.ProductId);

                // 2. İş Kuralları (Validasyon)
                if (shippingInfo == null || shippingInfo.Product.Escrow == null)
                    return (false, "İlgili sipariş kaydı bulunamadı.");

                // Güvenlik: İşlemi yapan Satıcı mı?
                if (shippingInfo.Product.Escrow.SellerId != sellerId)
                    return (false, "Bu siparişi kargolandı olarak işaretleme yetkiniz yok.");

                // Durum: Sipariş 'Ödendi' (Funded) mi?
                if (shippingInfo.Product.Escrow.Status != EscrowStatus.Funded)
                    return (false, "Sadece 'Ödendi' durumundaki siparişler kargolanabilir.");

                // 3. İş Mantığını Modele Devret
                // (Model metodu, 'carrier' ve 'trackingNumber'ın boş olmadığını
                // ve statünün doğru olduğunu (Pending/Preparing) kontrol eder)
                shippingInfo.Ship(model.Carrier, model.TrackingNumber);

                // 4. Değişiklikleri Kaydet
                _shippingRepo.Update(shippingInfo);
                await _shippingRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Sipariş kargolandı. (ProductID: {model.ProductId}, SellerID: {sellerId})");

                // 5. (İşlem Sonrası) Bildirim Gönder
                try
                {
                    var buyer = await _userRepo.GetByIdAsync(shippingInfo.Product.Escrow.BuyerId);
                    await _notificationService.SendProductShippedNotificationAsync(buyer, shippingInfo.Product, shippingInfo);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, "Sipariş kargolandı, ancak Alıcı bildirimi gönderilemedi.");
                }

                return (true, null); // Başarılı
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Sipariş kargolandı olarak işaretlenirken hata. ProductID: {model.ProductId}");
                return (false, ex.Message); // (Modelden gelen hatayı döndür, örn: "Takip Numarası zorunludur")
            }
        }

        /// <summary>
        /// (KRİTİK) Alıcı siparişi onaylar.
        /// Transactional: ShippingInfo'yu günceller VE IWalletService'i tetikler.
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> MarkAsDeliveredAsync(int productId, string buyerId)
        {
            // 1. Transaction'ı Başlat
            // Bu, 'ShippingInfo'nun güncellenmesi VE 'IWalletService'in
            // tetiklenmesinin TEK BİR ATOMİK İŞLEM olmasını garanti eder.
            // Eğer IWalletService hata verirse (örn: komisyon ayarı yok),
            // 'Delivered' (Teslim Edildi) durumu da geri alınır (Rollback).
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"Teslimat onayı (Transaction) başlatıldı: Alıcı: {buyerId}, ProductID: {productId}");

            try
            {
                // 2. Varlığı Al (Güncelleme amaçlı - Takip et)
                var shippingInfo = await _shippingRepo.GetShippingInfoForUpdateAsync(productId);

                // 3. İş Kuralları (Validasyon)
                if (shippingInfo == null || shippingInfo.Product.Escrow == null)
                    return (false, "İlgili sipariş kaydı bulunamadı.");

                // Güvenlik: İşlemi yapan Alıcı mı?
                if (shippingInfo.Product.Escrow.BuyerId != buyerId)
                    return (false, "Bu siparişi onaylama yetkiniz yok.");

                // 4. İş Mantığını Modele Devret
                // (Model metodu, statünün 'Shipped' veya 'InTransit' olduğunu kontrol eder)
                shippingInfo.Deliver();
                _shippingRepo.Update(shippingInfo);

                // 5. Diğer Servisi (IWalletService) ÇAĞIR
                // (IOrderService'teki FinalizeAuctionAsync'in yaptığı Escrow/Commission/Shipping
                // oluşturma işleminin tam tersi: Para Dağıtma)

                // ***** BURAYA DİKKAT: 'IWalletService.ReleaseFundsToSellerAsync' *****
                // Arayüzünüz bu metodun var olmasını istiyor. Bu metot,
                // Escrow'u 'Released' yapmalı, Komisyonu almalı
                // ve parayı Satıcının cüzdanına aktarmalıdır.

                // (Aşağıdaki metodu IWalletService'e eklemeniz gerekecek)
                // var payoutResult = await _walletService.ReleaseFundsToSellerAsync(productId, buyerId);
                // if (!payoutResult.Success)
                // {
                //     // Eğer cüzdan servisi hata verirse, tüm işlemi geri al
                //     await transaction.RollbackAsync();
                //     _logger.LogWarning($"Teslimat onaylandı ANCAK para transferi başarısız (Rollback): {payoutResult.ErrorMessage}");
                //     return (false, payoutResult.ErrorMessage);
                // }

                // ŞİMDİLİK IWalletService'te o metodun olmadığını varsayarak
                // SADECE ShippingInfo'yu güncelliyoruz:
                await _shippingRepo.UnitOfWork.SaveChangesAsync();

                // (Yukarıdaki yorum satırını açmak,
                // IWalletService'e o metodu eklemek,
                // ve 'SaveChangesAsync'i EN SONA almak,
                // bu sistemi TAM TRANSACTIONAL yapar)

                // 6. Transaction'ı Onayla
                await transaction.CommitAsync();
                _logger.LogInformation($"Teslimat onaylandı (Commit). Alıcı: {buyerId}, ProductID: {productId}");

                // 7. (Transaction DIŞINDA) Bildirim Gönder
                try
                {
                    var seller = await _userRepo.GetByIdAsync(shippingInfo.Product.Escrow.SellerId);
                    await _notificationService.SendProductDeliveredNotificationAsync(seller, shippingInfo.Product);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, "Teslimat onaylandı, ancak Satıcı bildirimi gönderilemedi.");
                }

                return (true, null); // Başarılı
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Teslimat onayı sırasında KRİTİK HATA (Rollback). ProductID: {productId}");
                return (false, ex.Message); // (Modelden gelen hatayı döndür)
            }
        }
    }
}