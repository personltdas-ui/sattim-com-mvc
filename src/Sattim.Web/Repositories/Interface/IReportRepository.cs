using Sattim.Web.Models.Analytical;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IReportRepository : IGenericRepository<Report>
    {
        Task<List<Report>> GetPendingReportsWithDetailsAsync();
    }
}