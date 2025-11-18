using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Shipping;
using Sattim.Web.ViewModels.Shipping;
using System.Threading.Tasks;

[Authorize]
[Route("Shipping")]
public class ShippingController : BaseController
{
    private readonly IShippingService _shippingService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ShippingController(
        IShippingService shippingService,
        UserManager<ApplicationUser> userManager)
    {
        _shippingService = shippingService;
        _userManager = userManager;
    }

    private string GetUserId() => _userManager.GetUserId(User)!;

    [HttpPost("MarkAsShipped")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsShipped(MarkAsShippedViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Kargo bilgileri geçersiz. Lütfen kargo firmasını ve takip numarasını girin.";
            return RedirectToAction("Details", "Orders", new { id = model.ProductId });
        }

        var (success, errorMessage) = await _shippingService.MarkAsShippedAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Kargo bilgileri kaydedildi ve alıcıya bildirildi.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction("Details", "Orders", new { id = model.ProductId });
    }

    [HttpPost("MarkAsDelivered")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsDelivered(int productId)
    {
        var (success, errorMessage) = await _shippingService.MarkAsDeliveredAsync(productId, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Teslimatı onayladığınız için teşekkür ederiz. Satıcıya ödemesi aktarılacaktır.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction("Details", "Orders", new { id = productId });
    }
}