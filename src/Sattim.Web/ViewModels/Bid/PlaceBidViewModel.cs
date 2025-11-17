using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Bid
{
    public class PlaceBidViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Teklif tutarı zorunludur.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335",ErrorMessage ="Teklif tutarı pozitif olmalıdır.")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }
    }
}
