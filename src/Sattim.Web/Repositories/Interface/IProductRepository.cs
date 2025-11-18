using Sattim.Web.Models.Product;
using Sattim.Web.ViewModels.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetProductWithDetailsAsync(int productId);

        Task<IEnumerable<Product>> GetHomepageProductsAsync(int count, int page);

        Task<IEnumerable<Product>> GetProductsBySellerAsync(string sellerId);
        Task<List<Product>> GetPendingProductsForAdminAsync();

        Task<List<Product>> GetMyProductsAsync(string sellerId, ProductStatus? filter);

        Task<Product?> GetProductForEditAsync(int productId, string userId);

        Task<List<Product>> SearchProductsAsync(string query);

        Task<(List<Product> Products, int TotalCount)> GetProductsByFilterAsync(ProductFilterViewModel filter);
    }
}