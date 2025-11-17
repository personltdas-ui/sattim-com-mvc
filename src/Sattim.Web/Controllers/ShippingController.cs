using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers; // BaseController için
using Sattim.Web.Models.User;
using Sattim.Web.Services.Shipping; // IShippingService
using Sattim.Web.ViewModels.Shipping; // MarkAsShippedViewModel
using System.Threading.Tasks;

[Authorize] // Bu controller'daki tüm metotlar giriş yapmış kullanıcı gerektirir
[Route("Shipping")] // URL: /Shipping/MarkAsShipped vb.
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

    // O anki giriş yapmış kullanıcının ID'sini alır
    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  COMMANDS (Yazma İşlemleri)
    // ====================================================================

    /// <summary>
    /// Satıcının kargo bilgilerini (POST) girmesini sağlar.
    /// Bu metot /Orders/Details.cshtml sayfasındaki form tarafından çağrılır.
    /// </summary>
    // POST: /Shipping/MarkAsShipped
    [HttpPost("MarkAsShipped")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsShipped(MarkAsShippedViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Kargo bilgileri geçersiz. Lütfen kargo firmasını ve takip numarasını girin.";
            // Hata olursa, kullanıcıyı Sipariş Detay sayfasına geri yönlendir
            return RedirectToAction("Details", "Orders", new { id = model.ProductId });
        }

        // Servis, bu 'userId'nin 'sellerId' olduğunu varsayar
        var (success, errorMessage) = await _shippingService.MarkAsShippedAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Kargo bilgileri kaydedildi ve alıcıya bildirildi.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        // Her durumda kullanıcıyı Sipariş Detay sayfasına geri yönlendir
        return RedirectToAction("Details", "Orders", new { id = model.ProductId });
    }

    /// <summary>
    /// Alıcının "Teslim Aldım" (POST) butonuna basmasını işler.
    /// Bu metot /Orders/Details.cshtml sayfasındaki form tarafından çağrılır.
    /// </summary>
    // POST: /Shipping/MarkAsDelivered
    [HttpPost("MarkAsDelivered")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsDelivered(int productId) // Formdan 'productId' gelmeli
    {
        // Servis, bu 'userId'nin 'buyerId' olduğunu varsayar
        var (success, errorMessage) = await _shippingService.MarkAsDeliveredAsync(productId, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Teslimatı onayladığınız için teşekkür ederiz. Satıcıya ödemesi aktarılacaktır.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        // Her durumda kullanıcıyı Sipariş Detay sayfasına geri yönlendir
        return RedirectToAction("Details", "Orders", new { id = productId });
    }
}