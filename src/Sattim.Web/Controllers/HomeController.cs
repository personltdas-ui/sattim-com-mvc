using Microsoft.AspNetCore.Diagnostics; // IExceptionHandlerPathFeature için
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Models.UI; // NotificationType enum'ı için
using Sattim.Web.Services.Blog;
using Sattim.Web.Services.Notification;
using Sattim.Web.Services.Product;
using Sattim.Web.ViewModels.Blog; // BlogSummaryViewModel
using Sattim.Web.ViewModels.Home; // HomeViewModel, ContactViewModel
using Sattim.Web.ViewModels.Product; // ProductFilterViewModel
using System.Diagnostics; // Activity
using System.Threading.Tasks;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;
    private readonly IBlogService _blogService;
    private readonly INotificationService _notificationService;

    public HomeController(
        ILogger<HomeController> logger,
        IProductService productService,
        IBlogService blogService,
        INotificationService notificationService)
    {
        _logger = logger;
        _productService = productService;
        _blogService = blogService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Sitenin ana sayfası (Anasayfa).
    /// En yeni ürünleri, süresi dolmak üzere olanları ve son blog yazılarını gösterir.
    /// </summary>
    // GET: /
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // 1. "En Yeni" ürünler için filtre hazırla (Örn: 8 adet)
        var newestFilter = new ProductFilterViewModel
        {
            SortBy = ProductSortOrder.Newest,
            Page = 1,
            PageSize = 8
        };
        var (newestProducts, _) = await _productService.GetProductListAsync(newestFilter);

        // 2. "Süresi Dolan" ürünler için filtre hazırla (Örn: 4 adet)
        var endingSoonFilter = new ProductFilterViewModel
        {
            SortBy = ProductSortOrder.EndingSoon,
            Page = 1,
            PageSize = 4
        };
        var (endingSoonProducts, _) = await _productService.GetProductListAsync(endingSoonFilter);

        // 3. "Son Blog Yazıları" için veri al (Örn: 3 adet)
        var (blogPosts, _) = await _blogService.GetPublishedPostsAsync(1, 3);

        // 4. Tüm veriyi tek bir ViewModel'de topla
        var viewModel = new HomeViewModel
        {
            NewestProducts = newestProducts,
            EndingSoonProducts = endingSoonProducts,
            RecentBlogPosts = blogPosts
        };

        return View(viewModel);
    }

    /// <summary>
    /// İletişim sayfasını (GET) gösterir.
    /// </summary>
    // GET: /Home/Contact
    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }

    /// <summary>
    /// İletişim formunu (POST) işler ve Adminlere bildirim gönderir.
    /// </summary>
    // POST: /Home/Contact
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Formda eksik veya hatalı alanlar var. Lütfen kontrol edin.";
            return View(model);
        }

        try
        {
            // 1. Mesajı adminlere bildirmek için hazırla
            string messageBody = $"Gönderen: {model.Name} ({model.Email})\n\n--- Mesaj ---\n{model.Message}";

            

            TempData["SuccessMessage"] = "Mesajınız başarıyla alındı. En kısa sürede size dönüş yapacağız.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İletişim formu gönderilirken hata oluştu.");
            TempData["ErrorMessage"] = "Mesajınız gönderilirken beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
            return View(model);
        }
    }


    /// <summary>
    /// Gizlilik politikası sayfasını gösterir.
    /// </summary>
    // GET: /Home/Privacy
    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// (KRİTİK) Uygulama genelinde oluşan tüm hataları yakalar.
    /// Bu metot, Startup.cs/Program.cs'teki 'app.UseExceptionHandler("/Home/Error")'
    /// tarafından tetiklenir.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // 1. Oluşan hatayı yakala
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionHandlerPathFeature != null)
        {
            // 2. Hatayı log'la (Hangi yolda, hangi hatayı aldık)
            _logger.LogError(
                exceptionHandlerPathFeature.Error,
                "İşlenemeyen Hata (Unhandled Exception) Oluştu. Yol: {Path}",
                exceptionHandlerPathFeature.Path
            );
        }

        // 3. Kullanıcıya 'RequestId'yi (Takip Kodu) göster
        return View(new Sattim.Web.ViewModels.Home.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}