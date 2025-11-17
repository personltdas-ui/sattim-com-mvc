using Sattim.Web.Models.Blog; // Enum'lar için
using Sattim.Web.Models.UI; // Enum'lar için
using Sattim.Web.ViewModels.Category; // CategoryViewModel
using Sattim.Web.ViewModels.Content; // Gerekli ViewModel'lar
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Content
{
    // BU SERVİSİN TAMAMI [Authorize(Roles = "Admin")] İLE KORUNMALIDIR
    public interface IContentService
    {
        // ====================================================================
        //  1. SİTE AYARLARI (SiteSettings)
        // ====================================================================

        /// <summary>
        /// Admin panelindeki "Site Ayarları" sayfasını doldurur.
        /// </summary>
        Task<List<SiteSettingGroupViewModel>> GetSiteSettingsAsync();

        /// <summary>
        /// "Site Ayarları" formundan gelen tüm ayarları toplu olarak günceller.
        /// </summary>
        Task<bool> UpdateSiteSettingsAsync(List<SiteSettingUpdateViewModel> model);

        // ====================================================================
        //  2. KATEGORİ YÖNETİMİ (Category)
        // ====================================================================

        // (IProductService'teki GetCategoriesAsync'e benzer,
        // ancak bu, admin paneli için (örn: pasif olanları da gösterir))
        Task<List<CategoryViewModel>> GetCategoryTreeAsync();
        Task<Models.Category.Category> GetCategoryByIdAsync(int id);
        Task<bool> CreateCategoryAsync(CategoryFormViewModel model);
        Task<bool> UpdateCategoryAsync(int id, CategoryFormViewModel model);
        Task<bool> DeleteCategoryAsync(int id);

        // ====================================================================
        //  3. BLOG YÖNETİMİ (BlogPost, Tag)
        // ====================================================================

        Task<List<BlogPostSummaryViewModel>> GetBlogPostsAsync();
        Task<BlogPostFormViewModel> GetBlogPostForEditAsync(int id);
        Task<int> CreateBlogPostAsync(BlogPostFormViewModel model, string authorId);
        Task<bool> UpdateBlogPostAsync(int id, BlogPostFormViewModel model);
        Task<bool> DeleteBlogPostAsync(int id);
        Task<List<TagViewModel>> GetTagsAsync();
        Task<bool> CreateTagAsync(TagViewModel model);
        Task<bool> DeleteTagAsync(int id);

        // ====================================================================
        //  4. ARAYÜZ (UI) YÖNETİMİ (FAQ, Banner)
        // ====================================================================

        Task<List<FAQ>> GetFAQsAsync();
        Task<FAQ> GetFAQByIdAsync(int id);
        Task<bool> CreateFAQAsync(FaqFormViewModel model);
        Task<bool> UpdateFAQAsync(int id, FaqFormViewModel model);
        Task<bool> DeleteFAQAsync(int id);
        Task<List<Banner>> GetBannersAsync();
        Task<Banner> GetBannerByIdAsync(int id);
        Task<bool> CreateBannerAsync(BannerFormViewModel model);
        Task<bool> UpdateBannerAsync(int id, BannerFormViewModel model);
        Task<bool> DeleteBannerAsync(int id);

        // ====================================================================
        //  5. E-POSTA ŞABLONU YÖNETİMİ (EmailTemplate)
        // ====================================================================

        Task<List<EmailTemplate>> GetEmailTemplatesAsync();
        Task<EmailTemplate> GetEmailTemplateByIdAsync(int id);
        Task<bool> CreateEmailTemplateAsync(EmailTemplateFormViewModel model);
        Task<bool> UpdateEmailTemplateAsync(int id, EmailTemplateFormViewModel model);
        Task<bool> DeleteEmailTemplateAsync(int id);

        // ====================================================================
        //  6. BÜLTEN (Newsletter) YÖNETİMİ
        // ====================================================================

        /// <summary>
        /// Bültene abone olan tüm aktif e-postaların listesini döndürür
        /// (örn: CSV olarak dışa aktarmak için).
        /// </summary>
        Task<List<string>> GetActiveNewsletterSubscribersAsync();
    }
}