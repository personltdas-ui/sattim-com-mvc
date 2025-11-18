using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IBidRepository : IGenericRepository<Models.Bid.Bid>
    {
        Task<Models.Bid.Bid?> GetHighestBidForProductAsync(int productId);

        Task<IEnumerable<Models.Bid.Bid>> GetBidsForProductAsync(int productId);

        Task<IEnumerable<Models.Bid.Bid>> GetBidsForUserAsync(string userId);
    }
}