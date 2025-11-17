using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Content;
using Sattim.Web.ViewModels.Category; // CategoryViewModel için
using Sattim.Web.ViewModels.Content;
using System.Collections.Generic;
using System.Linq;
using System.Text; // CSV Export için
using System.Threading.Tasks;

namespace Sattim.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Bu bölüm tamamen Admin'e özel
    public class ContentController : Controller
    {
        private readonly IContentService _contentService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContentController(IContentService contentService,
                                 UserManager<ApplicationUser> userManager)
        {
            _contentService = contentService;
            _userManager = userManager;
        }

        // Mevcut admin/yazar ID'sini almak için yardımcı metot
        private string GetCurrentAdminId() => _userManager.GetUserId(User);

        #region 1. Site Ayarları (Site Settings)

        /// <summary>
        /// Sitenin genel ayarlarını yönetme sayfası.
        /// Rota: /Admin/Content/SiteSettings
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SiteSettings()
        {
            var model = await _contentService.GetSiteSettingsAsync();
            return View(model);
        }

        /// <summary>
        /// Site ayarlarını günceller.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Parametreyi formdan gelen gruplu modelle (List<SiteSettingGroupViewModel>) eşleştirin
        public async Task<IActionResult> SiteSettings(List<SiteSettingGroupViewModel> model)
        {
            if (model == null || !model.Any())
            {
                // Forma geri yönlendir
                return RedirectToAction(nameof(SiteSettings));
            }

            // 1. Gruplanmış modeli (List<Grup> -> List<Ayar>) 
            //    servisin beklediği düz listeye (List<Ayar>) çevir.
            var settingsToUpdate = model.SelectMany(g => g.Settings).ToList();

            // 2. Servisi düz listeyle çağır.
            await _contentService.UpdateSiteSettingsAsync(settingsToUpdate);

            TempData["SuccessMessage"] = "Site ayarları başarıyla güncellendi.";
            return RedirectToAction(nameof(SiteSettings));
        }

        #endregion

        #region 2. Kategori Yönetimi (Category)

        /// <summary>
        /// Ürün/Blog kategorilerini listeler.
        /// Rota: /Admin/Content/Categories
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var model = await _contentService.GetCategoryTreeAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCategory()
        {
            // YENİ EKLENDİ: Dropdown için kategori listesini çek
            var categories = await _contentService.GetCategoryTreeAsync();
            // (Servis ağaç döndürse bile, 'GetCategoryTreeAsync' muhtemelen 
            // 'CategoryViewModel' listesi döndürür, bunu dropdown'a çevirebiliriz)
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name");

            return View(new CategoryFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _contentService.CreateCategoryAsync(model);
            TempData["SuccessMessage"] = "Kategori başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _contentService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound();

            // YENİ EKLENDİ: Dropdown için kategori listesini çek
            var categories = await _contentService.GetCategoryTreeAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name", category.ParentCategoryId);

            // Servis Domain Model (Category) döndürüyor,
            // bizim Form ViewModel'e (CategoryFormViewModel) dönüştürmemiz gerekiyor.
            var model = new CategoryFormViewModel
            {
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                ParentCategoryId = category.ParentCategoryId
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _contentService.UpdateCategoryAsync(id, model);
            TempData["SuccessMessage"] = "Kategori başarıyla güncellendi.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            await _contentService.DeleteCategoryAsync(id);
            TempData["SuccessMessage"] = "Kategori silindi.";
            return RedirectToAction(nameof(Categories));
        }

        #endregion

        #region 3. Blog Yönetimi (Blog & Tags)

        /// <summary>
        /// Tüm blog yazılarını listeler.
        /// Rota: /Admin/Content/BlogPosts
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BlogPosts()
        {
            var model = await _contentService.GetBlogPostsAsync();
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateBlogPost()
        {
            return View(new BlogPostFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlogPost(BlogPostFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var authorId = GetCurrentAdminId();
            int newPostId = await _contentService.CreateBlogPostAsync(model, authorId);
            TempData["SuccessMessage"] = "Blog yazısı başarıyla oluşturuldu.";
            // Edit sayfasına yönlendir, çünkü 'Edit'te 'Tags' gibi detaylar olabilir
            return RedirectToAction(nameof(EditBlogPost), new { id = newPostId });
        }

        [HttpGet]
        public async Task<IActionResult> EditBlogPost(int id)
        {
            // Servis zaten doğru ViewModel'i (BlogPostFormViewModel) döndürüyor.
            var model = await _contentService.GetBlogPostForEditAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlogPost(int id, BlogPostFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _contentService.UpdateBlogPostAsync(id, model);
            TempData["SuccessMessage"] = "Blog yazısı başarıyla güncellendi.";
            return RedirectToAction(nameof(BlogPosts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlogPost(int id)
        {
            await _contentService.DeleteBlogPostAsync(id);
            TempData["SuccessMessage"] = "Blog yazısı silindi.";
            return RedirectToAction(nameof(BlogPosts));
        }

        /// <summary>
        /// Blog etiketlerini yönetir.
        /// Rota: /Admin/Content/Tags
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Tags()
        {
            var model = await _contentService.GetTagsAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTag(TagViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _contentService.CreateTagAsync(model);
                TempData["SuccessMessage"] = "Yeni etiket eklendi.";
            }
            return RedirectToAction(nameof(Tags));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTag(int id)
        {
            await _contentService.DeleteTagAsync(id);
            TempData["SuccessMessage"] = "Etiket silindi.";
            return RedirectToAction(nameof(Tags));
        }

        #endregion

        #region 4. Arayüz Yönetimi (FAQ & Banner)

        // --- Sıkça Sorulan Sorular (FAQ) ---

        [HttpGet]
        public async Task<IActionResult> FAQs()
        {
            var model = await _contentService.GetFAQsAsync();
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateFAQ()
        {
            return View(new FaqFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFAQ(FaqFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            await _contentService.CreateFAQAsync(model);
            TempData["SuccessMessage"] = "Yeni SSS eklendi.";
            return RedirectToAction(nameof(FAQs));
        }

        [HttpGet]
        public async Task<IActionResult> EditFAQ(int id)
        {
            var faq = await _contentService.GetFAQByIdAsync(id);
            if (faq == null) return NotFound();

            // YENİ EKLENDİ: ID'yi forma gönderebilmek için ViewBag'e ata
            ViewBag.FaqId = id;

            // Domain Model -> Form ViewModel Dönüşümü
            var model = new FaqFormViewModel
            {
                Question = faq.Question,
                Answer = faq.Answer,
                Category = faq.Category,
                DisplayOrder = faq.DisplayOrder,
                IsActive = faq.IsActive
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFAQ(int id, FaqFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            await _contentService.UpdateFAQAsync(id, model);
            TempData["SuccessMessage"] = "SSS güncellendi.";
            return RedirectToAction(nameof(FAQs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFAQ(int id)
        {
            await _contentService.DeleteFAQAsync(id);
            TempData["SuccessMessage"] = "SSS silindi.";
            return RedirectToAction(nameof(FAQs));
        }

        // --- Banner Yönetimi ---

        [HttpGet]
        public async Task<IActionResult> Banners()
        {
            var model = await _contentService.GetBannersAsync();
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateBanner()
        {
            return View(new BannerFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBanner(BannerFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            await _contentService.CreateBannerAsync(model);
            TempData["SuccessMessage"] = "Yeni banner eklendi.";
            return RedirectToAction(nameof(Banners));
        }

        [HttpGet]
        public async Task<IActionResult> EditBanner(int id)
        {
            var banner = await _contentService.GetBannerByIdAsync(id);
            if (banner == null) return NotFound();

            // YENİ EKLENDİ: ID'yi forma gönderebilmek için ViewBag'e ata
            ViewBag.BannerId = id;

            // Domain Model -> Form ViewModel Dönüşümü
            var model = new BannerFormViewModel
            {
                Title = banner.Title,
                ImageUrl = banner.ImageUrl,
                LinkUrl = banner.LinkUrl,
                Position = banner.Position,
                DisplayOrder = banner.DisplayOrder,
                StartDate = banner.StartDate,
                EndDate = banner.EndDate,
                IsActive = banner.IsActive
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBanner(int id, BannerFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            await _contentService.UpdateBannerAsync(id, model);
            TempData["SuccessMessage"] = "Banner güncellendi.";
            return RedirectToAction(nameof(Banners));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBanner(int id)
        {
            await _contentService.DeleteBannerAsync(id);
            TempData["SuccessMessage"] = "Banner silindi.";
            return RedirectToAction(nameof(Banners));
        }

        #endregion

        #region 5. E-Posta Şablonları (Email Templates)

        [HttpGet]
        public async Task<IActionResult> EmailTemplates()
        {
            var model = await _contentService.GetEmailTemplatesAsync();
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateEmailTemplate()
        {
            return View(new EmailTemplateFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmailTemplate(EmailTemplateFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            await _contentService.CreateEmailTemplateAsync(model);
            TempData["SuccessMessage"] = "E-posta şablonu oluşturuldu.";
            return RedirectToAction(nameof(EmailTemplates));
        }

        [HttpGet]
        public async Task<IActionResult> EditEmailTemplate(int id)
        {
            var template = await _contentService.GetEmailTemplateByIdAsync(id);
            if (template == null) return NotFound();

            // YENİ EKLENDİ: ID'yi forma gönderebilmek için ViewBag'e ata
            ViewBag.TemplateId = id;

            // Domain Model -> Form ViewModel Dönüşümü
            var model = new EmailTemplateFormViewModel
            {
                Name = template.Name,
                Type = template.Type,
                Subject = template.Subject,
                Body = template.Body,
                IsActive = template.IsActive
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmailTemplate(int id, EmailTemplateFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            await _contentService.UpdateEmailTemplateAsync(id, model);
            TempData["SuccessMessage"] = "E-posta şablonu güncellendi.";
            return RedirectToAction(nameof(EmailTemplates));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmailTemplate(int id)
        {
            await _contentService.DeleteEmailTemplateAsync(id);
            TempData["SuccessMessage"] = "E-posta şablonu silindi.";
            return RedirectToAction(nameof(EmailTemplates));
        }

        #endregion

        #region 6. Bülten (Newsletter)

        /// <summary>
        /// Bültene abone olanları listeler.
        /// Rota: /Admin/Content/Newsletter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Newsletter()
        {
            var model = await _contentService.GetActiveNewsletterSubscribersAsync();
            return View(model);
        }

        /// <summary>
        /// Abone listesini CSV olarak dışa aktarır.
        /// Rota: /Admin/Content/ExportNewsletterSubscribers
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportNewsletterSubscribers()
        {
            var subscribers = await _contentService.GetActiveNewsletterSubscribersAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Email"); // CSV Başlığı
            foreach (var email in subscribers)
            {
                sb.AppendLine(email);
            }

            return File(
                Encoding.UTF8.GetBytes(sb.ToString()),
                "text/csv", // MIME Tipi
                $"subscribers_{DateTime.Now:yyyyMMddHHmmss}.csv" // Dosya Adı
            );
        }

        #endregion
    }
}