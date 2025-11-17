using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Account
{
    public class RecoveryCodeLoginViewModel
    {
        [Required(ErrorMessage = "Kurtarma kodu zorunludur.")]
        [Display(Name = "Kurtarma Kodu")]
        public string RecoveryCode { get; set; }
        public string ReturnUrl { get; set; }

        public bool RememberMe { get; set; }
    }
}
