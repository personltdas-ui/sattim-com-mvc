using Sattim.Web.Models.Shipping;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IShippingRepository : IGenericRepository<ShippingInfo>
    {
        Task<ShippingInfo?> GetShippingInfoForUpdateAsync(int productId);

        Task<ShippingInfo?> GetShippingInfoDetailsAsync(int productId);
    }
}