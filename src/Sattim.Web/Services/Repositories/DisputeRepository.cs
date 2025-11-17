using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Dispute;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    public class DisputeRepository : GenericRepository<Models.Dispute.Dispute>, IDisputeRepository
    {
        public DisputeRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Models.Dispute.Dispute>> GetDisputesForUserAsync(string userId)
        {
            // Bu sorgu, kullanıcının Alıcı VEYA Satıcı olduğu
            // tüm ürünlerin ihtilaflarını bulur.
            return await _dbSet
                .Include(d => d.Product)
                    .ThenInclude(p => p.Images.Where(i => i.IsPrimary)) // Ana resmi al
                .Include(d => d.Product.Escrow) // Escrow'u yükle (Alıcı/Satıcı kontrolü için)
                .Where(d =>
                    (d.Product != null && d.Product.Escrow != null) &&
                    (d.Product.Escrow.BuyerId == userId || d.Product.Escrow.SellerId == userId)
                )
                .OrderByDescending(d => d.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Models.Dispute.Dispute?> GetDisputeWithDetailsAsync(int disputeId)
        {
            return await _dbSet
                .Include(d => d.Product)
                    .ThenInclude(p => p.Escrow)
                .Include(d => d.Messages)
                    .ThenInclude(m => m.Sender) // SADECE Sender'ı (ApplicationUser) yükle
                .FirstOrDefaultAsync(d => d.Id == disputeId);
        }

        public async Task<List<Models.Dispute.Dispute>> GetPendingDisputesForAdminAsync()
        {
            return await _dbSet
                .Include(d => d.Product)
                .Include(d => d.Product.Escrow) // Alıcı/Satıcı bilgilerini almak için
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