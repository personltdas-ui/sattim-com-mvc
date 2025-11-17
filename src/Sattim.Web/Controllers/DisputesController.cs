using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers; // BaseController için
using Sattim.Web.Models.User;
using Sattim.Web.Services.Dispute; // IDisputeService
using Sattim.Web.ViewModels.Dispute;
using System; // Exception handling için
using System.Collections.Generic; // KeyNotFoundException için
using System.Threading.Tasks;

[Authorize] // Bu controller'daki tüm metotlar giriş yapmış kullanıcı gerektirir
[Route("Disputes")] // URL: /Disputes, /Disputes/Details/5 vb.
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

    // O anki giriş yapmış kullanıcının ID'sini alır
    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  QUERY (Okuma) Eylemleri
    // ====================================================================

    /// <summary>
    /// "İhtilaflarım" ana sayfası. Kullanıcının dahil olduğu
    /// (Alıcı veya Satıcı olarak) tüm ihtilafları listeler.
    /// </summary>
    // GET: /Disputes
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var myDisputes = await _disputeService.GetMyDisputesAsync(GetUserId());
        return View(myDisputes); // List<DisputeSummaryViewModel> modelini View'a gönder
    }

    /// <summary>
    /// Tek bir ihtilafın detaylarını (mesajlaşma geçmişi) gösterir.
    /// </summary>
    // GET: /Disputes/Details/5
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var disputeDetails = await _disputeService.GetMyDisputeDetailsAsync(id, GetUserId());

            // Detay sayfasındaki "yeni mesaj" formu için boş bir model hazırla
            var messageForm = new AddDisputeMessageViewModel
            {
                DisputeId = id
            };
            ViewBag.MessageForm = messageForm;

            return View(disputeDetails); // DisputeDetailViewModel modelini View'a gönder
        }
        catch (KeyNotFoundException ex)
        {
            
            TempData["ErrorMessage"] = "Aradığınız ihtilaf kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException ex)
        {
            
            TempData["ErrorMessage"] = "Bu ihtilafı görüntüleme yetkiniz yok.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ====================================================================
    //  COMMAND (Yazma) Eylemleri
    // ====================================================================

    /// <summary>
    /// Yeni bir ihtilaf açma formunu (GET) gösterir.
    /// Bu metot genellikle "Siparişlerim" sayfasından çağrılır.
    /// </summary>
    // GET: /Disputes/Open/5 (5 = ProductId/EscrowId)
    [HttpGet("Open/{productId}")]
    public async Task<IActionResult> OpenDispute(int productId)
    {
        // NOT: Servis katmanınız (OpenDisputeAsync) ZATEN bu kullanıcının
        // bu ürünün alıcısı olup olmadığını kontrol ediyor.
        // İstersek burada (GET) bir ön-kontrol daha yapabiliriz,
        // ancak şimdilik formu göstermek için servise güveniyoruz.

        var model = new OpenDisputeViewModel
        {
            ProductId = productId
        };

        // Gerekirse:
        // var product = await _productService.GetProductTitle(productId);
        // ViewBag.ProductTitle = product.Title;

        return View(model); // OpenDisputeViewModel modelini View'a gönder
    }

    /// <summary>
    /// Yeni ihtilaf açma formunu (POST) işler.
    /// </summary>
    // POST: /Disputes/Open
    [HttpPost("Open")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenDispute(OpenDisputeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Formda eksik veya hatalı alanlar var. Lütfen kontrol edin.";
            return View(model); // Hatalı formu geri döndür
        }

        // Servis, bu 'userId'nin 'buyerId' olduğunu varsayar
        var (success, disputeId, errorMessage) = await _disputeService.OpenDisputeAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "İhtilaf başarıyla açıldı. Satıcıya ve yöneticilere bildirim gönderildi.";
            // Kullanıcıyı doğrudan açılan ihtilafın detay/mesaj sayfasına yönlendir
            return RedirectToAction(nameof(Details), new { id = disputeId.Value });
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
            // Hata varsa (örn: "Zaten açık ihtilaf var"), formu tekrar göster
            return View(model);
        }
    }

    /// <summary>
    /// Mevcut bir ihtilafa yeni bir mesaj ekler (Detay sayfasındaki form).
    /// </summary>
    // POST: /Disputes/AddMessage
    [HttpPost("AddMessage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMessage(AddDisputeMessageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Mesajınız 2 karakterden uzun olmalıdır.";
            // Hata durumunda, kullanıcının bulunduğu detay sayfasına geri dön
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

        // Başarılı da olsa, hata da olsa kullanıcıyı ihtilaf detay sayfasına geri yönlendir
        return RedirectToAction(nameof(Details), new { id = model.DisputeId });
    }
}