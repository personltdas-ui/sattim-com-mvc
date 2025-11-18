using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Models.UI;
using Sattim.Web.Services.Blog;
using Sattim.Web.Services.Notification;
using Sattim.Web.Services.Product;
using Sattim.Web.ViewModels.Blog;
using Sattim.Web.ViewModels.Home;
using Sattim.Web.ViewModels.Product;
using System.Diagnostics;
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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var newestFilter = new ProductFilterViewModel
        {
            SortBy = ProductSortOrder.Newest,
            Page = 1,
            PageSize = 8
        };
        var (newestProducts, _) = await _productService.GetProductListAsync(newestFilter);

        var endingSoonFilter = new ProductFilterViewModel
        {
            SortBy = ProductSortOrder.EndingSoon,
            Page = 1,
            PageSize = 4
        };
        var (endingSoonProducts, _) = await _productService.GetProductListAsync(endingSoonFilter);

        var (blogPosts, _) = await _blogService.GetPublishedPostsAsync(1, 3);

        var viewModel = new HomeViewModel
        {
            NewestProducts = newestProducts,
            EndingSoonProducts = endingSoonProducts,
            RecentBlogPosts = blogPosts
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }

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
            string messageBody = $"Gönderen: {model.Name} ({model.Email})\n\n--- Mesaj ---\n{model.Message}";

            //await _notificationService.SendSystemNotificationToAdminsAsync(
            //    NotificationType.AdminAlert,
            //    "Yeni İletişim Formu",
            //    messageBody);

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


    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionHandlerPathFeature != null)
        {
            _logger.LogError(
                exceptionHandlerPathFeature.Error,
                "İşlenemeyen Hata (Unhandled Exception) Oluştu. Yol: {Path}",
                exceptionHandlerPathFeature.Path
            );
        }

        return View(new Sattim.Web.ViewModels.Home.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}