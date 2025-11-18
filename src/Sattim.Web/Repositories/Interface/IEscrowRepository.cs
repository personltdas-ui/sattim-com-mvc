using Sattim.Web.Models.Escrow;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IEscrowRepository : IGenericRepository<Escrow>
    {
        Task<List<Escrow>> GetOrdersForBuyerAsync(string buyerId);

        Task<List<Escrow>> GetSalesForSellerAsync(string sellerId);

        Task<Escrow?> GetOrderDetailAsync(int escrowProductId);
    }
}