using Sattim.Web.Models.Wallet;

namespace Sattim.Web.ViewModels.Wallet
{
    /// <summary>
    /// WalletDashboardViewModel içinde kullanılan tek bir para çekme talebi geçmişini temsil eder.
    /// </summary>
    public class PayoutHistoryViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public PayoutStatus Status { get; set; }
        public DateTime RequestedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string IBAN { get; set; } // (Maskelenmiş olmalı)
    }
}
