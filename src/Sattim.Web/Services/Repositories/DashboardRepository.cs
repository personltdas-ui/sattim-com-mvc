using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Commission;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.Wallet;
using Sattim.Web.Models.Analytical;
using Sattim.Web.ViewModels.Management;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public DashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Bu metot, sizin 10+ sıralı sorgunuzu alır ve tek bir yerde
        /// (Repository katmanında) çalıştırır.
        /// </summary>
        public async Task<AdminDashboardViewModel> GetDashboardDataAsync()
        {
            // NOT: EF Core 7+ 'ExecuteUpdate' ve 'ExecuteDelete' gibi
            // toplu (batch) işlemleri destekler, ancak toplu 'Select' (istatistik)
            // için en iyi yöntem ya 'FromSqlRaw' ile Stored Procedure
            // çağırmak ya da bu sıralı sorguları (sizin yaptığınız gibi)
            // Repository katmanında izole etmektir.
            // Bu yöntem, 'In-Memory' veritabanı ile test edilebilir
            // olduğu için Stored Procedure'den daha esnektir.

            var model = new AdminDashboardViewModel();
            var now = DateTime.UtcNow; // Sorgularda tutarlılık için zamanı şimdi al

            // --- 1. Ana Metrikler (KPIs) - Sıralı Sorgular ---
            model.TotalUsers = await _context.Users.AsNoTracking().CountAsync();

            model.TotalSalesVolume = await _context.Escrows.AsNoTracking()
                .Where(e => e.Status == EscrowStatus.Released)
                .SumAsync(e => e.Amount);

            model.TotalCommissionsEarned = await _context.Commissions.AsNoTracking()
                .Where(c => c.Status == CommissionStatus.Collected)
                .SumAsync(c => c.CommissionAmount);

            model.TotalActiveAuctions = await _context.Products.AsNoTracking()
                .CountAsync(p => p.Status == ProductStatus.Active && p.EndDate > now);

            // --- 2. Moderasyon Kuyruğu - Sıralı Sorgular ---
            model.PendingProducts = await _context.Products.AsNoTracking()
                .CountAsync(p => p.Status == ProductStatus.Pending);

            model.PendingDisputes = await _context.Disputes.AsNoTracking()
                .CountAsync(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview);

            model.PendingReports = await _context.Reports.AsNoTracking()
                .CountAsync(r => r.Status == ReportStatus.Pending);

            model.PendingPayouts = await _context.PayoutRequests.AsNoTracking()
                .CountAsync(p => p.Status == PayoutStatus.Pending || p.Status == PayoutStatus.Approved);

            model.PendingComments = await _context.BlogComments.AsNoTracking()
                .CountAsync(c => !c.IsApproved);

            // --- 3. Akış (Feeds) - Sıralı Sorgular ---

            // DÜZELTME (N+1 Hatası): '.Include(e => e.Product)' eklendi.
            model.RecentSales = await _context.Escrows.AsNoTracking()
                .Where(e => e.Status == EscrowStatus.Released)
                .OrderByDescending(e => e.ReleasedDate)
                .Take(5)
                .Include(e => e.Product) // N+1 Hatasını önlemek için Eklendi
                .Select(e => new RecentSaleViewModel
                {
                    ProductId = e.ProductId,
                    ProductName = e.Product.Title, // Bu artık güvenli
                    Amount = e.Amount,
                    SaleDate = e.ReleasedDate.Value
                })
                .ToListAsync();

            model.RecentUsers = await _context.Users.AsNoTracking()
                .OrderByDescending(u => u.RegisteredDate)
                .Take(5)
                .Select(u => new RecentUserViewModel
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    RegisteredDate = u.RegisteredDate
                })
                .ToListAsync();

            return model;
        }
    }
}