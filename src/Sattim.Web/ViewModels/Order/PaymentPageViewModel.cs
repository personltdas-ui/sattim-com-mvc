using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Order
{
    public class PaymentPageViewModel
    {
        public OrderDetailViewModel OrderDetails { get; set; }
        public OrderPaymentViewModel PaymentForm { get; set; }

        public decimal AvailableWalletBalance { get; set; }
    }
}