using Microsoft.AspNetCore.Identity; // UserManager için
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Product;
using Sattim.Web.ViewModels.Product;
using System.Security.Claims; // GetUserId için
using System.Threading.Tasks;

// Halka açık (anonim) kullanıcıların erişebileceği metotlar.
// Ürün listeleme, detay görme ve arama.
public class ProductsController : BaseController
{
    private readonly IProductService _productService;
    private readonly UserManager<ApplicationUser> _userManager; // Kullanıcı ID'si almak için
    private readonly IHttpContextAccessor _httpContextAccessor; // IP almak için

    public ProductsController(
        IProductService productService,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _productService = productService;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Ana sayfa veya kategori sayfası gibi ürün listeleme alanı.
    /// </summary>
    // GET: /Products veya /Products?CategoryId=5&Page=2
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProductFilterViewModel filter)
    {
        // Servisten hem ürünleri hem de sayfa bilgisini al
        var (products, totalPages) = await _productService.GetProductListAsync(filter);

        // Sayfalama bilgisini View'a taşı
        ViewBag.TotalPages = totalPages;
        // Mevcut filtreyi View'a geri gönder (sayfalama linkleri için)
        ViewBag.CurrentFilter = filter;

        return View(products); // List<ProductSummaryViewModel> model olarak gönderilir
    }

    /// <summary>
    /// Ürün detay sayfası.
    /// </summary>
    // GET: /Products/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Görüntüleme log'laması için yardımcı metotlardan IP ve UserID al
        string? userId = GetCurrentUserId();
        string ipAddress = GetUserIpAddress();

        var productDetail = await _productService.GetProductDetailsAsync(id.Value, userId, ipAddress);

        if (productDetail == null)
        {
            return NotFound();
        }

        return View(productDetail); // ProductDetailViewModel model olarak gönderilir
    }

    /// <summary>
    /// Arama sonuçları sayfası.
    /// </summary>
    // GET: /Products/Search?query=bisiklet
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // Boş arama yapılırsa anasayfaya yönlendir (veya boş bir view döndür)
            return RedirectToAction(nameof(Index));
        }

        // Arama log'laması için yardımcı metotlardan IP ve UserID al
        string? userId = GetCurrentUserId();
        string ipAddress = GetUserIpAddress();

        var (products, resultCount) = await _productService.GetSearchResultsAsync(query, userId, ipAddress);

        ViewBag.ResultCount = resultCount;
        ViewBag.Query = query;

        return View(products); // List<ProductSummaryViewModel> model olarak gönderilir
    }

    // --- Yardımcı Metotlar ---

    private string? GetCurrentUserId()
    {
        // Kullanıcı giriş yapmışsa ID'sini al
        return _userManager.GetUserId(User);
    }

    private string GetUserIpAddress()
    {
        // IP adresini al (Proxy/Load balancer arkasındaysa X-Forwarded-For'a bakmak gerekebilir)
        return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    }
}