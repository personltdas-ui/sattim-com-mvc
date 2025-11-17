using Sattim.Web.Models.Analytical;
using Sattim.Web.Services.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Report varlığı için 'Reporter' (Kullanıcı) bilgisini
    /// içeren özel sorgular sağlar.
    /// </summary>
    public interface IReportRepository : IGenericRepository<Models.Analytical.Report>
    {
        Task<List<Models.Analytical.Report>> GetPendingReportsWithDetailsAsync();
    }
}