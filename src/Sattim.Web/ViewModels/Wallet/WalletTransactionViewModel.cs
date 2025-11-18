using Sattim.Web.Models.Wallet;
using System;

namespace Sattim.Web.ViewModels.Wallet
{
    public class WalletTransactionViewModel
    {
        public decimal Amount { get; set; }
        public WalletTransactionType Type { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}