using Sattim.Web.Models.Analytical; // Enum'lar için
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Report
{
    /// <summary>
    /// CreateReportAsync metodu için şikayet formu verisi.
    /// </summary>
    public class ReportFormViewModel
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "Şikayet Edilen ID")]
        public string EntityId { get; set; } // Örn: ProductId="123", UserId="abc-xyz"

        [Required(ErrorMessage = "Şikayet tipi zorunludur.")]
        [Display(Name = "Şikayet Tipi")]
        public ReportEntityType EntityType { get; set; } // Product, User, Bid, Message

        [Required(ErrorMessage = "Bir sebep seçmelisiniz.")]
        [Display(Name = "Şikayet Sebebi")]
        public ReportReason Reason { get; set; }

        [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Açıklamanız en az 10 karakter olmalıdır.")]
        [Display(Name = "Açıklama (Detay)")]
        public string Description { get; set; }
    }
}