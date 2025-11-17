using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Profile
{
    public class AddressViewModel
    {
        [Required(ErrorMessage = "Adres başlığı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Adres Başlığı (Örn: Ev, Ofis)")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Ad ve Soyad zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Adres zorunludur.")]
        [StringLength(500)]
        [Display(Name = "Adres Satırı")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Şehir zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Şehir")]
        public string City { get; set; }

        [Required(ErrorMessage = "Posta kodu zorunludur.")]
        [StringLength(20)]
        [Display(Name = "Posta Kodu")]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [Phone(ErrorMessage = "Geçersiz telefon numarası.")]
        [StringLength(20)]
        [Display(Name = "Telefon Numarası")]
        public string Phone { get; set; }
    }
}
