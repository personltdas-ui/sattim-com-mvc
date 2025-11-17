using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Escrow;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
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
                .Include(e => e.Buyer) // Satıcı, Alıcının adını görmeli
                .OrderByDescending(e => e.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Escrow?> GetOrderDetailAsync(int escrowProductId)
        {
            // Bu, 'mega-query'dir. Detay sayfası için gereken her şeyi yükler.
            return await _dbSet
                .Include(e => e.Product)
                    .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
                .Include(e => e.Product.ShippingInfo) // Kargo bilgilerini yükle
                .Include(e => e.Buyer) // Alıcıyı yükle
                .Include(e => e.Seller) // Satıcıyı yükle
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ProductId == escrowProductId);
        }
    }
}