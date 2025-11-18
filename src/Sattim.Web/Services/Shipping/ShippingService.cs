using AutoMapper;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.Services.Notification;
using Sattim.Web.Services.Wallet;
using Sattim.Web.ViewModels.Shipping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Shipping
{
    public class ShippingService : IShippingService
    {
        private readonly IShippingRepository _shippingRepo;
        private readonly IGenericRepository<ApplicationUser> _userRepo;

        private readonly IWalletService _walletService;
        private readonly INotificationService _notificationService;

        private readonly ApplicationDbContext _context;
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

        // --------------------------------------------------------------------
        //  QUERY (Okuma İşlemi)
        // --------------------------------------------------------------------

        public async Task<ShippingDetailViewModel> GetShippingDetailsAsync(int productId, string userId)
        {
            var shippingInfo = await _shippingRepo.GetShippingInfoDetailsAsync(productId);

            if (shippingInfo == null)
                throw new KeyNotFoundException("Kargo detayı bulunamadı.");

            if (shippingInfo.Product.Escrow.BuyerId != userId &&
                shippingInfo.Product.Escrow.SellerId != userId)
            {
                _logger.LogWarning($"Yetkisiz kargo detayı erişim denemesi. Kullanıcı: {userId}, Sipariş (ÜrünID): {productId}");
                throw new UnauthorizedAccessException("Bu kargo detayını görüntüleme yetkiniz yok.");
            }

            return _mapper.Map<ShippingDetailViewModel>(shippingInfo);
        }

        // --------------------------------------------------------------------
        //  COMMANDS (Yazma İşlemleri)
        // --------------------------------------------------------------------

        public async Task<(bool Success, string ErrorMessage)> MarkAsShippedAsync(MarkAsShippedViewModel model, string sellerId)
        {
            try
            {
                var shippingInfo = await _shippingRepo.GetShippingInfoForUpdateAsync(model.ProductId);

                if (shippingInfo == null || shippingInfo.Product.Escrow == null)
                    return (false, "İlgili sipariş kaydı bulunamadı.");

                if (shippingInfo.Product.Escrow.SellerId != sellerId)
                    return (false, "Bu siparişi kargolandı olarak işaretleme yetkiniz yok.");

                if (shippingInfo.Product.Escrow.Status != EscrowStatus.Funded)
                    return (false, "Sadece 'Ödendi' durumundaki siparişler kargolanabilir.");

                shippingInfo.Ship(model.Carrier, model.TrackingNumber);

                _shippingRepo.Update(shippingInfo);
                await _shippingRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Sipariş kargolandı. (ProductID: {model.ProductId}, SellerID: {sellerId})");

                try
                {
                    var buyer = await _userRepo.GetByIdAsync(shippingInfo.Product.Escrow.BuyerId);
                    await _notificationService.SendProductShippedNotificationAsync(buyer, shippingInfo.Product, shippingInfo);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, "Sipariş kargolandı, ancak Alıcı bildirimi gönderilemedi.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Sipariş kargolandı olarak işaretlenirken hata. ProductID: {model.ProductId}");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> MarkAsDeliveredAsync(int productId, string buyerId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"Teslimat onayı (Transaction) başlatıldı: Alıcı: {buyerId}, ProductID: {productId}");

            try
            {
                var shippingInfo = await _shippingRepo.GetShippingInfoForUpdateAsync(productId);

                if (shippingInfo == null || shippingInfo.Product.Escrow == null)
                    return (false, "İlgili sipariş kaydı bulunamadı.");

                if (shippingInfo.Product.Escrow.BuyerId != buyerId)
                    return (false, "Bu siparişi onaylama yetkiniz yok.");

                shippingInfo.Deliver();
                _shippingRepo.Update(shippingInfo);

                await _shippingRepo.UnitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation($"Teslimat onaylandı (Commit). Alıcı: {buyerId}, ProductID: {productId}");

                try
                {
                    var seller = await _userRepo.GetByIdAsync(shippingInfo.Product.Escrow.SellerId);
                    await _notificationService.SendProductDeliveredNotificationAsync(seller, shippingInfo.Product);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, "Teslimat onaylandı, ancak Satıcı bildirimi gönderilemedi.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Teslimat onayı sırasında KRİTİK HATA (Rollback). ProductID: {productId}");
                return (false, ex.Message);
            }
        }
    }
}