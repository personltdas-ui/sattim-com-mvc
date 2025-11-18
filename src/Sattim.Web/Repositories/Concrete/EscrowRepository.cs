using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Repositories.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Concrete
{
    public class EscrowRepository : GenericRepository<Escrow>, IEscrowRepository
    {
        public EscrowRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Escrow>> GetOrdersForBuyerAsync(string buyerId)
        {
            return await _dbSet
                .Where(e => e.BuyerId == buyerId)
                .Include(e => e.Product)
                    .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
                .OrderByDescending(e => e.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Escrow>> GetSalesForSellerAsync(string sellerId)
        {
            return await _dbSet
                .Where(e => e.SellerId == sellerId)
                .Include(e => e.Product)
                    .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
                .Include(e => e.Buyer)
                .OrderByDescending(e => e.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Escrow?> GetOrderDetailAsync(int escrowProductId)
        {
            return await _dbSet
                .Include(e => e.Product)
                    .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
                .Include(e => e.Product.ShippingInfo)
                .Include(e => e.Buyer)
                .Include(e => e.Seller)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ProductId == escrowProductId);
        }
    }
}