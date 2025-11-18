using Sattim.Web.Models.Blog;
using Sattim.Web.Models.UI;
using Sattim.Web.ViewModels.Category;
using Sattim.Web.ViewModels.Content;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Content
{
    public interface IContentService
    {
        Task<List<SiteSettingGroupViewModel>> GetSiteSettingsAsync();
        Task<bool> UpdateSiteSettingsAsync(List<SiteSettingUpdateViewModel> model);

        Task<List<CategoryViewModel>> GetCategoryTreeAsync();
        Task<Models.Category.Category> GetCategoryByIdAsync(int id);
        Task<bool> CreateCategoryAsync(CategoryFormViewModel model);
        Task<bool> UpdateCategoryAsync(int id, CategoryFormViewModel model);
        Task<bool> DeleteCategoryAsync(int id);

        Task<List<BlogPostSummaryViewModel>> GetBlogPostsAsync();
        Task<BlogPostFormViewModel> GetBlogPostForEditAsync(int id);
        Task<int> CreateBlogPostAsync(BlogPostFormViewModel model, string authorId);
        Task<bool> UpdateBlogPostAsync(int id, BlogPostFormViewModel model);
        Task<bool> DeleteBlogPostAsync(int id);
        Task<List<TagViewModel>> GetTagsAsync();
        Task<bool> CreateTagAsync(TagViewModel model);
        Task<bool> DeleteTagAsync(int id);

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

        Task<List<EmailTemplate>> GetEmailTemplatesAsync();
        Task<EmailTemplate> GetEmailTemplateByIdAsync(int id);
        Task<bool> CreateEmailTemplateAsync(EmailTemplateFormViewModel model);
        Task<bool> UpdateEmailTemplateAsync(int id, EmailTemplateFormViewModel model);
        Task<bool> DeleteEmailTemplateAsync(int id);

        Task<List<string>> GetActiveNewsletterSubscribersAsync();
    }
}