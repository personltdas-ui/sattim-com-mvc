using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers; // BaseController için
using Sattim.Web.Models.User;
using Sattim.Web.Services.Blog; // IBlogService
using Sattim.Web.ViewModels.Blog;
using System.Collections.Generic; // KeyNotFoundException
using System.Threading.Tasks;

[Route("Blog")] // URL: /Blog, /Blog/Post/..., /Blog/TagCloud
public class BlogController : BaseController
{
    private readonly IBlogService _blogService;
    private readonly UserManager<ApplicationUser> _userManager;

    // Sabit: Sayfa başına kaç blog yazısı gösterilecek
    private const int PageSize = 10;

    public BlogController(
        IBlogService blogService,
        UserManager<ApplicationUser> userManager)
    {
        _blogService = blogService;
        _userManager = userManager;
    }

    /// <summary>
    /// Blog ana sayfası. Tüm yayınlanmış yazıları sayfalı olarak listeler
    /// veya etikete göre filtreler.
    /// </summary>
    // GET: /Blog?page=1
    // GET: /Blog?tag=teknoloji&page=2
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(int page = 1, [FromQuery] string? tag = null)
    {
        // 1. Yazıları al (Bu satır sizde zaten var)
        var (posts, totalPages) = await _blogService.GetPublishedPostsAsync(page, PageSize, tag);

        // 2. Etiketleri de al (YENİ EKLENEN KOD)
        var tagCloudData = await _blogService.GetTagCloudAsync();

        // 3. Verileri View'a gönder
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.CurrentTag = tag;

        // 4. Etiket verisini ViewBag'e ekle (YENİ EKLENEN KOD)
        ViewBag.TagCloudData = tagCloudData;

        return View(posts);
    }

    // GET: /Blog/Post/yazi-slug-burada
    [HttpGet("Post/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return BadRequest();
        }

        try
        {
            // 1. Yazı detayını al (Bu satır sizde zaten var)
            var post = await _blogService.GetPostBySlugAsync(slug);

            // 2. Etiket bulutu verisini al (BU SATIRI EKLEYİN)
            var tagCloudData = await _blogService.GetTagCloudAsync();

            // 3. Yorum formu modelini hazırla (Bu satır sizde zaten var)
            var commentFormModel = new PostCommentViewModel
            {
                BlogPostId = post.Id
            };
            ViewBag.CommentForm = commentFormModel;

            // 4. Etiket bulutu verisini View'a gönder (BU SATIRI EKLEYİN)
            ViewBag.TagCloudData = tagCloudData;

            return View(post); // BlogPostDetailViewModel modelini View'a gönder
        }
        catch (KeyNotFoundException ex)
        {
            
            TempData["ErrorMessage"] = "Aradığınız yazı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Bir blog yazısına yeni yorum ekler.
    /// (Sadece giriş yapmış kullanıcılar)
    /// </summary>
    // POST: /Blog/PostComment/yazi-slug-burada
    [HttpPost("PostComment/{slug}")]
    [Authorize] // Yorum için giriş GEREKLİ
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostComment(string slug, [FromForm] PostCommentViewModel model)
    {
        // Modeldeki BlogPostId'nin slug ile eşleştiğini de kontrol edebilirsiniz
        // (ekstra güvenlik için), ancak bu, servis katmanının işidir.

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Yorumunuz 10 karakterden uzun olmalıdır.";
            // Doğrulama hatası olursa, kullanıcıyı aynı yazıya geri yönlendir
            return RedirectToAction(nameof(Details), new { slug = slug });
        }

        var userId = _userManager.GetUserId(User);
        var (success, returnedSlug, errorMessage) = await _blogService.PostCommentAsync(model, userId);

        if (success)
        {
            TempData["SuccessMessage"] = "Yorumunuz başarıyla gönderildi ve onaya alındı.";
            // Servisten dönen 'slug'ı kullan (her zaman 'slug' ile aynı olmalı)
            return RedirectToAction(nameof(Details), new { slug = returnedSlug });
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
            // Servis hatası olursa (örn: "Bu yazıya yorum yapılamaz")
            return RedirectToAction(nameof(Details), new { slug = slug });
        }
    }

    /// <summary>
    /// (AJAX veya Child Action ile çağrılır)
    /// Blog sayfalarında (Index, Details) gösterilecek
    /// "Etiket Bulutu" (Tag Cloud) Partial View'ını döndürür.
    /// </summary>
    // GET: /Blog/TagCloud
    [HttpGet("TagCloud")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTagCloudPartial()
    {
        var tags = await _blogService.GetTagCloudAsync();
        // _TagCloudPartial.cshtml'e List<BlogTagCloudViewModel> modelini gönder
        return PartialView("_TagCloudPartial", tags);
    }
}