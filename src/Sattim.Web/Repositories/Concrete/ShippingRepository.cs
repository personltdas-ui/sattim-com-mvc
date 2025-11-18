using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Shipping;
using Sattim.Web.Repositories.Interface;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class ShippingRepository : GenericRepository<ShippingInfo>, IShippingRepository
    {
        public ShippingRepository(ApplicationDbContext context) : base(context)
        {
        }

        private IQueryable<ShippingInfo> GetBaseQueryWithDetails()
        {
            return _dbSet
                .Include(s => s.Product)
                    .ThenInclude(p => p.Escrow);
        }

        public async Task<ShippingInfo?> GetShippingInfoForUpdateAsync(int productId)
        {
            return await GetBaseQueryWithDetails()
                .FirstOrDefaultAsync(s => s.ProductId == productId);
        }

        public async Task<ShippingInfo?> GetShippingInfoDetailsAsync(int productId)
        {
            return await GetBaseQueryWithDetails()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ProductId == productId);
        }
    }
}