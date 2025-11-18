using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Wallet;
using Sattim.Web.ViewModels.Wallet;
using System.Threading.Tasks;

[Authorize]
[Route("Wallet")]
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

    private string GetUserId() => _userManager.GetUserId(User)!;

    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var model = await _walletService.GetWalletDashboardAsync(GetUserId());

        ViewBag.PayoutForm = new PayoutRequestViewModel();

        return View(model);
    }

    [HttpPost("RequestPayout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPayout(PayoutRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Para çekme talebi formu geçersiz. Lütfen tüm alanları (IBAN, Tutar vb.) kontrol edin.";

            return RedirectToAction(nameof(Index));
        }

        var (success, errorMessage) = await _walletService.RequestPayoutAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Para çekme talebiniz başarıyla alındı ve incelemeye gönderildi.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction(nameof(Index));
    }
}