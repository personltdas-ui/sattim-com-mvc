using Sattim.Web.Models.Dispute;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IDisputeRepository : IGenericRepository<Dispute>
    {
        Task<List<Dispute>> GetDisputesForUserAsync(string userId);

        Task<Dispute?> GetDisputeWithDetailsAsync(int disputeId);

        Task<List<Dispute>> GetPendingDisputesForAdminAsync();
    }
}