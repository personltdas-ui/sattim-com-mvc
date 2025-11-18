using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Repositories.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class DisputeRepository : GenericRepository<Dispute>, IDisputeRepository
    {
        public DisputeRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Dispute>> GetDisputesForUserAsync(string userId)
        {
            return await _dbSet
                .Include(d => d.Product)
                    .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
                .Include(d => d.Product.Escrow)
                .Where(d =>
                    d.Product != null && d.Product.Escrow != null &&
                    (d.Product.Escrow.BuyerId == userId || d.Product.Escrow.SellerId == userId)
                )
                .OrderByDescending(d => d.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dispute?> GetDisputeWithDetailsAsync(int disputeId)
        {
            return await _dbSet
                .Include(d => d.Product)
                    .ThenInclude(p => p.Escrow)
                .Include(d => d.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
        }

        public async Task<List<Dispute>> GetPendingDisputesForAdminAsync()
        {
            return await _dbSet
                .Include(d => d.Product)
                .Include(d => d.Product.Escrow)
                    .ThenInclude(e => e.Buyer)
                .Include(d => d.Product.Escrow)
                    .ThenInclude(e => e.Seller)
                .Where(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview)
                .OrderByDescending(d => d.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}