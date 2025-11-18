using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Analytical;
using Sattim.Web.Models.Audit;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.Services.Notification;
using Sattim.Web.Services.Storage;
using Sattim.Web.ViewModels;
using Sattim.Web.ViewModels.Category;
using Sattim.Web.ViewModels.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Product
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly IGenericRepository<Models.Category.Category> _categoryRepo;
        private readonly IGenericRepository<ProductAnalytics> _analyticsRepo;
        private readonly IGenericRepository<ProductView> _viewRepo;
        private readonly IGenericRepository<SearchHistory> _searchHistoryRepo;
        private readonly IGenericRepository<PopularSearch> _popularSearchRepo;
        private readonly IGenericRepository<AuditLog> _auditLogRepo;
        private readonly IProductImageRepository _imageRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;

        private readonly IFileStorageService _storageService;
        private readonly INotificationService _notificationService;

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;
        private readonly IMemoryCache _cache;

        public ProductService(
            IProductRepository productRepo,
            IGenericRepository<Models.Category.Category> categoryRepo,
            IGenericRepository<ProductAnalytics> analyticsRepo,
            IGenericRepository<ProductView> viewRepo,
            IGenericRepository<SearchHistory> searchHistoryRepo,
            IGenericRepository<PopularSearch> popularSearchRepo,
            IGenericRepository<AuditLog> auditLogRepo,
            IProductImageRepository imageRepo,
            IGenericRepository<UserProfile> profileRepo,
            IFileStorageService storageService,
            INotificationService notificationService,
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<ProductService> logger,
            IMemoryCache cache)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _analyticsRepo = analyticsRepo;
            _viewRepo = viewRepo;
            _searchHistoryRepo = searchHistoryRepo;
            _popularSearchRepo = popularSearchRepo;
            _auditLogRepo = auditLogRepo;
            _imageRepo = imageRepo;
            _profileRepo = profileRepo;
            _storageService = storageService;
            _notificationService = notificationService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
        }

        public async Task<(List<ProductSummaryViewModel> Products, int TotalPages)> GetProductListAsync(ProductFilterViewModel filter)
        {
            var (products, totalCount) = await _productRepo.GetProductsByFilterAsync(filter);

            var viewModels = _mapper.Map<List<ProductSummaryViewModel>>(products);
            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            return (viewModels, totalPages);
        }

        public async Task<ProductDetailViewModel> GetProductDetailsAsync(int productId, string? userId, string ipAddress)
        {
            var product = await _productRepo.GetProductWithDetailsAsync(productId);
            if (product == null || product.Status != ProductStatus.Active)
            {
                throw new KeyNotFoundException("Ürün bulunamadı veya şu anda aktif değil.");
            }

            var viewModel = _mapper.Map<ProductDetailViewModel>(product);

            var sellerProfile = await _profileRepo.GetByIdAsync(product.SellerId);
            if (sellerProfile != null)
            {
                viewModel.Seller.AverageRating = sellerProfile.AverageRating;
                viewModel.Seller.RatingCount = sellerProfile.RatingCount;
            }

            await LogProductViewAsync(productId, userId, ipAddress);

            return viewModel;
        }

        public async Task<(List<ProductSummaryViewModel> Products, int ResultCount)> GetSearchResultsAsync(string query, string? userId, string ipAddress)
        {
            var products = await _productRepo.SearchProductsAsync(query);
            var resultCount = products.Count;

#pragma warning disable CS4014
            LogSearchAsync(query, userId, ipAddress, resultCount);
#pragma warning restore CS4014

            var viewModels = _mapper.Map<List<ProductSummaryViewModel>>(products);
            return (viewModels, resultCount);
        }

        public async Task<List<CategoryViewModel>> GetCategoriesAsync()
        {
            return await _cache.GetOrCreateAsync("AllCategories_Tree", async (cacheEntry) =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                var allCategories = await _categoryRepo.GetAllAsync();
                var lookup = allCategories.ToDictionary(c => c.Id);
                var viewModels = _mapper.Map<List<CategoryViewModel>>(allCategories);
                var tree = new List<CategoryViewModel>();

                foreach (var item in viewModels)
                {
                    if (item.ParentCategoryId.HasValue && lookup.ContainsKey(item.ParentCategoryId.Value))
                    {
                        var parent = viewModels.First(p => p.Id == item.ParentCategoryId.Value);
                        parent.SubCategories.Add(item);
                    }
                    else
                    {
                        tree.Add(item);
                    }
                }
                return tree;
            });
        }

        // --------------------------------------------------------------------
        //  2. USER COMMANDS (Kullanıcı/Satıcı Ürün Yönetimi)
        // --------------------------------------------------------------------

        public async Task<ProductFormViewModel> GetProductForCreateAsync()
        {
            var categories = await GetCategoriesAsync();
            var model = new ProductFormViewModel
            {
                Categories = categories,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7)
            };
            return model;
        }

        public async Task<(bool Success, int? ProductId, string ErrorMessage)> CreateProductAsync(ProductFormViewModel model, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var product = new Models.Product.Product(
                    model.Title, model.Description, model.StartingPrice, model.BidIncrement,
                    model.StartDate, model.EndDate, model.CategoryId, userId, model.ReservePrice
                );

                await _productRepo.AddAsync(product);
                await _context.SaveChangesAsync();

                var analytics = new ProductAnalytics(product.Id);
                await _analyticsRepo.AddAsync(analytics);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation($"Yeni ürün oluşturuldu (Pending). ID: {product.Id}, Satıcı: {userId}");
                return (true, product.Id, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Ürün oluşturma başarısız (Rollback). Kullanıcı: {userId}");
                return (false, null, ex.Message);
            }
        }

        public async Task<ProductFormViewModel> GetProductForEditAsync(int productId, string userId)
        {
            var product = await _productRepo.GetProductForEditAsync(productId, userId);

            if (product == null)
                throw new UnauthorizedAccessException("Bu ürünü düzenleme yetkiniz yok veya ürün bulunamadı.");

            var model = _mapper.Map<ProductFormViewModel>(product);
            model.Categories = await GetCategoriesAsync();
            return model;
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateProductAsync(int productId, ProductFormViewModel model, string userId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);

                if (product == null) return (false, "Ürün bulunamadı.");
                if (product.SellerId != userId) return (false, "Bu ürünü düzenleme yetkiniz yok.");
                if (product.Status != ProductStatus.Pending) return (false, "Sadece 'Onay Bekliyor' durumundaki ürünler düzenlenebilir.");

                product.UpdateDetails(
                    model.Title, model.Description, model.StartingPrice, model.BidIncrement,
                    model.StartDate, model.EndDate, model.CategoryId, model.ReservePrice
                );

                _productRepo.Update(product);
                await _productRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Ürün güncellendi (ID: {productId}).");
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün güncellenemedi (ID: {productId}).");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> CancelProductAsync(int productId, string userId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                if (product == null) return (false, "Ürün bulunamadı.");
                if (product.SellerId != userId) return (false, "Bu ürünü iptal etme yetkiniz yok.");

                product.Cancel();

                _productRepo.Update(product);
                await _productRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Ürün satıcı tarafından iptal edildi (ID: {productId}).");
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün iptal edilemedi (ID: {productId}).");
                return (false, ex.Message);
            }
        }

        // --------------------------------------------------------------------
        //  3. USER QUERIES (Kullanıcı/Satıcı Panelim)
        // --------------------------------------------------------------------

        public async Task<List<UserProductViewModel>> GetMyProductsAsync(string userId, ProductStatus? filter)
        {
            var products = await _productRepo.GetMyProductsAsync(userId, filter);
            return _mapper.Map<List<UserProductViewModel>>(products);
        }

        // --------------------------------------------------------------------
        //  4. ADMIN/MODERATION COMMANDS (Yönetim Paneli)
        // --------------------------------------------------------------------

        public async Task<bool> ApproveProductAsync(int productId, string adminId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                if (product == null) return false;

                product.Approve();
                _productRepo.Update(product);

                await _auditLogRepo.AddAsync(new AuditLog("ProductApproved", "Product", productId.ToString(), null, null, "SYSTEM", adminId));

                await _productRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Ürün onaylandı (ID: {productId}). Onaylayan: {adminId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün onaylanırken hata (ID: {productId})");
                return false;
            }
        }

        public async Task<bool> RejectProductAsync(int productId, string adminId, string reason)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                if (product == null) return false;

                product.Cancel();
                _productRepo.Update(product);

                await _auditLogRepo.AddAsync(new AuditLog("ProductRejected", "Product", productId.ToString(),
                    null, $"Reason: {reason}", "SYSTEM", adminId));

                await _productRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Ürün reddedildi (ID: {productId}). Reddeden: {adminId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün reddedilirken hata (ID: {productId})");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsAdminAsync(int productId, string adminId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                if (product == null) return false;

                _productRepo.Remove(product);

                await _auditLogRepo.AddAsync(new AuditLog("ProductDeleted", "Product", productId.ToString(),
                    null, "Admin tarafından kalıcı olarak silindi.", "SYSTEM", adminId));

                await _productRepo.UnitOfWork.SaveChangesAsync();
                _logger.LogWarning($"Ürün KALICI OLARAK SİLİNDİ (ID: {productId}). Yapan: {adminId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Ürün kalıcı olarak silinirken KRİTİK HATA (ID: {productId})");
                return false;
            }
        }

        // --------------------------------------------------------------------
        //  5. IMAGE MANAGEMENT (Resim Yönetimi)
        // --------------------------------------------------------------------

        public async Task<(bool Success, string ErrorMessage)> AddProductImagesAsync(int productId, List<IFormFile> images, string userId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                if (product == null || product.SellerId != userId)
                    return (false, "Yetkisiz işlem.");

                int maxDisplayOrder = await _imageRepo.GetMaxDisplayOrderAsync(productId);

                for (int i = 0; i < images.Count; i++)
                {
                    var file = images[i];
                    var imageUrl = await _storageService.UploadFileAsync(file, "product-images");
                    int newDisplayOrder = maxDisplayOrder + 1 + i;

                    var productImage = new ProductImage(productId, imageUrl, newDisplayOrder);

                    await _imageRepo.AddAsync(productImage);
                }

                await _imageRepo.UnitOfWork.SaveChangesAsync();

                bool hasPrimary = await _imageRepo.HasPrimaryImageAsync(productId);

                if (!hasPrimary)
                {
                    var imageToMakePrimary = await _imageRepo.GetImageToMakePrimaryAsync(productId);

                    if (imageToMakePrimary != null)
                    {
                        imageToMakePrimary.SetAsPrimary(true);
                        await _imageRepo.UnitOfWork.SaveChangesAsync();
                    }
                }

                return (true, null);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, $"Geçersiz resim verisi (ID: {productId})");
                return (false, argEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürüne resim eklenirken hata (ID: {productId})");
                return (false, "Resimler yüklenirken bir hata oluştu.");
            }
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteProductImageAsync(int imageId, string userId)
        {
            try
            {
                var image = await _imageRepo.FirstOrDefaultAsync(i =>
                    i.Id == imageId && i.Product.SellerId == userId
                );

                if (image == null)
                    return (false, "Resim bulunamadı veya silme yetkiniz yok.");

                await _storageService.DeleteFileAsync(image.ImageUrl, "product-images");

                _imageRepo.Remove(image);
                await _imageRepo.UnitOfWork.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün resmi silinirken hata (ImageID: {imageId})");
                return (false, "Resim silinirken bir hata oluştu.");
            }
        }

        public async Task<bool> UpdateImageOrderAsync(int productId, List<ImageOrderViewModel> imageOrders, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                if (product == null || product.SellerId != userId)
                    return false;

                var images = await _imageRepo.FindAsync(i => i.ProductId == productId);

                foreach (var dbImage in images)
                {
                    var orderInfo = imageOrders.FirstOrDefault(o => o.ImageId == dbImage.Id);
                    if (orderInfo != null)
                    {
                        dbImage.UpdateDisplayOrder(orderInfo.DisplayOrder);
                        dbImage.SetAsPrimary(orderInfo.IsPrimary);
                        _imageRepo.Update(dbImage);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Resim sırası güncellenirken hata (ProductID: {productId})");
                return false;
            }
        }

        // --------------------------------------------------------------------
        //  PRIVATE HELPERS (Yardımcı Metotlar)
        // --------------------------------------------------------------------

        private async Task LogProductViewAsync(int productId, string? userId, string ipAddress)
        {
            try
            {
                var viewLog = new ProductView(productId, ipAddress, userId);
                await _viewRepo.AddAsync(viewLog);

                var analytics = await _analyticsRepo.GetByIdAsync(productId);
                if (analytics != null)
                {
                    analytics.IncrementView();

                    if (!await _viewRepo.AnyAsync(v => v.ProductId == productId &&
                            (v.UserId == userId || v.IpAddress == ipAddress)))
                    {
                        analytics.IncrementUniqueView();
                    }

                    _analyticsRepo.Update(analytics);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"LogProductViewAsync HATA (ProductID: {productId})");
            }
        }

        private async Task LogSearchAsync(string query, string? userId, string ipAddress, int resultCount)
        {
            try
            {
                var historyLog = new SearchHistory(query, resultCount, ipAddress, userId);
                await _searchHistoryRepo.AddAsync(historyLog);

                var popular = await _popularSearchRepo.FirstOrDefaultAsync(p => p.SearchTerm == query);
                if (popular != null)
                {
                    popular.IncrementSearch();
                    _popularSearchRepo.Update(popular);
                }
                else
                {
                    popular = new PopularSearch(query);
                    await _popularSearchRepo.AddAsync(popular);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"LogSearchAsync HATA (Query: {query})");
            }
        }
    }
}