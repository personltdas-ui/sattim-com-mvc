using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Shipping;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    public class ShippingRepository : GenericRepository<ShippingInfo>, IShippingRepository
    {
        public ShippingRepository(ApplicationDbContext context) : base(context)
        {
        }

        // 'Escrow'u dahil eden temel sorgu
        private IQueryable<ShippingInfo> GetBaseQueryWithDetails()
        {
            // ShippingInfo'nun PK'si ProductId olduğu için,
            // Product -> Escrow yolunu takip etmeliyiz.
            return _dbSet
                .Include(s => s.Product)
                    .ThenInclude(p => p.Escrow); // Güvenlik (Alıcı/Satıcı) kontrolü için
        }

        public async Task<ShippingInfo?> GetShippingInfoForUpdateAsync(int productId)
        {
            // Update için varlığı 'Takip Et' (Track)
            return await GetBaseQueryWithDetails()
                .FirstOrDefaultAsync(s => s.ProductId == productId);
        }

        public async Task<ShippingInfo?> GetShippingInfoDetailsAsync(int productId)
        {
            // Read-only için varlığı 'Takip Etme' (AsNoTracking)
            return await GetBaseQueryWithDetails()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ProductId == productId);
        }
    }
}