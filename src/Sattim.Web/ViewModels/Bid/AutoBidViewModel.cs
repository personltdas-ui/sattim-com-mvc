using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Bid
{
    public class AutoBidViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Maksimum tutar zorunludur.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335",ErrorMessage ="Tutar pozitif olmalı")]
        [Display(Name = "Verebileceğim Maksimum Tutar")]
        public decimal MaxAmount { get; set; }

        [Required(ErrorMessage = "Artış tutarı zorunludur.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Tutar pozitif olmalıdır")]
        [Display(Name = "Her Seferinde Artır")]
        public decimal IncrementAmount { get; set; }
    }
}
