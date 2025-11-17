using Sattim.Web.Models.Wallet;
using Sattim.Web.Services.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Repositories
{
    /// <summary>
    /// Wallet varlığı için jenerik repository'ye EK OLARAK
    /// 'Cüzdan Paneli' için gereken işlemleri (transactions)
    /// ve para çekme taleplerini (payouts) getiren metotları sağlar.
    /// </summary>
    public interface IWalletRepository : IGenericRepository<Models.Wallet.Wallet>
    {
        /// <summary>
        /// Bir kullanıcının son (örn: 20) cüzdan işlemini (transactions) getirir.
        /// </summary>
        Task<List<WalletTransaction>> GetRecentTransactionsAsync(string userId, int count = 20);

        /// <summary>
        /// Bir kullanıcının son (örn: 20) para çekme talebi geçmişini (payouts) getirir.
        /// </summary>
        Task<List<PayoutRequest>> GetPayoutHistoryAsync(string userId, int count = 20);
    }
}