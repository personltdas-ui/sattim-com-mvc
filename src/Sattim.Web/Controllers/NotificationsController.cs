using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers; // BaseController için
using Sattim.Web.Models.User;
using Sattim.Web.Services.Notification; // INotificationService
using System.Threading.Tasks;

[Authorize] // Bu controller'daki tüm metotlar giriş yapmış kullanıcı gerektirir
[Route("Notifications")] // URL: /Notifications
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

    // O anki giriş yapmış kullanıcının ID'sini alır
    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  QUERY (Okuma) Eylemleri
    // ====================================================================

    /// <summary>
    /// "Bildirimlerim" ana sayfasını (tam sayfa) döndürür.
    /// </summary>
    // GET: /Notifications
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Servisin, bu ViewModel listesindeki 'LinkUrl' alanlarını
        // doldurduğunu varsayıyoruz.
        var notifications = await _notificationService.GetUserNotificationsAsync(GetUserId());
        return View(notifications); // List<NotificationViewModel> modelini View'a gönder
    }

    /// <summary>
    /// (AJAX/Fetch ile çağrılır)
    /// Site layout'undaki (örn. zil ikonu) okunmamış bildirim
    /// sayısını döndürür.
    /// </summary>
    // GET: /Notifications/GetUnreadCount
    [HttpGet("GetUnreadCount")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadNotificationCountAsync(GetUserId());
        return Ok(new { count = count });
    }

    // ====================================================================
    //  COMMAND (Yazma) Eylemleri
    // ====================================================================

    /// <summary>
    /// Kullanıcının tüm okunmamış bildirimlerini "okundu" olarak işaretler.
    /// </summary>
    // POST: /Notifications/MarkAllAsRead
    [HttpPost("MarkAllAsRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllNotificationsAsReadAsync(GetUserId());
        // Kullanıcıyı bildirimler sayfasına geri yönlendir
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// (AJAX/Fetch ile çağrılır)
    /// Belirli bir bildirimi "okundu" olarak işaretler.
    /// Bu metot, genellikle kullanıcı bir bildirime tıkladığında
    /// JavaScript tarafından arka planda çağrılır.
    /// </summary>
    // POST: /Notifications/MarkAsRead/5
    [HttpPost("MarkAsRead/{id}")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var success = await _notificationService.MarkNotificationAsReadAsync(id, GetUserId());

        if (!success)
        {
            // Bildirim bulunamadı veya kullanıcıya ait değil
            return NotFound(new { success = false, message = "Bildirim bulunamadı." });
        }

        return Ok(new { success = true });
    }
}