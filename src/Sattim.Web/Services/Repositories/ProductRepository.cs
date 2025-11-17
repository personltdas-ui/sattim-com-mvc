using Sattim.Web.Models.Product;

namespace Sattim.Web.Services.Repositories
{
    public class ProductRepository : GenericRepository<Models.Product.Product>, IProductRepository
    {
        // 'GenericRepository' zaten _context'i 'protected' olarak
        // tanımladığı için ona erişebiliriz.
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Models.Product.Product?> GetProductWithDetailsAsync(int productId)
        {
            // Bu, 'Eager Loading' (Erken Yükleme) örneğidir.
            // Servis katmanının ihtiyaç duyduğu tüm verileri tek sorguda getiririz.
            return await _dbSet
                .Include(p => p.Seller)      // Satıcıyı yükle
                .Include(p => p.Category)    // Kategoriyi yükle
                .Include(p => p.Images)      // Resimleri yükle
                .Include(p => p.Bids)        // Teklifleri yükle
                    .ThenInclude(b => b.Bidder) // Teklif vereni yükle
                .Include(p => p.Reviews)     // Yorumları yükle
                .Include(p => p.Analytics)   // Analitikleri yükle
                .AsNoTracking() // Takip edilmeyen (read-only) bir sorgu
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<IEnumerable<Models.Product.Product>> GetHomepageProductsAsync(int count, int page)
        {
            int skip = (page - 1) * count;

            return await _dbSet
                .Where(p => p.Status == ProductStatus.Active && p.EndDate > DateTime.UtcNow)
                .Include(p => p.Images) // Ana resim için
                .OrderByDescending(p => p.StartDate)
                .Skip(skip)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Models.Product.Product>> GetProductsBySellerAsync(string sellerId)
        {
            return await _dbSet
                .Where(p => p.SellerId == sellerId)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        // === IModerationService İÇİN YENİ EKLENEN METOT ===
        public async Task<List<Models.Product.Product>> GetPendingProductsForAdminAsync()
        {
            return await _dbSet
                .Where(p => p.Status == ProductStatus.Pending)
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Models.Product.Product>> GetMyProductsAsync(string sellerId, ProductStatus? filter)
        {
            var query = _dbSet
                .Where(p => p.SellerId == sellerId)
                .Include(p => p.Images.Where(i => i.IsPrimary))
                .Include(p => p.Bids) // BidCount için
                .AsNoTracking();

            if (filter.HasValue)
            {
                query = query.Where(p => p.Status == filter.Value);
            }

            return await query.OrderByDescending(p => p.CreatedDate).ToListAsync();
        }

        public async Task<Models.Product.Product?> GetProductForEditAsync(int productId, string userId)
        {
            // Düzenleme için varlığı 'Takip Et' (Track), AsNoTracking KULLANMA!
            return await _dbSet
                .Include(p => p.Images) // Resimlerini de yükle
                .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == userId);
        }

        public async Task<List<Models.Product.Product>> SearchProductsAsync(string query)
        {
            var lowerQuery = query.ToLower();
            return await _dbSet
                .Where(p => p.Status == ProductStatus.Active &&
                            (p.Title.ToLower().Contains(lowerQuery) ||
                             p.Description.ToLower().Contains(lowerQuery)))
                .Include(p => p.Images.Where(i => i.IsPrimary))
                .Include(p => p.Bids)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(List<Models.Product.Product> Products, int TotalCount)> GetProductsByFilterAsync(ProductFilterViewModel filter)
        {
            var query = _dbSet
                .Where(p => p.Status == ProductStatus.Active && p.EndDate > DateTime.UtcNow)
                .AsNoTracking();

            // --- FİLTRELEME ---
            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                query = query.Where(p => p.Title.ToLower().Contains(filter.Query.ToLower()));
            }
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.CurrentPrice >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.CurrentPrice <= filter.MaxPrice.Value);
            }

            // --- SIRALAMA ---
            switch (filter.SortBy)
            {
                case ProductSortOrder.EndingSoon:
                    query = query.OrderBy(p => p.EndDate);
                    break;
                case ProductSortOrder.PriceAsc:
                    query = query.OrderBy(p => p.CurrentPrice);
                    break;
                case ProductSortOrder.PriceDesc:
                    query = query.OrderByDescending(p => p.CurrentPrice);
                    break;
                case ProductSortOrder.Newest:
                default:
                    query = query.OrderByDescending(p => p.StartDate);
                    break;
            }

            // --- SAYFALAMA ---
            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Include(p => p.Images.Where(i => i.IsPrimary))
                .Include(p => p.Bids) // BidCount için
                .ToListAsync();

            return (products, totalCount);
        }
    
}
}
