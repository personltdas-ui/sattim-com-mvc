using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Dispute;
using Sattim.Web.ViewModels.Dispute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Authorize]
[Route("Disputes")]
public class DisputesController : BaseController
{
    private readonly IDisputeService _disputeService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DisputesController(
        IDisputeService disputeService,
        UserManager<ApplicationUser> userManager)
    {
        _disputeService = disputeService;
        _userManager = userManager;
    }

    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  QUERY (Okuma) Eylemleri
    // ====================================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var myDisputes = await _disputeService.GetMyDisputesAsync(GetUserId());
        return View(myDisputes);
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var disputeDetails = await _disputeService.GetMyDisputeDetailsAsync(id, GetUserId());

            var messageForm = new AddDisputeMessageViewModel
            {
                DisputeId = id
            };
            ViewBag.MessageForm = messageForm;

            return View(disputeDetails);
        }
        catch (KeyNotFoundException)
        {
            TempData["ErrorMessage"] = "Aradığınız ihtilaf kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] = "Bu ihtilafı görüntüleme yetkiniz yok.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ====================================================================
    //  COMMAND (Yazma) Eylemleri
    // ====================================================================

    [HttpGet("Open/{productId}")]
    public async Task<IActionResult> OpenDispute(int productId)
    {
        var model = new OpenDisputeViewModel
        {
            ProductId = productId
        };

        return View(model);
    }

    [HttpPost("Open")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenDispute(OpenDisputeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Formda eksik veya hatalı alanlar var. Lütfen kontrol edin.";
            return View(model);
        }

        var (success, disputeId, errorMessage) = await _disputeService.OpenDisputeAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "İhtilaf başarıyla açıldı. Satıcıya ve yöneticilere bildirim gönderildi.";
            return RedirectToAction(nameof(Details), new { id = disputeId.Value });
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
            return View(model);
        }
    }

    [HttpPost("AddMessage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMessage(AddDisputeMessageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Mesajınız 2 karakterden uzun olmalıdır.";
            return RedirectToAction(nameof(Details), new { id = model.DisputeId });
        }

        var (success, errorMessage) = await _disputeService.AddDisputeMessageAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction(nameof(Details), new { id = model.DisputeId });
    }
}