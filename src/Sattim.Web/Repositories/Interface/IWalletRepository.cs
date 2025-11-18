using Sattim.Web.Models.Wallet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IWalletRepository : IGenericRepository<Wallet>
    {
        Task<List<WalletTransaction>> GetRecentTransactionsAsync(string userId, int count = 20);

        Task<List<PayoutRequest>> GetPayoutHistoryAsync(string userId, int count = 20);
    }
}