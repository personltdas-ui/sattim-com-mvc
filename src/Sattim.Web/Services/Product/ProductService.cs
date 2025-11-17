using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore; // Transaction için
using Microsoft.Extensions.Caching.Memory; // IMemoryCache için
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // ApplicationDbContext
using Sattim.Web.Models.Analytical; // AuditLog, SearchHistory vb.
using Sattim.Web.Models.Audit;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.User; // UserProfile
using Sattim.Web.Services.Notification; // INotificationService
using Sattim.Web.Services.Repositories; // Gerekli tüm Repolar
using Sattim.Web.Services.Storage; // IFileStorageService
using Sattim.Web.ViewModels; // Gerekli
using Sattim.Web.ViewModels.Category;
using Sattim.Web.ViewModels.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Arayüzünüzle aynı namespace'i kullanır
namespace Sattim.Web.Services.Product
{
    public class ProductService : IProductService
    {
        // Repositories
        private readonly IProductRepository _productRepo;
        private readonly IGenericRepository<Models.Category.Category> _categoryRepo;
        private readonly IGenericRepository<ProductAnalytics> _analyticsRepo;
        private readonly IGenericRepository<ProductView> _viewRepo;
        private readonly IGenericRepository<SearchHistory> _searchHistoryRepo;
        private readonly IGenericRepository<PopularSearch> _popularSearchRepo;
        private readonly IGenericRepository<AuditLog> _auditLogRepo;
        private readonly IGenericRepository<ProductImage> _imageRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;

        // Servisler
        private readonly IFileStorageService _storageService;
        private readonly INotificationService _notificationService;

        // Yardımcılar
        private readonly ApplicationDbContext _context; // Transaction için
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;
        private readonly IMemoryCache _cache; // Kategori listesi gibi verileri önbelleğe almak için

        public ProductService(
            IProductRepository productRepo,
            IGenericRepository<Models.Category.Category> categoryRepo,
            IGenericRepository<ProductAnalytics> analyticsRepo,
            IGenericRepository<ProductView> viewRepo,
            IGenericRepository<SearchHistory> searchHistoryRepo,
            IGenericRepository<PopularSearch> popularSearchRepo,
            IGenericRepository<AuditLog> auditLogRepo,
            IGenericRepository<ProductImage> imageRepo,
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

        // ====================================================================
        //  1. PUBLIC QUERIES (Genel Katalog/Arama İşlemleri)
        // ====================================================================

        public async Task<(List<ProductSummaryViewModel> Products, int TotalPages)> GetProductListAsync(ProductFilterViewModel filter)
        {
            var (products, totalCount) = await _productRepo.GetProductsByFilterAsync(filter);

            var viewModels = _mapper.Map<List<ProductSummaryViewModel>>(products);
            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            return (viewModels, totalPages);
        }

        public async Task<ProductDetailViewModel> GetProductDetailsAsync(int productId, string? userId, string ipAddress)
        {
            // 1. Ana veriyi al (Repo, 'Seller', 'Category', 'Images' vb. yükler)
            var product = await _productRepo.GetProductWithDetailsAsync(productId);
            if (product == null || product.Status != ProductStatus.Active)
            {
                throw new KeyNotFoundException("Ürün bulunamadı veya şu anda aktif değil.");
            }

            // 2. DTO'ya dönüştür
            var viewModel = _mapper.Map<ProductDetailViewModel>(product);

            // 3. Eksik verileri manuel olarak al (God-Object'i çözdüğümüz için)
            var sellerProfile = await _profileRepo.GetByIdAsync(product.SellerId);
            if (sellerProfile != null)
            {
                viewModel.Seller.AverageRating = sellerProfile.AverageRating;
                viewModel.Seller.RatingCount = sellerProfile.RatingCount;
            }

            // 4. (Asenkron) Görüntülemeyi Logla (Ana işlemi bekletme)

            await LogProductViewAsync(productId, userId, ipAddress);


            return viewModel;
        }

        public async Task<(List<ProductSummaryViewModel> Products, int ResultCount)> GetSearchResultsAsync(string query, string? userId, string ipAddress)
        {
            var products = await _productRepo.SearchProductsAsync(query);
            var resultCount = products.Count;

            // (Asenkron) Aramayı Logla (Ana işlemi bekletme)
#pragma warning disable CS4014
            LogSearchAsync(query, userId, ipAddress, resultCount);
#pragma warning restore CS4014

            var viewModels = _mapper.Map<List<ProductSummaryViewModel>>(products);
            return (viewModels, resultCount);
        }

        public async Task<List<CategoryViewModel>> GetCategoriesAsync()
        {
            // Kategorileri 1 saatliğine önbelleğe al (Cache)
            return await _cache.GetOrCreateAsync("AllCategories_Tree", async (cacheEntry) =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                // (Bu kod IContentService.GetCategoryTreeAsync ile aynı)
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

        // ====================================================================
        //  2. USER COMMANDS (Kullanıcı/Satıcı Ürün Yönetimi)
        // ====================================================================

        public async Task<ProductFormViewModel> GetProductForCreateAsync()
        {
            var categories = await GetCategoriesAsync(); // Önbellekten gelir
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
            // Transactional: Product ve ProductAnalytics aynı anda oluşturulmalı
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. İş Mantığını Modele Devret (Constructor doğrular)
                var product = new Models.Product.Product(
                    model.Title, model.Description, model.StartingPrice, model.BidIncrement,
                    model.StartDate, model.EndDate, model.CategoryId, userId, model.ReservePrice
                );

                await _productRepo.AddAsync(product);
                await _context.SaveChangesAsync(); // ID'yi almak için kaydet

                // 2. İlişkili 1'e 1 varlığı (Analytics) oluştur
                var analytics = new ProductAnalytics(product.Id);
                await _analyticsRepo.AddAsync(analytics);

                await _context.SaveChangesAsync(); // İkinci değişikliği kaydet

                // 3. Transaction'ı Onayla
                await transaction.CommitAsync();

                _logger.LogInformation($"Yeni ürün oluşturuldu (Pending). ID: {product.Id}, Satıcı: {userId}");
                return (true, product.Id, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Ürün oluşturma başarısız (Rollback). Kullanıcı: {userId}");
                return (false, null, ex.Message); // (Modelin fırlattığı hatayı döndür)
            }
        }

        public async Task<ProductFormViewModel> GetProductForEditAsync(int productId, string userId)
        {
            // Özel repo (Güvenlik kontrolünü de yapar: p.SellerId == userId)
            var product = await _productRepo.GetProductForEditAsync(productId, userId);

            if (product == null)
                throw new UnauthorizedAccessException("Bu ürünü düzenleme yetkiniz yok veya ürün bulunamadı.");

            var model = _mapper.Map<ProductFormViewModel>(product);
            model.Categories = await GetCategoriesAsync(); // Kategori listesini ekle
            return model;
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateProductAsync(int productId, ProductFormViewModel model, string userId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);

                // Güvenlik ve Durum Kontrolü
                if (product == null) return (false, "Ürün bulunamadı.");
                if (product.SellerId != userId) return (false, "Bu ürünü düzenleme yetkiniz yok.");
                if (product.Status != ProductStatus.Pending) return (false, "Sadece 'Onay Bekliyor' durumundaki ürünler düzenlenebilir.");

                // İş Mantığını Modele Devret
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

                // İş Mantığını Modele Devret
                product.Cancel(); // (Model, statü 'Active' ise izin verir, 'Sold' ise hata fırlatır)

                _productRepo.Update(product);
                await _productRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Ürün satıcı tarafından iptal edildi (ID: {productId}).");
                // (Burada INotificationService çağrılıp teklif verenler bilgilendirilir)
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün iptal edilemedi (ID: {productId}).");
                return (false, ex.Message);
            }
        }


        // ====================================================================
        //  3. USER QUERIES (Kullanıcı/Satıcı Panelim)
        // ====================================================================

        public async Task<List<UserProductViewModel>> GetMyProductsAsync(string userId, ProductStatus? filter)
        {
            var products = await _productRepo.GetMyProductsAsync(userId, filter);
            return _mapper.Map<List<UserProductViewModel>>(products);
        }

        // ====================================================================
        //  4. ADMIN/MODERATION COMMANDS (Yönetim Paneli)
        // ====================================================================

        public async Task<bool> ApproveProductAsync(int productId, string adminId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                if (product == null) return false;

                product.Approve(); // Model metodu
                _productRepo.Update(product);

                // Admin eylemini logla
                await _auditLogRepo.AddAsync(new AuditLog("ProductApproved", "Product", productId.ToString(), null, null, "SYSTEM", adminId));

                await _productRepo.UnitOfWork.SaveChangesAsync(); // (Hem Update hem Add)

                _logger.LogInformation($"Ürün onaylandı (ID: {productId}). Onaylayan: {adminId}");

                // (Transaction Dışında) Bildirimi gönder
                // var seller = await _userRepo.GetByIdAsync(product.SellerId);
                // await _notificationService.SendProductApprovedNotificationAsync(seller, product);
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

                product.Cancel(); // Model metodu ('Cancelled' statüsüne çeker)
                _productRepo.Update(product);

                await _auditLogRepo.AddAsync(new AuditLog("ProductRejected", "Product", productId.ToString(),
                    null, $"Reason: {reason}", "SYSTEM", adminId));

                await _productRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Ürün reddedildi (ID: {productId}). Reddeden: {adminId}");

                // (Transaction Dışında) Bildirimi gönder
                // var seller = await _userRepo.GetByIdAsync(product.SellerId);
                // await _notificationService.SendProductRejectedNotificationAsync(seller, product, reason);
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
            // DİKKAT: Bu, ilişkili tüm verileri (Bids, Images, Escrow, Commission vb.)
            // 'OnDelete(DeleteBehavior.Cascade)' kuralı sayesinde SİLECEKTİR.
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

        // ====================================================================
        //  5. IMAGE MANAGEMENT (Resim Yönetimi)
        // ====================================================================

        public async Task<(bool Success, string ErrorMessage)> AddProductImagesAsync(int productId, List<IFormFile> images, string userId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                if (product == null || product.SellerId != userId)
                    return (false, "Yetkisiz işlem.");

                foreach (var file in images)
                {
                    // 1. Harici Servis: Dosyayı kaydet (Azure/S3/Lokal)
                    var imageUrl = await _storageService.UploadFileAsync(file, "product-images");

                    // 2. Modeli oluştur
                    var productImage = new ProductImage(productId, imageUrl);
                    await _imageRepo.AddAsync(productImage);
                }

                // 3. Toplu kaydet
                await _imageRepo.UnitOfWork.SaveChangesAsync();
                return (true, null);
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
                // Güvenlik: Resim, bu kullanıcıya mı ait?
                var image = await _imageRepo.FirstOrDefaultAsync(i =>
                    i.Id == imageId && i.Product.SellerId == userId
                );

                if (image == null)
                    return (false, "Resim bulunamadı veya silme yetkiniz yok.");

                // 1. Harici Servis: Dosyayı sil (Azure/S3/Lokal)
                await _storageService.DeleteFileAsync(image.ImageUrl, "product-images");

                // 2. Veritabanından sil
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
                // Güvenlik: Ürün bu kullanıcıya mı ait?
                var product = await _productRepo.GetByIdAsync(productId);
                if (product == null || product.SellerId != userId)
                    return false;

                // Ürünün mevcut tüm resimlerini TAKİP ET (Update için)
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


        // ====================================================================
        //  PRIVATE HELPERS (Yardımcı Metotlar)
        // ====================================================================

        private async Task LogProductViewAsync(int productId, string? userId, string ipAddress)
        {
            try
            {
                // 1. Ham Logu (ProductView) Ekle
                var viewLog = new ProductView(productId, ipAddress, userId);
                await _viewRepo.AddAsync(viewLog);

                // 2. Analitik Sayacını (ProductAnalytics) Güncelle
                // (Daha performanslı bir dünyada bu, bir kuyruk (queue)
                // sistemiyle (RabbitMQ/Redis) yapılır, ancak bu da çalışır)
                var analytics = await _analyticsRepo.GetByIdAsync(productId);
                if (analytics != null)
                {
                    analytics.IncrementView();

                    // Benzersiz (Unique) sayımı kontrol et
                    if (!await _viewRepo.AnyAsync(v => v.ProductId == productId &&
                            (v.UserId == userId || v.IpAddress == ipAddress)))
                    {
                        analytics.IncrementUniqueView();
                    }

                    _analyticsRepo.Update(analytics);
                }

                await _context.SaveChangesAsync(); // İki repoyu da kaydet
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
                // 1. Arama Geçmişini (SearchHistory) Logla
                var historyLog = new SearchHistory(query, resultCount, ipAddress, userId);
                await _searchHistoryRepo.AddAsync(historyLog);

                // 2. Popüler Arama (PopularSearch) Sayacını Güncelle
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