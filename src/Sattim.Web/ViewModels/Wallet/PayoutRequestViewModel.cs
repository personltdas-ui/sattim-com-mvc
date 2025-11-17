using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Wallet
{
    /// <summary>
    /// RequestPayoutAsync metodu için para çekme formu verisi.
    /// </summary>
    public class PayoutRequestViewModel
    {
        [Required(ErrorMessage = "Tutar zorunludur.")]
        [Range(10, double.MaxValue, ErrorMessage = "Minimum 10 TL çekebilirsiniz.")] // Örn: Min 10 TL
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Banka adı zorunludur.")]
        [StringLength(100)]
        public string BankName { get; set; }

        [Required(ErrorMessage = "Hesap sahibi adı zorunludur.")]
        [StringLength(150)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "IBAN zorunludur.")]
        [StringLength(34, MinimumLength = 26, ErrorMessage = "Geçerli bir IBAN girin (örn: TR...).")]
        public string IBAN { get; set; }
    }
}
