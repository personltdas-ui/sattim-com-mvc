using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Product;
using Sattim.Web.ViewModels.Product;
using System.Security.Claims;
using System.Threading.Tasks;

public class ProductsController : BaseController
{
    private readonly IProductService _productService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProductsController(
        IProductService productService,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _productService = productService;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProductFilterViewModel filter)
    {
        var (products, totalPages) = await _productService.GetProductListAsync(filter);

        ViewBag.TotalPages = totalPages;
        ViewBag.CurrentFilter = filter;

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        string? userId = GetCurrentUserId();
        string ipAddress = GetUserIpAddress();

        var productDetail = await _productService.GetProductDetailsAsync(id.Value, userId, ipAddress);

        if (productDetail == null)
        {
            return NotFound();
        }

        return View(productDetail);
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return RedirectToAction(nameof(Index));
        }

        string? userId = GetCurrentUserId();
        string ipAddress = GetUserIpAddress();

        var (products, resultCount) = await _productService.GetSearchResultsAsync(query, userId, ipAddress);

        ViewBag.ResultCount = resultCount;
        ViewBag.Query = query;

        return View(products);
    }

    private string? GetCurrentUserId()
    {
        return _userManager.GetUserId(User);
    }

    private string GetUserIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    }
}