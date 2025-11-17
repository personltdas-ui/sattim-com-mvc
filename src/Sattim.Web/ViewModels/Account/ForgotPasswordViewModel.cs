using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçersiz e-posta adresi.")]
        [Display(Name = "E-posta Adresiniz")]
        public string Email { get; set; }
    }
}