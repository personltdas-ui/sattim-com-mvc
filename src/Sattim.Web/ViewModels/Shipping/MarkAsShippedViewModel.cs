using Sattim.Web.Models.Shipping;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Shipping
{
    public class MarkAsShippedViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Kargo firması zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Kargo Firması")]
        public string Carrier { get; set; }

        [Required(ErrorMessage = "Takip numarası zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Kargo Takip Numarası")]
        public string TrackingNumber { get; set; }
    }
}