using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Order
{
    public class SubmitShippingViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Kargo firması adı zorunludur.")]
        [Display(Name = "Kargo Firması")]
        [StringLength(100)]
        public string Carrier { get; set; }

        [Required(ErrorMessage = "Kargo takip numarası zorunludur.")]
        [Display(Name = "Kargo Takip Numarası")]
        [StringLength(100)]
        public string TrackingNumber { get; set; }
    }
}