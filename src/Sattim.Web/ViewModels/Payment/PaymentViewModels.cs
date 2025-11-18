using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Payment
{
    public class CheckoutViewModel
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public int PaymentId { get; set; }

        public string HtmlContent { get; set; }
        public string GatewayName { get; set; } = "Iyzico";
    }

    public class PaymentConfirmationViewModel
    {
        [Required]
        public int PaymentId { get; set; }

        [Required]
        public bool Success { get; set; }

        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? GatewayResponse { get; set; }
    }
}