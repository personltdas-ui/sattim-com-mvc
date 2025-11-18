using Sattim.Web.ViewModels.Shipping;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Shipping
{
    public interface IShippingService
    {
        Task<(bool Success, string ErrorMessage)> MarkAsShippedAsync(MarkAsShippedViewModel model, string sellerId);

        Task<(bool Success, string ErrorMessage)> MarkAsDeliveredAsync(int productId, string buyerId);

        Task<ShippingDetailViewModel> GetShippingDetailsAsync(int productId, string userId);
    }
}