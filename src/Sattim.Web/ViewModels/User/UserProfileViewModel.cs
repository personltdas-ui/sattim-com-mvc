using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.User
{
    public class UserProfileViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }

        [Phone]
        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Display(Name = "Adres")]
        public string? Address { get; set; }

        [Display(Name = "Şehir")]
        public string? City { get; set; }

        [Display(Name = "Ülke")]
        public string? Country { get; set; }

        [Display(Name = "Posta Kodu")]
        public string? PostalCode { get; set; }

        [Display(Name = "Hakkımda")]
        [StringLength(500)]
        public string? Bio { get; set; }

        public string? ProfileImageUrl { get; set; }
        public string? IdCardImageUrl { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public int RatingCount { get; set; }
        public double AverageRating { get; set; }

        public IFormFile? ProfileImage { get; set; }
        public IFormFile? IdCardImage { get; set; }
    }
}
