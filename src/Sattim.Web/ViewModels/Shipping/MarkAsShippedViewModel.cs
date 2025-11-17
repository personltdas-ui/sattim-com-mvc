using Sattim.Web.Models.Shipping; // ShippingStatus enum'u için
using System;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Shipping
{
    /// <summary>
    /// MarkAsShippedAsync metodu için satıcının kargo formunu temsil eder.
    /// </summary>
    public class MarkAsShippedViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Kargo firması zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Kargo Firması")]
        public string Carrier { get; set; } // Örn: "Yurtiçi Kargo"

        [Required(ErrorMessage = "Takip numarası zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Kargo Takip Numarası")]
        public string TrackingNumber { get; set; }
    }

    
}