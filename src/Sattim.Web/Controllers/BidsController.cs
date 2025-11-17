using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers; // BaseController için
using Sattim.Web.Models.User;
using Sattim.Web.Models.Bid; // BidFilterType enum'ı için (VARSAYIM)
using Sattim.Web.Services.Bid;  // IBidService
using Sattim.Web.ViewModels.Bid;
using System.Threading.Tasks;

[Authorize] // Bu controller'daki tüm metotlar giriş yapmış kullanıcı gerektirir
[Route("Bids")] // URL: /Bids/PlaceBid, /Bids/MyBids vb.
public class BidsController : BaseController
{
    private readonly IBidService _bidService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BidsController(
        IBidService bidService,
        UserManager<ApplicationUser> userManager)
    {
        _bidService = bidService;
        _userManager = userManager;
    }

    // O anki giriş yapmış kullanıcının ID'sini alır
    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  COMMANDS (Teklif Verme/Ayarlama) - Genellikle Ürün Detay sayfasından
    // ====================================================================

    /// <summary>
    /// Ürün detay sayfasındaki manuel teklif verme formunu işler.
    /// </summary>
    // POST: /Bids/PlaceBid
    [HttpPost("PlaceBid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceBid(PlaceBidViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Teklif formu geçerli değil. Lütfen tutarı kontrol edin.";
            // Hatalı form, ürün detay sayfasına geri döner
            return RedirectToAction("Details", "Products", new { id = model.ProductId });
        }

        var (success, errorMessage) = await _bidService.PlaceBidAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Teklifiniz başarıyla alındı!";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        // Başarılı da olsa, hata da olsa kullanıcıyı ürün detay sayfasına geri yönlendir
        return RedirectToAction("Details", "Products", new { id = model.ProductId });
    }

    /// <summary>
    /// Ürün detay sayfasındaki otomatik teklif ayarlama formunu işler.
    /// </summary>
    // POST: /Bids/PlaceAutoBid
    [HttpPost("PlaceAutoBid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceAutoBid(AutoBidViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Otomatik teklif formu geçerli değil. Lütfen tutarları kontrol edin.";
            return RedirectToAction("Details", "Products", new { id = model.ProductId });
        }

        var (success, errorMessage) = await _bidService.PlaceAutoBidAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Otomatik teklif ayarlandı! Artık sizin için teklif vereceğiz.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction("Details", "Products", new { id = model.ProductId });
    }

    /// <summary>
    /// Ürün detay sayfasındaki "Otomatik Teklifi İptal Et" butonunu işler.
    /// </summary>
    // POST: /Bids/CancelAutoBid
    [HttpPost("CancelAutoBid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAutoBid(int productId) // Formdan 'productId' gelmeli
    {
        var success = await _bidService.CancelAutoBidAsync(productId, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Otomatik teklif ayarınız iptal edildi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Otomatik teklif iptal edilirken bir hata oluştu.";
        }

        return RedirectToAction("Details", "Products", new { id = productId });
    }

    // ====================================================================
    //  QUERIES (Okuma) - "Tekliflerim" sayfası ve AJAX Partial'ları
    // ====================================================================

    /// <summary>
    /// Kullanıcının "Tekliflerim" sayfasını (tam sayfa) döndürür.
    /// </summary>
    // GET: /Bids/MyBids?filter=Active
    [HttpGet("MyBids")]
    public async Task<IActionResult> MyBids(BidFilterType filter = BidFilterType.All)
    {
        var userBids = await _bidService.GetUserBidsAsync(GetUserId(), filter);

        // Aktif filtreyi View'a gönder (Navigasyon menüsü için)
        ViewBag.CurrentFilter = filter;

        return View(userBids); // List<UserBidItemViewModel> modelini View'a gönder
    }

    /// <summary>
    /// (AJAX/Fetch ile çağrılır)
    /// Ürün detay sayfasındaki "Teklif Geçmişi" sekmesini doldurmak için
    /// bir Partial View döndürür.
    /// </summary>
    // GET: /Bids/HistoryPartial/5
    [HttpGet("HistoryPartial/{productId}")]
    public async Task<IActionResult> GetBidHistoryPartial(int productId)
    {
        try
        {
            var history = await _bidService.GetProductBidHistoryAsync(productId);
            // _BidHistoryPartial.cshtml'e 'ProductBidHistoryViewModel' modelini gönder
            return PartialView("_BidHistoryPartial", history);
        }
        catch (System.Exception ex)
        {
            // (Loglama)
            // Ürün bulunamazsa veya hata olursa boş bir partial döndür
            return PartialView("_BidHistoryPartial", null);
        }
    }

    /// <summary>
    /// (AJAX/Fetch ile çağrılır)
    /// Ürün detay sayfasındaki "Otomatik Teklif" sekmesini, kullanıcının
    /// mevcut ayarlarıyla (eğer varsa) doldurmak için bir Partial View döndürür.
    /// </summary>
    // GET: /Bids/AutoBidSettingPartial/5
    [HttpGet("AutoBidSettingPartial/{productId}")]
    public async Task<IActionResult> GetAutoBidSettingPartial(int productId)
    {
        // Mevcut ayarı al (yoksa null döner)
        var setting = await _bidService.GetUserAutoBidSettingAsync(productId, GetUserId());

        // Modeli (AutoBidSettingViewModel veya null) _AutoBidSettingPartial.cshtml'e gönder
        return PartialView("_AutoBidSettingPartial", setting);
    }
}