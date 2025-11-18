using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Bid;
using Sattim.Web.Services.Bid;
using Sattim.Web.ViewModels.Bid;
using System.Threading.Tasks;

[Authorize]
[Route("Bids")]
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

    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  COMMANDS (Teklif Verme/Ayarlama)
    // ====================================================================

    [HttpPost("PlaceBid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceBid(PlaceBidViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Teklif formu geçerli değil. Lütfen tutarı kontrol edin.";
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

        return RedirectToAction("Details", "Products", new { id = model.ProductId });
    }

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

    [HttpPost("CancelAutoBid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAutoBid(int productId)
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
    //  QUERIES (Okuma)
    // ====================================================================

    [HttpGet("MyBids")]
    public async Task<IActionResult> MyBids(BidFilterType filter = BidFilterType.All)
    {
        var userBids = await _bidService.GetUserBidsAsync(GetUserId(), filter);

        ViewBag.CurrentFilter = filter;

        return View(userBids);
    }

    [HttpGet("HistoryPartial/{productId}")]
    public async Task<IActionResult> GetBidHistoryPartial(int productId)
    {
        try
        {
            var history = await _bidService.GetProductBidHistoryAsync(productId);
            return PartialView("_BidHistoryPartial", history);
        }
        catch (System.Exception)
        {
            return PartialView("_BidHistoryPartial", null);
        }
    }

    [HttpGet("AutoBidSettingPartial/{productId}")]
    public async Task<IActionResult> GetAutoBidSettingPartial(int productId)
    {
        var setting = await _bidService.GetUserAutoBidSettingAsync(productId, GetUserId());

        return PartialView("_AutoBidSettingPartial", setting);
    }
}