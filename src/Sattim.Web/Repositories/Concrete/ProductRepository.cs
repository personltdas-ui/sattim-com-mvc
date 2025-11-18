using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Product;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.ViewModels.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Product?> GetProductWithDetailsAsync(int productId)
        {
            return await _dbSet
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Bids)
                    .ThenInclude(b => b.Bidder)
                .Include(p => p.Reviews)
                .Include(p => p.Analytics)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<IEnumerable<Product>> GetHomepageProductsAsync(int count, int page)
        {
            int skip = (page - 1) * count;

            return await _dbSet
                .Where(p => p.Status == ProductStatus.Active && p.EndDate > DateTime.UtcNow)
                .Include(p => p.Images)
                .OrderByDescending(p => p.StartDate)
                .Skip(skip)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsBySellerAsync(string sellerId)
        {
            return await _dbSet
                .Where(p => p.SellerId == sellerId)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Product>> GetPendingProductsForAdminAsync()
        {
            return await _dbSet
                .Where(p => p.Status == ProductStatus.Pending)
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Product>> GetMyProductsAsync(string sellerId, ProductStatus? filter)
        {
            var query = _dbSet
                .Where(p => p.SellerId == sellerId)
                .Include(p => p.Images.Where(i => i.IsPrimary))
                .Include(p => p.Bids)
                .AsNoTracking();

            if (filter.HasValue)
            {
                query = query.Where(p => p.Status == filter.Value);
            }

            return await query.OrderByDescending(p => p.CreatedDate).ToListAsync();
        }

        public async Task<Product?> GetProductForEditAsync(int productId, string userId)
        {
            return await _dbSet
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == userId);
        }

        public async Task<List<Product>> SearchProductsAsync(string query)
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

        public async Task<(List<Product> Products, int TotalCount)> GetProductsByFilterAsync(ProductFilterViewModel filter)
        {
            var query = _dbSet
                .Where(p => p.Status == ProductStatus.Active && p.EndDate > DateTime.UtcNow)
                .AsNoTracking();

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

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Include(p => p.Images.Where(i => i.IsPrimary))
                .Include(p => p.Bids)
                .ToListAsync();

            return (products, totalCount);
        }
    }
}