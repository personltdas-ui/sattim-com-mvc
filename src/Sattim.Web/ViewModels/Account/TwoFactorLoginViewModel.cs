using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Account
{
    public class TwoFactorLoginViewModel
    {
        [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
        [StringLength(7, ErrorMessage = "Kod 6-7 karakter olmalıdır.", MinimumLength = 6)]
        [Display(Name = "Doğrulama Kodu")]
        public string TwoFactorCode { get; set; }

        [Display(Name = "Bu tarayıcıyı hatırla")]
        public bool RememberMachine { get; set; }

        // Bu alanlar, Login sayfasından bu sayfaya taşınır
        public bool RememberMe { get; set; }
        public string ReturnUrl { get; set; }
    }
}