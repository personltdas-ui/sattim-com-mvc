using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Services.Moderation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Sattim.Web.Models.User;
using Sattim.Web.ViewModels.Moderation;

namespace Sattim.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Moderator")]
    public class ModerationController : Controller
    {
        private readonly IModerationService _moderationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ModerationController(IModerationService moderationService, UserManager<ApplicationUser> userManager)
        {
            _moderationService = moderationService;
            _userManager = userManager;
        }

        private string GetCurrentAdminId() => _userManager.GetUserId(User);

        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var reports = await _moderationService.GetPendingReportsAsync();
            return View(reports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewReport(int reportId)
        {
            await _moderationService.MarkReportAsUnderReviewAsync(reportId, GetCurrentAdminId());
            TempData["SuccessMessage"] = "Şikayet 'İncelemede' olarak işaretlendi.";
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int reportId, string resolutionNote)
        {
            await _moderationService.ResolveReportAsync(reportId, GetCurrentAdminId(), resolutionNote);
            TempData["SuccessMessage"] = "Şikayet 'Çözüldü' olarak işaretlendi.";
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReport(int reportId, string rejectionNote)
        {
            await _moderationService.RejectReportAsync(reportId, GetCurrentAdminId(), rejectionNote);
            TempData["SuccessMessage"] = "Şikayet 'Reddedildi' (asılsız) olarak işaretlendi.";
            return RedirectToAction(nameof(Reports));
        }

        [HttpGet]
        public async Task<IActionResult> Disputes()
        {
            var disputes = await _moderationService.GetPendingDisputesAsync();
            return View(disputes);
        }

        [HttpGet]
        public async Task<IActionResult> DisputeDetail(int id)
        {
            var disputeDetails = await _moderationService.GetDisputeDetailsAsync(id);
            if (disputeDetails == null) return NotFound();
            return View(disputeDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDisputeMessage(int disputeId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                TempData["ErrorMessage"] = "Mesaj boş olamaz.";
                return RedirectToAction(nameof(DisputeDetail), new { id = disputeId });
            }

            await _moderationService.AddDisputeMessageAsync(disputeId, GetCurrentAdminId(), message);
            TempData["SuccessMessage"] = "Mesajınız ihtilafa eklendi.";
            return RedirectToAction(nameof(DisputeDetail), new { id = disputeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDisputeForSeller(int disputeId, string resolutionNote)
        {
            await _moderationService.ResolveDisputeForSellerAsync(disputeId, GetCurrentAdminId(), resolutionNote);
            TempData["SuccessMessage"] = "İhtilaf SATICI lehine çözüldü. Bakiye serbest bırakıldı.";
            return RedirectToAction(nameof(Disputes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDisputeForBuyer(int disputeId, string resolutionNote)
        {
            await _moderationService.ResolveDisputeForBuyerAsync(disputeId, GetCurrentAdminId(), resolutionNote);
            TempData["SuccessMessage"] = "İhtilaf ALICI lehine çözüldü. Bakiye iade edildi.";
            return RedirectToAction(nameof(Disputes));
        }

        [HttpGet]
        public async Task<IActionResult> Comments()
        {
            var comments = await _moderationService.GetPendingCommentsAsync();
            return View(comments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveComment(int commentId)
        {
            await _moderationService.ApproveCommentAsync(commentId, GetCurrentAdminId());
            TempData["SuccessMessage"] = "Yorum onaylandı ve yayınlandı.";
            return RedirectToAction(nameof(Comments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectComment(int commentId)
        {
            await _moderationService.RejectCommentAsync(commentId, GetCurrentAdminId());
            TempData["SuccessMessage"] = "Yorum reddedildi.";
            return RedirectToAction(nameof(Comments));
        }

        [HttpGet]
        public async Task<IActionResult> Products()
        {
            var products = await _moderationService.GetPendingProductsAsync();
            return View(products);
        }
    }
}