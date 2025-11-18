using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Bid;
using Sattim.Web.Models.Product;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.ViewModels.Bid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Bid
{
    public class BidService : IBidService
    {
        private readonly IBidRepository _bidRepo;
        private readonly IProductRepository _productRepo;
        private readonly IGenericRepository<AutoBid> _autoBidRepo;

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BidService> _logger;


        public BidService(
          IBidRepository bidRepo,
          IProductRepository productRepo,
          IGenericRepository<AutoBid> autoBidRepo,
          ApplicationDbContext context,
          IMapper mapper,
          ILogger<BidService> logger

          )
        {
            _bidRepo = bidRepo;
            _productRepo = productRepo;
            _autoBidRepo = autoBidRepo;
            _context = context;
            _mapper = mapper;
            _logger = logger;

        }

        public async Task<(bool Success, string ErrorMessage)> PlaceBidAsync(PlaceBidViewModel model, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var product = await _productRepo.GetByIdAsync(model.ProductId);

                if (product == null)
                    return (false, "Teklif verilmek istenen ürün bulunamadı.");
                if (product.Status != ProductStatus.Active)
                    return (false, "Bu ihale şu anda aktif değil.");
                if (product.EndDate <= DateTime.UtcNow)
                    return (false, "Bu ihalenin süresi dolmuş.");
                if (product.SellerId == userId)
                    return (false, "Kendi ürününüze teklif veremezsiniz.");

                decimal minBid;
                if (product.CurrentPrice == product.StartingPrice && (await _bidRepo.AnyAsync(b => b.ProductId == product.Id) == false))
                {
                    minBid = product.StartingPrice;
                }
                else
                {
                    minBid = product.CurrentPrice + product.BidIncrement;
                }

                if (model.Amount < minBid)
                    return (false, $"Teklifiniz minimum ({minBid:C}) tutarı karşılamıyor.");

                product.UpdateCurrentPrice(model.Amount);

                var newBid = new Models.Bid.Bid(product.Id, userId, model.Amount);

                _productRepo.Update(product);
                await _bidRepo.AddAsync(newBid);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation($"Manuel Teklif BAŞARILI (Commit). Kullanıcı: {userId}, Ürün: {product.Id}, Yeni Fiyat: {model.Amount}");

                await ProcessAutoBidsAsync(product, newBid);

                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Teklif verme sırasında KRİTİK HATA (Rollback). Kullanıcı: {userId}, Ürün: {model.ProductId}");
                return (false, "Teklifiniz işlenirken beklenmedik bir sistem hatası oluştu.");
            }
        }

        public async Task<(bool Success, string ErrorMessage)> PlaceAutoBidAsync(AutoBidViewModel model, string userId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(model.ProductId);
                if (product == null || product.Status != ProductStatus.Active)
                    return (false, "Bu ürüne şu anda otomatik teklif ayarlanamaz.");

                if (model.MaxAmount <= product.CurrentPrice)
                    return (false, "Maksimum teklif, mevcut fiyattan yüksek olmalıdır.");

                var existingAutoBid = await _autoBidRepo.GetByIdAsync(new object[] { userId, model.ProductId });

                if (existingAutoBid != null)
                {
                    existingAutoBid.UpdateSettings(model.MaxAmount, model.IncrementAmount);
                    _autoBidRepo.Update(existingAutoBid);
                    _logger.LogInformation($"Otomatik teklif güncellendi. Kullanıcı: {userId}, Ürün: {model.ProductId}");
                }
                else
                {
                    existingAutoBid = new AutoBid(userId, model.ProductId, model.MaxAmount, model.IncrementAmount);
                    await _autoBidRepo.AddAsync(existingAutoBid);
                    _logger.LogInformation($"Yeni otomatik teklif ayarlandı. Kullanıcı: {userId}, Ürün: {model.ProductId}");
                }

                await _autoBidRepo.UnitOfWork.SaveChangesAsync();

                var highestBid = await _bidRepo.GetHighestBidForProductAsync(product.Id);
                if (highestBid == null || (highestBid.BidderId != userId && existingAutoBid.MaxAmount > product.CurrentPrice))
                {
                    var dummyBid = new Models.Bid.Bid(product.Id, "SYSTEM", product.CurrentPrice);
                    await ProcessAutoBidsAsync(product, dummyBid);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Otomatik teklif ayarlanırken hata. Kullanıcı: {userId}, Ürün: {model.ProductId}");
                return (false, ex.Message);
            }
        }

        public async Task<bool> CancelAutoBidAsync(int productId, string userId)
        {
            try
            {
                var autoBid = await _autoBidRepo.GetByIdAsync(new object[] { userId, productId });
                if (autoBid == null)
                {
                    _logger.LogWarning($"İptal edilecek otomatik teklif bulunamadı. Kullanıcı: {userId}, Ürün: {productId}");
                    return false;
                }

                autoBid.Deactivate();
                _autoBidRepo.Update(autoBid);
                await _autoBidRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Otomatik teklif iptal edildi. Kullanıcı: {userId}, Ürün: {productId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Otomatik teklif iptal edilirken hata. Kullanıcı: {userId}, Ürün: {productId}");
                return false;
            }
        }


        public async Task<ProductBidHistoryViewModel> GetProductBidHistoryAsync(int productId)
        {
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null)
                throw new KeyNotFoundException($"Ürün bulunamadı (ID: {productId})");

            var bids = await _bidRepo.GetBidsForProductAsync(productId);

            var viewModel = _mapper.Map<ProductBidHistoryViewModel>(product);
            viewModel.Bids = _mapper.Map<List<BidHistoryItemViewModel>>(bids);



            return viewModel;
        }

        public async Task<List<UserBidItemViewModel>> GetUserBidsAsync(string userId, BidFilterType filter)
        {
            var allUserBids = await _bidRepo.GetBidsForUserAsync(userId);

            var groupedByProduct = allUserBids.GroupBy(bid => bid.Product);

            var results = new List<UserBidItemViewModel>();

            foreach (var group in groupedByProduct)
            {
                var product = group.Key;
                var myHighestBidInGroup = group.Max(b => b.Amount);

                UserBidStatus status;
                if (product.Status == ProductStatus.Active)
                {
                    status = (product.CurrentPrice == myHighestBidInGroup)
                      ? UserBidStatus.Active_Winning
                      : UserBidStatus.Active_Losing;
                }
                else if (product.Status == ProductStatus.Sold)
                {
                    status = (product.WinnerId == userId)
                      ? UserBidStatus.Won
                      : UserBidStatus.Lost;
                }
                else
                {
                    status = UserBidStatus.Lost;
                }

                bool matchesFilter = filter == BidFilterType.All ||
                  (filter == BidFilterType.Active && (status == UserBidStatus.Active_Losing || status == UserBidStatus.Active_Winning)) ||
                  (filter == BidFilterType.Won && status == UserBidStatus.Won) ||
                  (filter == BidFilterType.Lost && status == UserBidStatus.Lost);

                if (!matchesFilter)
                    continue;

                var viewModelItem = _mapper.Map<UserBidItemViewModel>(product);
                viewModelItem.MyHighestBid = myHighestBidInGroup;
                viewModelItem.Status = status;

                results.Add(viewModelItem);
            }

            return results;
        }

        public async Task<AutoBidSettingViewModel> GetUserAutoBidSettingAsync(int productId, string userId)
        {
            var autoBid = await _autoBidRepo.GetByIdAsync(new object[] { userId, productId });
            if (autoBid == null)
                return null;

            return _mapper.Map<AutoBidSettingViewModel>(autoBid);
        }

        private async Task ProcessAutoBidsAsync(Models.Product.Product product, Models.Bid.Bid lastBid)
        {
            _logger.LogInformation($"Otomatik teklif süreci kontrol ediliyor... (Ürün: {product.Id})");

            var autoBids = await _autoBidRepo.FindAsync(ab =>
              ab.ProductId == product.Id &&
              ab.IsActive &&
              ab.UserId != lastBid.BidderId &&
              ab.MaxAmount > product.CurrentPrice
            );

            if (!autoBids.Any())
            {
                _logger.LogInformation("Tetiklenecek aktif otomatik teklif bulunamadı.");
                return;
            }

            var highestAutoBidder = autoBids
              .OrderByDescending(ab => ab.MaxAmount)
              .ThenBy(ab => ab.CreatedDate)
              .First();

            decimal newAutoBidAmount = product.CurrentPrice + product.BidIncrement;

            if (newAutoBidAmount > highestAutoBidder.MaxAmount)
            {
                newAutoBidAmount = highestAutoBidder.MaxAmount;
                highestAutoBidder.Deactivate();
                _autoBidRepo.Update(highestAutoBidder);
            }

            if (newAutoBidAmount <= product.CurrentPrice)
            {
                _logger.LogWarning($"Otomatik teklif (Kullanıcı: {highestAutoBidder.UserId}) tetiklenemedi, limit aşıldı.");
                highestAutoBidder.Deactivate();
                _autoBidRepo.Update(highestAutoBidder);
                await _autoBidRepo.UnitOfWork.SaveChangesAsync();
                return;
            }

            await using var autoBidTx = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Otomatik teklif tetiklendi: Kullanıcı: {highestAutoBidder.UserId}, Tutar: {newAutoBidAmount}");

                product.UpdateCurrentPrice(newAutoBidAmount);
                _productRepo.Update(product);

                var autoBidEntry = new Models.Bid.Bid(product.Id, highestAutoBidder.UserId, newAutoBidAmount);
                await _bidRepo.AddAsync(autoBidEntry);

                await _context.SaveChangesAsync();
                await autoBidTx.CommitAsync();
            }
            catch (Exception ex)
            {
                await autoBidTx.RollbackAsync();
                _logger.LogError(ex, "Otomatik teklif verilirken kritik hata (Rollback).");
            }
        }
    }
}