using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Payment;
using Sattim.Web.ViewModels.Payment;
using Sattim.Web.ViewModels.Order;
using System.Threading.Tasks;

[Authorize]
[Route("Payment")]
public class PaymentController : BaseController
{
    private readonly IPaymentService _paymentService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PaymentController(
        IPaymentService paymentService,
        UserManager<ApplicationUser> userManager)
    {
        _paymentService = paymentService;
        _userManager = userManager;
    }

    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  KULLANICIYA YÖNELİK EYLEMLER (Client-Side)
    // ====================================================================

    [HttpPost("ProcessPayment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(OrderPaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Ödeme formu geçersiz.";
            return RedirectToAction("Pay", "Orders", new { id = model.ProductId });
        }

        var userId = GetUserId();

        if (model.PaymentMethod == "Wallet")
        {
            var (success, error) = await _paymentService.PayWithWalletAsync(model.ProductId, userId);

            if (success)
            {
                TempData["SuccessMessage"] = "Ödeme cüzdanınızdan başarıyla alındı.";
                return RedirectToAction("Details", "Orders", new { id = model.ProductId });
            }
            else
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Pay", "Orders", new { id = model.ProductId });
            }
        }
        else if (model.PaymentMethod == "Gateway")
        {
            var checkout = await _paymentService.CreateGatewayCheckoutAsync(model.ProductId, userId);

            if (checkout.Success)
            {
                return View("Checkout", checkout);
            }
            else
            {
                TempData["ErrorMessage"] = checkout.ErrorMessage;
                return RedirectToAction("Pay", "Orders", new { id = model.ProductId });
            }
        }

        TempData["ErrorMessage"] = "Geçersiz ödeme yöntemi seçildi.";
        return RedirectToAction("Pay", "Orders", new { id = model.ProductId });
    }

    [HttpGet("Checkout")]
    public IActionResult Checkout(CheckoutViewModel model)
    {
        if (string.IsNullOrEmpty(model?.HtmlContent))
        {
            return BadRequest("Geçersiz ödeme oturumu.");
        }
        return View(model);
    }


    // ====================================================================
    //  SUNUCUYA YÖNELİK EYLEMLER (Server-Side Callback)
    // ====================================================================

    [HttpPost("Confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirmation([FromForm] PaymentConfirmationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var success = await _paymentService.ProcessPaymentConfirmationAsync(model);

        if (success)
        {
            return Ok();
        }
        else
        {
            return StatusCode(500, "Ödeme işlenirken sunucu hatası.");
        }
    }
}