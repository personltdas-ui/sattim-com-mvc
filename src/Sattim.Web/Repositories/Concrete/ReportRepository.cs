using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Analytical;
using Sattim.Web.Repositories.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class ReportRepository : GenericRepository<Report>, IReportRepository
    {
        public ReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Report>> GetPendingReportsWithDetailsAsync()
        {
            return await _dbSet
                .Include(r => r.Reporter)
                .Where(r => r.Status == ReportStatus.Pending)
                .OrderByDescending(r => r.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}