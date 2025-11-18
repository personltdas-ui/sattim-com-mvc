using Sattim.Web.Repositories.Interface;

namespace Sattim.Web.Repositories.Concrete
{
    public class BidRepository : GenericRepository<Models.Bid.Bid>, IBidRepository
    {
        public BidRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Models.Bid.Bid?> GetHighestBidForProductAsync(int productId)
        {
            return await _dbSet
                .Where(b => b.ProductId == productId)
                .OrderByDescending(b => b.Amount)
                .ThenBy(b => b.BidDate)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Models.Bid.Bid>> GetBidsForProductAsync(int productId)
        {
            return await _dbSet
                .Where(b => b.ProductId == productId)
                .Include(b => b.Bidder)
                .OrderByDescending(b => b.Amount)
                .ThenBy(b => b.BidDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Models.Bid.Bid>> GetBidsForUserAsync(string userId)
        {
            return await _dbSet
                .Where(b => b.BidderId == userId)
                .Include(b => b.Product)
                    .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
                .OrderByDescending(b => b.BidDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}