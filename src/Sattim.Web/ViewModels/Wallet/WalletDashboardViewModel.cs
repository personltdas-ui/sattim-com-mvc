using System.Collections.Generic;

namespace Sattim.Web.ViewModels.Wallet
{
    public class WalletDashboardViewModel
    {
        public decimal CurrentBalance { get; set; }
        public List<WalletTransactionViewModel> RecentTransactions { get; set; } = new List<WalletTransactionViewModel>();
        public List<PayoutHistoryViewModel> PayoutHistory { get; set; } = new List<PayoutHistoryViewModel>();
    }
}