using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers; // BaseController için
using Sattim.Web.Models.User;
using Sattim.Web.Services.Wallet; // IWalletService
using Sattim.Web.ViewModels.Wallet; // ViewModel'lar
using System.Threading.Tasks;

[Authorize] // Bu controller'daki tüm metotlar giriş yapmış kullanıcı gerektirir
[Route("Wallet")] // URL: /Wallet
public class WalletController : BaseController
{
    private readonly IWalletService _walletService;
    private readonly UserManager<ApplicationUser> _userManager;

    public WalletController(
        IWalletService walletService,
        UserManager<ApplicationUser> userManager)
    {
        _walletService = walletService;
        _userManager = userManager;
    }

    // O anki giriş yapmış kullanıcının ID'sini alır
    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  QUERY (Okuma) Eylemi
    // ====================================================================

    /// <summary>
    /// "Cüzdanım" ana sayfasını (Dashboard) gösterir.
    /// Bakiye, son işlemler ve para çekme geçmişini listeler.
    /// </summary>
    // GET: /Wallet veya /Wallet/Index
    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        // 1. Servisten cüzdanın tüm verilerini (Dashboard modeli) al
        var model = await _walletService.GetWalletDashboardAsync(GetUserId());

        // 2. Para çekme formu (Modal/Form) için boş bir ViewModel hazırla
        // (Ana modeli kirletmemek için ViewBag ile taşıyoruz)
        ViewBag.PayoutForm = new PayoutRequestViewModel();

        // 3. Ana 'WalletDashboardViewModel'i View'a gönder
        return View(model);
    }

    // ====================================================================
    //  COMMAND (Yazma) Eylemi
    // ====================================================================

    /// <summary>
    /// Kullanıcının "Para Çekme Talebi" formunu (POST) işler.
    /// </summary>
    // POST: /Wallet/RequestPayout
    [HttpPost("RequestPayout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPayout(PayoutRequestViewModel model)
    {
        // 1. Formun geçerliliğini kontrol et (Required, Range vb.)
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Para çekme talebi formu geçersiz. Lütfen tüm alanları (IBAN, Tutar vb.) kontrol edin.";

            // Hata durumunda, 'Index' sayfasına geri yönlendir.
            // (Doğrudan 'View(model)' DÖNDÜREMEYİZ, çünkü 'Index' sayfası
            // 'WalletDashboardViewModel' bekler, 'PayoutRequestViewModel' değil).
            return RedirectToAction(nameof(Index));
        }

        // 2. Servis katmanını çağır
        var (success, errorMessage) = await _walletService.RequestPayoutAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Para çekme talebiniz başarıyla alındı ve incelemeye gönderildi.";
        }
        else
        {
            // Servisten gelen hatayı (örn: "Yetersiz bakiye.") göster
            TempData["ErrorMessage"] = errorMessage;
        }

        // 3. (Post-Redirect-Get) Başarılı da olsa, hata da olsa 'Index' sayfasına geri yönlendir
        return RedirectToAction(nameof(Index));
    }
}