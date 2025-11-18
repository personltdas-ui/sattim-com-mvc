using Microsoft.AspNetCore.Http;
using Sattim.Web.Models.Product;
using Sattim.Web.ViewModels;
using Sattim.Web.ViewModels.Category;
using Sattim.Web.ViewModels.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Product
{
    public interface IProductService
    {
        Task<(List<ProductSummaryViewModel> Products, int TotalPages)> GetProductListAsync(ProductFilterViewModel filter);
        Task<ProductDetailViewModel> GetProductDetailsAsync(int productId, string? userId, string ipAddress);
        Task<(List<ProductSummaryViewModel> Products, int ResultCount)> GetSearchResultsAsync(string query, string? userId, string ipAddress);
        Task<List<CategoryViewModel>> GetCategoriesAsync();
        Task<ProductFormViewModel> GetProductForCreateAsync();
        Task<(bool Success, int? ProductId, string ErrorMessage)> CreateProductAsync(ProductFormViewModel model, string userId);
        Task<ProductFormViewModel> GetProductForEditAsync(int productId, string userId);
        Task<(bool Success, string ErrorMessage)> UpdateProductAsync(int productId, ProductFormViewModel model, string userId);
        Task<(bool Success, string ErrorMessage)> CancelProductAsync(int productId, string userId);
        Task<List<UserProductViewModel>> GetMyProductsAsync(string userId, ProductStatus? filter);
        Task<bool> ApproveProductAsync(int productId, string adminId);
        Task<bool> RejectProductAsync(int productId, string adminId, string reason);
        Task<bool> DeleteProductAsAdminAsync(int productId, string adminId);
        Task<(bool Success, string ErrorMessage)> AddProductImagesAsync(int productId, List<IFormFile> images, string userId);
        Task<(bool Success, string ErrorMessage)> DeleteProductImageAsync(int imageId, string userId);
        Task<bool> UpdateImageOrderAsync(int productId, List<ImageOrderViewModel> imageOrders, string userId);
    }
}