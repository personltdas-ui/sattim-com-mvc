namespace Sattim.Web.ViewModels.Wallet
{
    /// <summary>
    /// Kullanıcının "Cüzdanım" sayfasını doldurur.
    /// (GetWalletDashboardAsync tarafından döndürülür)
    /// </summary>
    public class WalletDashboardViewModel
    {
        public decimal CurrentBalance { get; set; }
        public List<WalletTransactionViewModel> RecentTransactions { get; set; } = new List<WalletTransactionViewModel>();
        public List<PayoutHistoryViewModel> PayoutHistory { get; set; } = new List<PayoutHistoryViewModel>();
    }
}
