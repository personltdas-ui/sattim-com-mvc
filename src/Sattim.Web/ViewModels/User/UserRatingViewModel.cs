using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.User
{
    public class UserRatingViewModel
    {
        public int Id { get; set; }
        public string RatedUserId { get; set; }
        public string RatedUserName { get; set; }
        public string RaterUserId { get; set; }
        public string RaterUserName { get; set; }

        [Required(ErrorMessage = "Ürün seçimi zorunludur")]
        public int ProductId { get; set; }
        public string? ProductTitle { get; set; }

        [Required(ErrorMessage = "Puan zorunludur")]
        [Range(1, 5, ErrorMessage = "Puan 1-5 arasında olmalıdır")]
        [Display(Name = "Puan")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Yorum en fazla 500 karakter olabilir")]
        [Display(Name = "Yorum")]
        public string? Comment { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
