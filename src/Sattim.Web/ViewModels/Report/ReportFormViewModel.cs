using Sattim.Web.Models.Analytical;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Report
{
    public class ReportFormViewModel
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "Şikayet Edilen ID")]
        public string EntityId { get; set; }

        [Required(ErrorMessage = "Şikayet tipi zorunludur.")]
        [Display(Name = "Şikayet Tipi")]
        public ReportEntityType EntityType { get; set; }

        [Required(ErrorMessage = "Bir sebep seçmelisiniz.")]
        [Display(Name = "Şikayet Sebebi")]
        public ReportReason Reason { get; set; }

        [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Açıklamanız en az 10 karakter olmalıdır.")]
        [Display(Name = "Açıklama (Detay)")]
        public string Description { get; set; }
    }
}