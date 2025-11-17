using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Order
{
    public class SubmitShippingViewModel
    {
        /// <summary>
        /// Hangi ürünün (Escrow) kargolandığını belirten ID.
        /// Bu, formda 'hidden' (gizli) bir input olarak tutulmalıdır.
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Kargo firması adı zorunludur.")]
        [Display(Name = "Kargo Firması")]
        [StringLength(100)]
        public string Carrier { get; set; } // Örn: MNG Kargo, Yurtiçi Kargo

        [Required(ErrorMessage = "Kargo takip numarası zorunludur.")]
        [Display(Name = "Kargo Takip Numarası")]
        [StringLength(100)]
        public string TrackingNumber { get; set; }
    }
}