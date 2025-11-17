using AutoMapper;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // DbContext (Transaction)
using Sattim.Web.Models.Commission;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.Shipping;
using Sattim.Web.Models.UI; // SiteSettings için
using Sattim.Web.Models.User; // UserAddress için
using Sattim.Web.Services.Notification; // INotificationService için
using Sattim.Web.Services.Repositories; // Gerekli tüm Repolar
using Sattim.Web.ViewModels.Order; // Arayüzün istediği DTO'lar
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Arayüzünüzle aynı namespace'i kullanır
namespace Sattim.Web.Services.Order
{
    public class OrderService : IOrderService
    {
        // Gerekli Özel Repolar
        private readonly IEscrowRepository _escrowRepo;
        private readonly IProductRepository _productRepo; // (Ürünü 'Update' etmek için)

        // Gerekli Jenerik Repolar
        private readonly IGenericRepository<Commission> _commissionRepo;
        private readonly IGenericRepository<ShippingInfo> _shippingRepo;
        private readonly IGenericRepository<UserAddress> _addressRepo;
        private readonly IGenericRepository<SiteSettings> _settingsRepo;
        private readonly IGenericRepository<ApplicationUser> _userRepo; // (Kazananı/kaybedenleri bulmak için)

        // Gerekli Diğer Servisler
        private readonly INotificationService _notificationService; // Bildirimler için

        // Yardımcılar
        private readonly ApplicationDbContext _context; // Transaction yönetimi için
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IEscrowRepository escrowRepo,
            IProductRepository productRepo,
            IGenericRepository<Commission> commissionRepo,
            IGenericRepository<ShippingInfo> shippingRepo,
            IGenericRepository<UserAddress> addressRepo,
            IGenericRepository<SiteSettings> settingsRepo,
            IGenericRepository<ApplicationUser> userRepo,
            INotificationService notificationService,
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<OrderService> logger)
        {
            _escrowRepo = escrowRepo;
            _productRepo = productRepo;
            _commissionRepo = commissionRepo;
            _shippingRepo = shippingRepo;
            _addressRepo = addressRepo;
            _settingsRepo = settingsRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // ====================================================================
        //  QUERIES (Okuma İşlemleri)
        // ====================================================================

        public async Task<List<OrderSummaryViewModel>> GetMyOrdersAsync(string buyerId)
        {
            var escrows = await _escrowRepo.GetOrdersForBuyerAsync(buyerId);
            return _mapper.Map<List<OrderSummaryViewModel>>(escrows);
        }

        public async Task<List<SalesSummaryViewModel>> GetMySalesAsync(string sellerId)
        {
            var escrows = await _escrowRepo.GetSalesForSellerAsync(sellerId);
            return _mapper.Map<List<SalesSummaryViewModel>>(escrows);
        }

        public async Task<OrderDetailViewModel> GetOrderDetailAsync(int productId, string userId)
        {
            // 1. Özel Repo'dan tüm detayları al
            var escrow = await _escrowRepo.GetOrderDetailAsync(productId);

            if (escrow == null)
                throw new KeyNotFoundException("Sipariş detayı bulunamadı.");

            // 2. Güvenlik Kontrolü: Bu kişi Alıcı veya Satıcı mı?
            if (escrow.BuyerId != userId && escrow.SellerId != userId)
            {
                _logger.LogWarning($"Yetkisiz sipariş detayı erişim denemesi. Kullanıcı: {userId}, Sipariş (ÜrünID): {productId}");
                throw new UnauthorizedAccessException("Bu sipariş detayını görüntüleme yetkiniz yok.");
            }

            // 3. DTO'ya dönüştür
            return _mapper.Map<OrderDetailViewModel>(escrow);
        }

        // ====================================================================
        //  COMMAND (Transactional Yazma İşlemi)
        // ====================================================================

        /// <summary>
        /// Süresi dolan bir ihaleyi sonuçlandırır.
        /// Transactional: Product, Escrow, Commission, ShippingInfo.
        /// </summary>
        public async Task<bool> FinalizeAuctionAsync(Models.Product.Product product)
        {
            if (product.Status != ProductStatus.Active)
            {
                _logger.LogWarning($"İhale sonlandırılamadı. Ürün 'Active' değil (ID: {product.Id}, Status: {product.Status})");
                return false;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"İhale sonlandırma işlemi (Transaction) başlatıldı: (Ürün: {product.Id})");

            bool isSold = false;
            string? winnerId = null;
            Models.Bid.Bid? winningBid = null;

            try
            {
                // 1. Kazananı Belirle
                // (Ürünün 'Bids' koleksiyonunun zaten 'Include' edildiğini varsayıyoruz)
                winningBid = product.Bids
                    .OrderByDescending(b => b.Amount)
                    .ThenBy(b => b.BidDate)
                    .FirstOrDefault();

                if (winningBid != null)
                {
                    // Rezerv Fiyatı Kontrol Et
                    if (product.ReservePrice.HasValue && winningBid.Amount < product.ReservePrice.Value)
                    {
                        isSold = false; // Satılmadı (Rezerv fiyata ulaşmadı)
                    }
                    else
                    {
                        isSold = true; // Satıldı
                        winnerId = winningBid.BidderId;
                    }
                }
                else
                {
                    isSold = false; // Satılmadı (Teklif yok)
                }

                // 2. Ürünün Statüsünü Kapat (Model Metoduyla)
                product.CloseAuction(winnerId);
                _productRepo.Update(product); // (Zaten takip ediliyor ama emin olmak için)

                if (isSold)
                {
                    _logger.LogInformation($"Ürün SATILDI (ID: {product.Id}). Kazanan: {winnerId}, Fiyat: {winningBid.Amount}");

                    // 3a. Escrow (Sipariş) Oluştur
                    var escrow = new Escrow(product.Id, winnerId, product.SellerId, winningBid.Amount);
                    await _escrowRepo.AddAsync(escrow);

                    // 3b. Commission (Komisyon) Oluştur
                    var rateSetting = await _settingsRepo.FirstOrDefaultAsync(s => s.Key == "CommissionRate");
                    if (rateSetting == null || !decimal.TryParse(rateSetting.Value, out var commissionRate))
                    {
                        throw new InvalidOperationException("Site ayarlarında 'CommissionRate' bulunamadı veya geçersiz.");
                    }
                    var commission = new Commission(product.Id, winningBid.Amount, commissionRate);
                    await _commissionRepo.AddAsync(commission);

                    // 3c. ShippingInfo (Kargo Bilgisi) Oluştur
                    // (Alıcının varsayılan adresini al)
                    var buyerAddress = await _addressRepo.FirstOrDefaultAsync(a => a.UserId == winnerId && a.IsDefault) ??
                                       await _addressRepo.FirstOrDefaultAsync(a => a.UserId == winnerId);

                    if (buyerAddress == null)
                        throw new InvalidOperationException($"Kazanan alıcının (ID: {winnerId}) kayıtlı adresi bulunamadı. Sipariş oluşturulamıyor.");

                    var shipping = new ShippingInfo(
                        product.Id,
                        winnerId,
                        buyerAddress.FullName,
                        buyerAddress.Address,
                        buyerAddress.City,
                        buyerAddress.PostalCode,
                        buyerAddress.Phone
                    );
                    await _shippingRepo.AddAsync(shipping);
                }
                else
                {
                    _logger.LogInformation($"Ürün SATILMADI (ID: {product.Id}). Teklif yok veya rezerv fiyata ulaşılamadı.");
                }

                // 4. Transaction'ı Kaydet ve Onayla
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"İhale sonlandırma işlemi BAŞARILI (Commit). (Ürün: {product.Id})");

                // 5. (Transaction DIŞINDA) Bildirimleri Gönder
                try
                {
                    if (isSold)
                    {
                        var winner = await _userRepo.GetByIdAsync(winnerId);
                        await _notificationService.SendAuctionWonNotificationAsync(winner, product);
                        await _notificationService.SendProductSoldNotificationAsync(product.Seller, product);

                        var loserIds = product.Bids.Select(b => b.BidderId).Where(id => id != winnerId).Distinct().ToList();
                        await _notificationService.SendAuctionLostNotificationAsync(loserIds, product);
                    }
                    else
                    {
                        await _notificationService.SendAuctionNotSoldNotificationAsync(product.Seller, product);
                    }
                }
                catch (Exception notifyEx)
                {
                    // KRİTİK: Ana işlem başarılı oldu. Bildirim hatası (örn: SMTP sunucusu çöktü)
                    // ana işlemin sonucunu 'false' döndürmemeli. Sadece logla.
                    _logger.LogCritical(notifyEx, $"İhale sonlandırma (ID: {product.Id}) başarılı, ancak bildirim gönderilemedi!");
                }

                return true; // Ana işlem başarılı
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogCritical(ex, $"İhale sonlandırma sırasında KRİTİK HATA (Rollback). (Ürün: {product.Id})");
                return false; // Ana işlem başarısız
            }
        }
    }
}