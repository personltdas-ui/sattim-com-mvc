using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Order
{
    public class OrderPaymentViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Lütfen bir ödeme yöntemi seçin.")]
        public string PaymentMethod { get; set; }
    }
}