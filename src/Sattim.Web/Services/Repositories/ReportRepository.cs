using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Analytical;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    public class ReportRepository : GenericRepository<Models.Analytical.Report>, IReportRepository
    {
        public ReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Models.Analytical.Report>> GetPendingReportsWithDetailsAsync()
        {
            return await _dbSet
                .Include(r => r.Reporter) // Raporlayan kullanıcıyı yükle
                .Where(r => r.Status == ReportStatus.Pending)
                .OrderByDescending(r => r.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}