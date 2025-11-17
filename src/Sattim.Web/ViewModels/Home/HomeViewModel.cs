using Sattim.Web.ViewModels.Product;
using Sattim.Web.ViewModels.Blog;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Home
{
    /// <summary>
    /// Ana sayfanın (Index) ihtiyaç duyduğu tüm dinamik verileri
    /// tek bir modelde toplar.
    /// </summary>
    public class HomeViewModel
    {
        public List<ProductSummaryViewModel> NewestProducts { get; set; }
        public List<ProductSummaryViewModel> EndingSoonProducts { get; set; }
        public List<BlogSummaryViewModel> RecentBlogPosts { get; set; }
    }

    /// <summary>
    /// /Home/Contact sayfasındaki iletişim formu için.
    /// </summary>
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Adınız zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Adınız Soyadınız")]
        public string Name { get; set; }

        [Required(ErrorMessage = "E-posta adresiniz zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Konu zorunludur.")]
        [StringLength(100)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Mesajınız zorunludur.")]
        [StringLength(2000, MinimumLength = 10)]
        public string Message { get; set; }
    }
}