using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Notification;
using System.Threading.Tasks;

[Authorize]
[Route("Notifications")]
public class NotificationsController : BaseController
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  QUERY (Okuma) Eylemleri
    // ====================================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(GetUserId());
        return View(notifications);
    }

    [HttpGet("GetUnreadCount")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadNotificationCountAsync(GetUserId());
        return Ok(new { count = count });
    }

    // ====================================================================
    //  COMMAND (Yazma) Eylemleri
    // ====================================================================

    [HttpPost("MarkAllAsRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllNotificationsAsReadAsync(GetUserId());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("MarkAsRead/{id}")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var success = await _notificationService.MarkNotificationAsReadAsync(id, GetUserId());

        if (!success)
        {
            return NotFound(new { success = false, message = "Bildirim bulunamadı." });
        }

        return Ok(new { success = true });
    }
}