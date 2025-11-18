using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Product;
using Sattim.Web.Repositories.Concrete;
using System.Linq;
using System.Threading.Tasks;

public class ProductImageRepository : GenericRepository<ProductImage>, IProductImageRepository
{
    public ProductImageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<int> GetMaxDisplayOrderAsync(int productId)
    {
        return await _dbSet
            .Where(p => p.ProductId == productId)
            .MaxAsync(p => (int?)p.DisplayOrder) ?? -1;
    }

    public async Task<bool> HasPrimaryImageAsync(int productId)
    {
        return await _dbSet
            .AnyAsync(img => img.ProductId == productId && img.IsPrimary);
    }

    public async Task<ProductImage> GetImageToMakePrimaryAsync(int productId)
    {
        return await _dbSet
            .Where(img => img.ProductId == productId)
            .OrderBy(img => img.DisplayOrder)
            .FirstOrDefaultAsync();
    }
}