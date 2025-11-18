using AutoMapper;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Commission;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.Shipping;
using Sattim.Web.Models.UI;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.Services.Notification;
using Sattim.Web.ViewModels.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Order
{
    public class OrderService : IOrderService
    {
        private readonly IEscrowRepository _escrowRepo;
        private readonly IProductRepository _productRepo;

        private readonly IGenericRepository<Commission> _commissionRepo;
        private readonly IGenericRepository<ShippingInfo> _shippingRepo;
        private readonly IGenericRepository<UserAddress> _addressRepo;
        private readonly IGenericRepository<SiteSettings> _settingsRepo;
        private readonly IGenericRepository<ApplicationUser> _userRepo;

        private readonly INotificationService _notificationService;

        private readonly ApplicationDbContext _context;
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
            var escrow = await _escrowRepo.GetOrderDetailAsync(productId);

            if (escrow == null)
                throw new KeyNotFoundException("Sipariş detayı bulunamadı.");

            if (escrow.BuyerId != userId && escrow.SellerId != userId)
            {
                _logger.LogWarning($"Yetkisiz sipariş detayı erişim denemesi. Kullanıcı: {userId}, Sipariş (ÜrünID): {productId}");
                throw new UnauthorizedAccessException("Bu sipariş detayını görüntüleme yetkiniz yok.");
            }

            return _mapper.Map<OrderDetailViewModel>(escrow);
        }

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
                winningBid = product.Bids
                  .OrderByDescending(b => b.Amount)
                  .ThenBy(b => b.BidDate)
                  .FirstOrDefault();

                if (winningBid != null)
                {
                    if (product.ReservePrice.HasValue && winningBid.Amount < product.ReservePrice.Value)
                    {
                        isSold = false;
                    }
                    else
                    {
                        isSold = true;
                        winnerId = winningBid.BidderId;
                    }
                }
                else
                {
                    isSold = false;
                }

                product.CloseAuction(winnerId);
                _productRepo.Update(product);

                if (isSold)
                {
                    _logger.LogInformation($"Ürün SATILDI (ID: {product.Id}). Kazanan: {winnerId}, Fiyat: {winningBid.Amount}");

                    var escrow = new Escrow(product.Id, winnerId, product.SellerId, winningBid.Amount);
                    await _escrowRepo.AddAsync(escrow);

                    var rateSetting = await _settingsRepo.FirstOrDefaultAsync(s => s.Key == "CommissionRate");
                    if (rateSetting == null || !decimal.TryParse(rateSetting.Value, out var commissionRate))
                    {
                        throw new InvalidOperationException("Site ayarlarında 'CommissionRate' bulunamadı veya geçersiz.");
                    }
                    var commission = new Commission(product.Id, winningBid.Amount, commissionRate);
                    await _commissionRepo.AddAsync(commission);

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

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"İhale sonlandırma işlemi BAŞARILI (Commit). (Ürün: {product.Id})");

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
                    _logger.LogCritical(notifyEx, $"İhale sonlandırma (ID: {product.Id}) başarılı, ancak bildirim gönderilemedi!");
                }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogCritical(ex, $"İhale sonlandırma sırasında KRİTİK HATA (Rollback). (Ürün: {product.Id})");
                return false;
            }
        }
    }
}