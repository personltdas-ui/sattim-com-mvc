using Sattim.Web.Models.Wallet;
using System;

namespace Sattim.Web.ViewModels.Wallet
{
    public class PayoutHistoryViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public PayoutStatus Status { get; set; }
        public DateTime RequestedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string IBAN { get; set; }
    }
}