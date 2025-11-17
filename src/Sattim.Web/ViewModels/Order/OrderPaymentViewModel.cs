using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Order
{
    /// <summary>
    /// /Orders/Pay sayfasındaki ödeme formu.
    /// </summary>
    public class OrderPaymentViewModel
    {
        [Required]
        public int ProductId { get; set; } // Bu, Escrow ID'si ile aynıdır

        [Required(ErrorMessage = "Lütfen bir ödeme yöntemi seçin.")]
        public string PaymentMethod { get; set; } // "Wallet" veya "Gateway"
    }
}