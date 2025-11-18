using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Manage
{
    public class EnableAuthenticatorViewModel
    {
        
        [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
        [Display(Name = "Doğrulama Kodu")]
        public string Code { get; set; }

        
        public string SharedKey { get; set; } 
        public string AuthenticatorUri { get; set; }
    }
}
