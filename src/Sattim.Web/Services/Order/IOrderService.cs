using Sattim.Web.Models.Bid;
using Sattim.Web.Models.Product;
using Sattim.Web.ViewModels.Order;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Order
{
    public interface IOrderService
    {
        Task<bool> FinalizeAuctionAsync(Models.Product.Product product);

        Task<List<OrderSummaryViewModel>> GetMyOrdersAsync(string buyerId);

        Task<List<SalesSummaryViewModel>> GetMySalesAsync(string sellerId);

        Task<OrderDetailViewModel> GetOrderDetailAsync(int productId, string userId);
    }
}