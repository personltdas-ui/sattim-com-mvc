using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers; // BaseController için
using Sattim.Web.Models.User;
using Sattim.Web.Services.Payment; // IPaymentService
using Sattim.Web.ViewModels.Payment; // ViewModels
using Sattim.Web.ViewModels.Order; // OrderPaymentViewModel (Önceki adımdan)
using System.Threading.Tasks;

[Authorize] // Bu controller'daki tüm metotlar giriş yapmış kullanıcı gerektirir
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

    // O anki giriş yapmış kullanıcının ID'sini alır
    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  KULLANICIYA YÖNELİK EYLEMLER (Client-Side)
    // ====================================================================

    /// <summary>
    /// Bu metot, /Orders/Pay sayfasındaki form tarafından çağrılır.
    /// Hangi ödeme yönteminin seçildiğine karar verir ve yönlendirir.
    /// </summary>
    // POST: /Payment/ProcessPayment
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
            // 1. CÜZDAN İLE ÖDEME (Doğrudan Servisi Çağır)
            var (success, error) = await _paymentService.PayWithWalletAsync(model.ProductId, userId);

            if (success)
            {
                TempData["SuccessMessage"] = "Ödeme cüzdanınızdan başarıyla alındı.";
                return RedirectToAction("Details", "Orders", new { id = model.ProductId });
            }
            else
            {
                TempData["ErrorMessage"] = error; // Örn: "Yetersiz bakiye."
                return RedirectToAction("Pay", "Orders", new { id = model.ProductId });
            }
        }
        else if (model.PaymentMethod == "Gateway")
        {
            // 2. KREDİ KARTI İLE ÖDEME (Ağ Geçidi Sayfasına Yönlendir)

            // Servisten Checkout (HTML/Script) içeriğini al
            var checkout = await _paymentService.CreateGatewayCheckoutAsync(model.ProductId, userId);

            if (checkout.Success)
            {
                // Kullanıcıyı, içinde ağ geçidinin HTML formu bulunan
                // 'Checkout' sayfasına yönlendir.
                return View("Checkout", checkout); // CheckoutViewModel'i View'a gönder
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

    // GET: /Payment/Checkout (ProcessPayment tarafından yönlendirilir)
    // Bu View, sadece Iyzico/Stripe'ın HTML içeriğini göstermek için vardır.
    [HttpGet("Checkout")]
    public IActionResult Checkout(CheckoutViewModel model)
    {
        // Bu metot doğrudan çağrılmamalı, ProcessPayment'ten yönlendirilmeli.
        // Model (HTML içeriği) zaten ProcessPayment'ten geliyor.
        if (string.IsNullOrEmpty(model?.HtmlContent))
        {
            return BadRequest("Geçersiz ödeme oturumu.");
        }
        return View(model);
    }


    // ====================================================================
    //  SUNUCUYA YÖNELİK EYLEMLER (Server-Side Callback)
    // ====================================================================

    /// <summary>
    /// Ödeme Ağ Geçidinin (Iyzico/Stripe) ödeme sonucunu
    /// bildirmek için çağırdığı URL (Callback/Webhook).
    /// </summary>
    // POST: /Payment/Confirmation
    [HttpPost("Confirmation")]
    [AllowAnonymous] // BU ÇOK ÖNEMLİ: Ağ geçidi (sunucu) giriş yapmış olmayacaktır.
    public async Task<IActionResult> Confirmation([FromForm] PaymentConfirmationViewModel model)
    {
        // NOT: [FromForm] veya [FromBody] kullanmanız, ağ geçidinin
        // size veriyi nasıl (Form-encoded veya JSON) gönderdiğine bağlıdır.
        // Iyzico genellikle [FromForm] kullanır.

        if (!ModelState.IsValid)
        {
            
            // Ağ geçidine 'Başarısız' (HTTP 400) bildir
            return BadRequest();
        }

        var success = await _paymentService.ProcessPaymentConfirmationAsync(model);

        if (success)
        {
            // Ağ geçidine 'Başarılı' (HTTP 200) bildir (işledik, tekrar gönderme)
            return Ok();
        }
        else
        {
            // Ağ geçidine 'Başarısız' (HTTP 500) bildir (işleyemedik, tekrar dene)
            return StatusCode(500, "Ödeme işlenirken sunucu hatası.");
        }
    }
}