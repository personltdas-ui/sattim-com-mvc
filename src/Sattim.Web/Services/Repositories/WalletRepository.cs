using Microsoft.EntityFrameworkCore;
using Sattim.Web.Data;
using Sattim.Web.Models.Wallet;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    public class WalletRepository : GenericRepository<Models.Wallet.Wallet>, IWalletRepository
    {
        // _context, 'GenericRepository'den 'protected' olarak gelir.

        public WalletRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<WalletTransaction>> GetRecentTransactionsAsync(string userId, int count = 20)
        {
            // _context.Set<T>() yerine doğrudan _context'i kullanabiliriz
            return await _context.WalletTransactions
                .Where(t => t.WalletUserId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PayoutRequest>> GetPayoutHistoryAsync(string userId, int count = 20)
        {
            return await _context.PayoutRequests
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.RequestedDate)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}