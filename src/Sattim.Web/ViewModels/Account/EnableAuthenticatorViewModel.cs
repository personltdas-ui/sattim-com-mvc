using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Account
{
    // 2FA Kurulum (QR Kod) sayfasını gösterir
    public class EnableAuthenticatorViewModel
    {
        public string SharedKey { get; set; }
        public string AuthenticatorUri { get; set; }

        [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
        [Display(Name = "Doğrulama Kodu")]
        public string VerificationCode { get; set; }
    }
}