using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Category;
using Sattim.Web.Models.UI;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.ViewModels.Category;
using Sattim.Web.ViewModels.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Content
{
    public class ContentService : IContentService
    {
        private readonly IGenericRepository<SiteSettings> _settingsRepo;
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly IBlogPostRepository _blogPostRepo;
        private readonly ITagRepository _tagRepo;
        private readonly IGenericRepository<BlogPostTag> _blogPostTagRepo;
        private readonly IGenericRepository<FAQ> _faqRepo;
        private readonly IGenericRepository<Banner> _bannerRepo;
        private readonly IGenericRepository<EmailTemplate> _emailTemplateRepo;
        private readonly IGenericRepository<Newsletter> _newsletterRepo;

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ContentService> _logger;

        public ContentService(
          IGenericRepository<SiteSettings> settingsRepo,
          IGenericRepository<Category> categoryRepo,
          IBlogPostRepository blogPostRepo,
          ITagRepository tagRepo,
          IGenericRepository<BlogPostTag> blogPostTagRepo,
          IGenericRepository<FAQ> faqRepo,
          IGenericRepository<Banner> bannerRepo,
          IGenericRepository<EmailTemplate> emailTemplateRepo,
          IGenericRepository<Newsletter> newsletterRepo,
          ApplicationDbContext context,
          IMapper mapper,
          ILogger<ContentService> logger)
        {
            _settingsRepo = settingsRepo;
            _categoryRepo = categoryRepo;
            _blogPostRepo = blogPostRepo;
            _tagRepo = tagRepo;
            _blogPostTagRepo = blogPostTagRepo;
            _faqRepo = faqRepo;
            _bannerRepo = bannerRepo;
            _emailTemplateRepo = emailTemplateRepo;
            _newsletterRepo = newsletterRepo;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<SiteSettingGroupViewModel>> GetSiteSettingsAsync()
        {
            var allSettings = await _settingsRepo.GetAllAsync();

            return allSettings
              .GroupBy(s => s.Category)
              .Select(g => new SiteSettingGroupViewModel
              {
                  Category = g.Key,
                  Settings = _mapper.Map<List<SiteSettingUpdateViewModel>>(g.ToList())
              })
              .OrderBy(g => g.Category.ToString())
              .ToList();
        }

        public async Task<bool> UpdateSiteSettingsAsync(List<SiteSettingUpdateViewModel> model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingSettings = (await _settingsRepo.GetAllAsync())
                             .ToDictionary(s => s.Key, s => s);

                foreach (var item in model)
                {
                    if (existingSettings.TryGetValue(item.Key, out var setting))
                    {
                        setting.UpdateSetting(item.Value, item.Description);
                        _settingsRepo.Update(setting);
                    }
                    else
                    {
                        _logger.LogWarning($"Site ayarı güncellenemedi: '{item.Key}' anahtarı veritabanında bulunamadı.");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Site ayarları başarıyla güncellendi (Commit).");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Site ayarları güncellenirken kritik hata (Rollback).");
                return false;
            }
        }

        public async Task<List<CategoryViewModel>> GetCategoryTreeAsync()
        {
            var allCategories = await _categoryRepo.GetAllAsync();
            var lookup = allCategories.ToDictionary(c => c.Id);

            var viewModels = _mapper.Map<List<CategoryViewModel>>(allCategories);
            var tree = new List<CategoryViewModel>();

            foreach (var item in viewModels)
            {
                if (item.ParentCategoryId.HasValue && lookup.ContainsKey(item.ParentCategoryId.Value))
                {
                    var parent = viewModels.First(p => p.Id == item.ParentCategoryId.Value);
                    parent.SubCategories.Add(item);
                }
                else
                {
                    tree.Add(item);
                }
            }
            return tree;
        }

        public async Task<Models.Category.Category> GetCategoryByIdAsync(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) throw new KeyNotFoundException("Kategori bulunamadı.");
            return category;
        }

        public async Task<bool> CreateCategoryAsync(CategoryFormViewModel model)
        {
            try
            {
                if (await _categoryRepo.AnyAsync(c => c.Slug == model.Slug))
                {
                    _logger.LogWarning($"Kategori oluşturulamadı: '{model.Slug}' slug'ı zaten kullanılıyor.");
                    return false;
                }

                var category = new Category(
                  model.Name,
                  model.Slug,
                  model.ParentCategoryId,
                  model.Description,
                  model.ImageUrl
                );

                if (!model.IsActive) category.Deactivate();

                await _categoryRepo.AddAsync(category);
                await _categoryRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori oluşturulurken hata.");
                return false;
            }
        }

        public async Task<bool> UpdateCategoryAsync(int id, CategoryFormViewModel model)
        {
            try
            {
                var category = await GetCategoryByIdAsync(id);

                if (category.Slug != model.Slug && await _categoryRepo.AnyAsync(c => c.Slug == model.Slug && c.Id != id))
                {
                    _logger.LogWarning($"Kategori güncellenemedi: '{model.Slug}' slug'ı zaten kullanılıyor.");
                    return false;
                }

                category.UpdateDetails(model.Name, model.Slug, model.Description);
                category.ChangeParent(model.ParentCategoryId);
                category.UpdateImageUrl(model.ImageUrl);
                if (model.IsActive) category.Activate();
                else category.Deactivate();

                _categoryRepo.Update(category);
                await _categoryRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori (ID: {id}) güncellenirken hata.");
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await GetCategoryByIdAsync(id);

                _categoryRepo.Remove(category);
                await _categoryRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogWarning(dbEx, $"Kategori (ID: {id}) silinemedi (İlişkili veri var).");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori (ID: {id}) silinirken hata.");
                return false;
            }
        }

        public async Task<List<BlogPostSummaryViewModel>> GetBlogPostsAsync()
        {
            var posts = await _blogPostRepo.GetAllPostsForAdminAsync();
            return _mapper.Map<List<BlogPostSummaryViewModel>>(posts);
        }

        public async Task<BlogPostFormViewModel> GetBlogPostForEditAsync(int id)
        {
            var post = await _blogPostRepo.GetPostForEditAsync(id);
            if (post == null) throw new KeyNotFoundException("Blog yazısı bulunamadı.");
            return _mapper.Map<BlogPostFormViewModel>(post);
        }

        public async Task<int> CreateBlogPostAsync(BlogPostFormViewModel model, string authorId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _blogPostRepo.AnyAsync(p => p.Slug == model.Slug))
                    throw new InvalidOperationException($"'{model.Slug}' slug'ı zaten kullanılıyor.");

                var post = new BlogPost(
                  model.Title,
                  model.Slug,
                  model.Content,
                  authorId,
                  model.Excerpt
                );
                post.UpdateFeaturedImage(model.FeaturedImage);

                if (model.Status == BlogPostStatus.Published) post.Publish();
                else if (model.Status == BlogPostStatus.Archived) post.Archive();

                await _blogPostRepo.AddAsync(post);
                await _context.SaveChangesAsync();

                await ProcessTagsAsync(post, model.CommaSeparatedTags);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation($"Yeni blog yazısı oluşturuldu (ID: {post.Id})");
                return post.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Blog yazısı oluşturulurken kritik hata (Rollback).");
                return 0;
            }
        }

        public async Task<bool> UpdateBlogPostAsync(int id, BlogPostFormViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var post = await _blogPostRepo.GetPostForEditAsync(id);
                if (post == null) throw new KeyNotFoundException("Blog yazısı bulunamadı.");

                if (post.Slug != model.Slug && await _blogPostRepo.AnyAsync(p => p.Slug == model.Slug && p.Id != id))
                    throw new InvalidOperationException($"'{model.Slug}' slug'ı zaten kullanılıyor.");

                post.Update(model.Title, model.Slug, model.Content, model.Excerpt);
                post.UpdateFeaturedImage(model.FeaturedImage);

                if (model.Status == BlogPostStatus.Published) post.Publish();
                else if (model.Status == BlogPostStatus.Archived) post.Archive();
                else post.SetAsDraft();

                await ProcessTagsAsync(post, model.CommaSeparatedTags);

                _blogPostRepo.Update(post);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation($"Blog yazısı güncellendi (ID: {post.Id})");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Blog yazısı güncellenirken kritik hata (ID: {id}, Rollback).");
                return false;
            }
        }

        public async Task<bool> DeleteBlogPostAsync(int id)
        {
            try
            {
                var post = await _blogPostRepo.GetByIdAsync(id);
                if (post == null) return false;

                _blogPostRepo.Remove(post);
                await _blogPostRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Blog yazısı silinirken hata (ID: {id}).");
                return false;
            }
        }

        public async Task<List<TagViewModel>> GetTagsAsync()
        {
            var tags = await _tagRepo.GetAllTagsWithPostCountAsync();
            return _mapper.Map<List<TagViewModel>>(tags);
        }

        public async Task<bool> CreateTagAsync(TagViewModel model)
        {
            try
            {
                if (await _tagRepo.AnyAsync(t => t.Slug == model.Slug))
                    return false;

                var tag = new Tag(model.Name, model.Slug);
                await _tagRepo.AddAsync(tag);
                await _tagRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etiket (Tag) oluşturulurken hata.");
                return false;
            }
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            try
            {
                var tag = await _tagRepo.GetByIdAsync(id);
                if (tag == null) return false;

                _tagRepo.Remove(tag);
                await _tagRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogWarning(dbEx, $"Etiket (ID: {id}) silinemedi (İlişkili yazı var).");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Etiket (ID: {id}) silinirken hata.");
                return false;
            }
        }

        private async Task ProcessTagsAsync(BlogPost post, string commaSeparatedTags)
        {
            post.BlogPostTags.Clear();

            if (string.IsNullOrWhiteSpace(commaSeparatedTags))
                return;

            var tagNames = commaSeparatedTags.Split(',')
              .Select(t => t.Trim().ToLower())
              .Where(t => !string.IsNullOrEmpty(t))
              .Distinct();

            foreach (var tagName in tagNames)
            {
                string tagSlug = tagName.Replace(" ", "-");
                var tag = await _tagRepo.FirstOrDefaultAsync(t => t.Slug == tagSlug);

                if (tag == null)
                {
                    tag = new Tag(tagName, tagSlug);
                    await _tagRepo.AddAsync(tag);

                    await _context.SaveChangesAsync();
                }

                post.BlogPostTags.Add(new BlogPostTag(post.Id, tag.Id));
            }
        }

        public async Task<List<FAQ>> GetFAQsAsync() => (await _faqRepo.GetAllAsync()).ToList();

        public async Task<FAQ> GetFAQByIdAsync(int id)
        {
            var faq = await _faqRepo.GetByIdAsync(id);
            if (faq == null) throw new KeyNotFoundException("SSS bulunamadı.");
            return faq;
        }

        public async Task<bool> CreateFAQAsync(FaqFormViewModel model)
        {
            try
            {
                var faq = _mapper.Map<FAQ>(model);
                await _faqRepo.AddAsync(faq);
                await _faqRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSS oluşturulurken hata.");
                return false;
            }
        }

        public async Task<bool> UpdateFAQAsync(int id, FaqFormViewModel model)
        {
            try
            {
                var faq = await GetFAQByIdAsync(id);
                _mapper.Map(model, faq);
                _faqRepo.Update(faq);
                await _faqRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SSS güncellenirken hata (ID: {id}).");
                return false;
            }
        }

        public async Task<bool> DeleteFAQAsync(int id)
        {
            try
            {
                var faq = await GetFAQByIdAsync(id);
                _faqRepo.Remove(faq);
                await _faqRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SSS silinirken hata (ID: {id}).");
                return false;
            }
        }

        public async Task<List<Banner>> GetBannersAsync() => (await _bannerRepo.GetAllAsync()).ToList();

        public async Task<Banner> GetBannerByIdAsync(int id)
        {
            var banner = await _bannerRepo.GetByIdAsync(id);
            if (banner == null) throw new KeyNotFoundException("Banner bulunamadı.");
            return banner;
        }

        public async Task<bool> CreateBannerAsync(BannerFormViewModel model)
        {
            try
            {
                var banner = _mapper.Map<Banner>(model);
                await _bannerRepo.AddAsync(banner);
                await _bannerRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Banner oluşturulurken hata.");
                return false;
            }
        }

        public async Task<bool> UpdateBannerAsync(int id, BannerFormViewModel model)
        {
            try
            {
                var banner = await GetBannerByIdAsync(id);
                _mapper.Map(model, banner);
                _bannerRepo.Update(banner);
                await _bannerRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Banner güncellenirken hata (ID: {id}).");
                return false;
            }
        }

        public async Task<bool> DeleteBannerAsync(int id)
        {
            try
            {
                var banner = await GetBannerByIdAsync(id);
                _bannerRepo.Remove(banner);
                await _bannerRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Banner silinirken hata (ID: {id}).");
                return false;
            }
        }

        public async Task<List<EmailTemplate>> GetEmailTemplatesAsync() =>
          (await _emailTemplateRepo.GetAllAsync()).ToList();

        public async Task<EmailTemplate> GetEmailTemplateByIdAsync(int id)
        {
            var template = await _emailTemplateRepo.GetByIdAsync(id);
            if (template == null) throw new KeyNotFoundException("E-posta şablonu bulunamadı.");
            return template;
        }

        public async Task<bool> CreateEmailTemplateAsync(EmailTemplateFormViewModel model)
        {
            try
            {
                if (await _emailTemplateRepo.AnyAsync(t => t.Name == model.Name))
                {
                    _logger.LogWarning($"E-posta şablonu oluşturulamadı: '{model.Name}' adı zaten kullanılıyor.");
                    return false;
                }

                var template = _mapper.Map<EmailTemplate>(model);
                await _emailTemplateRepo.AddAsync(template);
                await _emailTemplateRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta şablonu oluşturulurken hata.");
                return false;
            }
        }

        public async Task<bool> UpdateEmailTemplateAsync(int id, EmailTemplateFormViewModel model)
        {
            try
            {
                var template = await GetEmailTemplateByIdAsync(id);

                template.UpdateTemplate(model.Subject, model.Body);
                if (model.IsActive) template.Activate();
                else template.Deactivate();

                _emailTemplateRepo.Update(template);
                await _emailTemplateRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"E-posta şablonu güncellenirken hata (ID: {id}).");
                return false;
            }
        }

        public async Task<bool> DeleteEmailTemplateAsync(int id)
        {
            try
            {
                var template = await GetEmailTemplateByIdAsync(id);
                _emailTemplateRepo.Remove(template);
                await _emailTemplateRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"E-posta şablonu silinirken hata (ID: {id}).");
                return false;
            }
        }

        public async Task<List<string>> GetActiveNewsletterSubscribersAsync()
        {
            var subscribers = await _newsletterRepo.FindAsync(
              n => n.IsActive == true
            );

            return subscribers.Select(n => n.Email).ToList();
        }
    }
}