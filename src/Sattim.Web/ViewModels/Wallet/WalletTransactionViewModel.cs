using Sattim.Web.Models.Wallet;

namespace Sattim.Web.ViewModels.Wallet
{
    /// <summary>
    /// WalletDashboardViewModel içinde kullanılan tek bir cüzdan işlemini temsil eder.
    /// </summary>
    public class WalletTransactionViewModel
    {
        public decimal Amount { get; set; } // Pozitif (Giriş) veya Negatif (Çıkış)
        public WalletTransactionType Type { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
