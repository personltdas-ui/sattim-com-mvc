using Sattim.Web.Models.Product;
using Sattim.Web.Repositories.Interface;
using System.Threading.Tasks;

public interface IProductImageRepository : IGenericRepository<ProductImage>
{
    Task<int> GetMaxDisplayOrderAsync(int productId);

    Task<bool> HasPrimaryImageAsync(int productId);

    Task<ProductImage> GetImageToMakePrimaryAsync(int productId);
}