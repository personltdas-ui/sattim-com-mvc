using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Product;
using System.Threading.Tasks;

[Authorize(Roles = "Admin,Moderator")]
[Area("Admin")]
[Route("Admin/Products")]
public class AdminProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminProductsController(IProductService productService, UserManager<ApplicationUser> userManager)
    {
        _productService = productService;
        _userManager = userManager;
    }

    private string GetAdminId() => _userManager.GetUserId(User)!;

    [HttpPost("Approve/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var success = await _productService.ApproveProductAsync(id, GetAdminId());

        if (success)
        {
            TempData["SuccessMessage"] = "Ürün onaylandı ve yayına alındı.";
        }
        else
        {
            TempData["ErrorMessage"] = "Ürün onaylanırken bir hata oluştu.";
        }

        return RedirectToAction("Products", "Moderation", new { Area = "Admin" });
    }

    [HttpPost("Reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int productId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Reddetme sebebi girmek zorunludur.";
            return RedirectToAction("ProductDetails", "AdminDashboard", new { id = productId });
        }

        var success = await _productService.RejectProductAsync(productId, GetAdminId(), reason);

        if (success)
        {
            TempData["SuccessMessage"] = "Ürün reddedildi ve kullanıcıya bildirim gönderildi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Ürün reddedilirken bir hata oluştu.";
        }

        return RedirectToAction("Products", "Moderation", new { Area = "Admin" });
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _productService.DeleteProductAsAdminAsync(id, GetAdminId());

        if (success)
        {
            TempData["SuccessMessage"] = "Ürün sistemden kalıcı olarak silindi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Ürün silinirken bir hata oluştu.";
        }

        return RedirectToAction("Products", "Moderation", new { Area = "Admin" });
    }
}