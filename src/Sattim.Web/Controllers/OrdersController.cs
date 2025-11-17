using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers; // BaseController için
using Sattim.Web.Models.User;
using Sattim.Web.Models.Escrow; // EscrowStatus enum'ı için
using Sattim.Web.Services.Order; // IOrderService
using Sattim.Web.ViewModels.Order; // ViewModel'lar
using System; // Exception handling
using System.Collections.Generic; // KeyNotFoundException
using System.Threading.Tasks;

[Authorize] // Bu controller'daki tüm metotlar giriş yapmış kullanıcı gerektirir
[Route("Orders")] // URL: /Orders/MyOrders, /Orders/MySales vb.
public class OrdersController : BaseController
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;
    // TODO: Ödeme yapmak için IWalletService veya IPaymentService enjekte edilmeli.

    public OrdersController(
        IOrderService orderService,
        UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    // O anki giriş yapmış kullanıcının ID'sini alır
    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  QUERY (Okuma) Eylemleri - Alıcı
    // ====================================================================

    /// <summary>
    /// "Siparişlerim" sayfası (Alıcı Görünümü).
    /// </summary>
    // GET: /Orders/MyOrders
    [HttpGet("MyOrders")]
    public async Task<IActionResult> MyOrders()
    {
        // Servisinizden List<OrderSummaryViewModel> döner (UYUMLU)
        var orders = await _orderService.GetMyOrdersAsync(GetUserId());
        return View(orders);
    }

    /// <summary>
    /// Alıcının, kazandığı bir ürün için "Ödeme Yap" sayfasını (GET) gösterir.
    /// </summary>
    // GET: /Orders/Pay/5 (5 = ProductId)
    [HttpGet("Pay/{id}")]
    public async Task<IActionResult> Pay(int id)
    {
        try
        {
            // Servisinizden OrderDetailViewModel döner (UYUMLU)
            var orderDetail = await _orderService.GetOrderDetailAsync(id, GetUserId());

            if (orderDetail.BuyerId != GetUserId() || orderDetail.Status != EscrowStatus.Pending)
            {
                TempData["ErrorMessage"] = "Bu sipariş için ödeme yapamazsınız.";
                return RedirectToAction(nameof(MyOrders));
            }

            // Yeni ödeme formu için boş bir model hazırla
            var paymentForm = new OrderPaymentViewModel
            {
                ProductId = id
            };
            ViewBag.PaymentForm = paymentForm;

            // Ana model olarak sipariş detayını gönder
            return View(orderDetail);
        }
        catch (Exception ex) when (ex is KeyNotFoundException || ex is UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] = "Sipariş bulunamadı veya yetkiniz yok.";
            return RedirectToAction(nameof(MyOrders));
        }
    }

    // ====================================================================
    //  QUERY (Okuma) Eylemleri - Satıcı
    // ====================================================================

    /// <summary>
    /// "Satışlarım" sayfası (Satıcı Görünümü).
    /// </summary>
    // GET: /Orders/MySales
    [HttpGet("MySales")]
    public async Task<IActionResult> MySales()
    {
        // Servisinizden List<SalesSummaryViewModel> döner (UYUMLU)
        var sales = await _orderService.GetMySalesAsync(GetUserId());
        return View(sales);
    }


    // ====================================================================
    //  QUERY (Okuma) Eylemleri - Ortak
    // ====================================================================

    /// <summary>
    /// Alıcı veya Satıcı için "Sipariş Detayı" sayfasını gösterir.
    /// </summary>
    // GET: /Orders/Details/5 (5 = ProductId)
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            // Servisinizden OrderDetailViewModel döner (UYUMLU)
            var orderDetail = await _orderService.GetOrderDetailAsync(id, GetUserId());

            ViewBag.UserRoleInOrder = (orderDetail.BuyerId == GetUserId()) ? "Buyer" : "Seller";

            // Satıcının kargo formu girmesi için boş bir model hazırla
            if (ViewBag.UserRoleInOrder == "Seller")
            {
                ViewBag.ShippingForm = new SubmitShippingViewModel { ProductId = id };
            }

            return View(orderDetail);
        }
        catch (Exception ex) when (ex is KeyNotFoundException || ex is UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] = "Sipariş bulunamadı veya yetkiniz yok.";
            return RedirectToAction("Index", "Home");
        }
    }


    // ====================================================================
    //  COMMAND (Yazma) Eylemleri
    // ====================================================================

    /// <summary>
    /// Alıcının ödeme formunu (POST) işler.
    /// (OrderPaymentViewModel modeline göre GÜNCELLENDİ)
    /// </summary>
    // POST: /Orders/SubmitPayment
    [HttpPost("SubmitPayment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitPayment(OrderPaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Ödeme bilgileri geçersiz.";
            return RedirectToAction(nameof(Pay), new { id = model.ProductId });
        }

        // TODO: IOrderService'e 'ProcessPaymentAsync(OrderPaymentViewModel model, string userId)' metodu eklenmeli.
        // Bu metot:
        // 1. model.PaymentMethod'u kontrol etmeli ("Wallet" veya "Gateway").
        // 2. "Wallet" ise: IWalletService'i çağırıp cüzdandan parayı çekmeli.
        // 3. "Gateway" ise: IPaymentGatewayService'i (Stripe/Iyzico) çağırıp ödemeyi almalı.
        // 4. Başarılıysa Escrow'u 'Funded' yapmalı.
        // 5. INotificationService ile Satıcıya 'SendSellerPaymentReceivedNotificationAsync' bildirimi göndermeli.

        // var (success, errorMessage) = await _orderService.ProcessPaymentAsync(model, GetUserId());

        bool success = true; // Sadece simülasyon
        string errorMessage = "Simülasyon hatası";

        if (success)
        {
            TempData["SuccessMessage"] = "Ödeme başarıyla alındı. Satıcı ürünü kargolayacaktır.";
            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Pay), new { id = model.ProductId });
        }
    }

    /// <summary>
    /// Satıcının kargo bilgilerini (POST) girmesini sağlar.
    /// (SubmitShippingViewModel modeline göre UYUMLU)
    /// </summary>
    // POST: /Orders/SubmitShipping
    [HttpPost("SubmitShipping")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitShipping(SubmitShippingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Kargo bilgileri geçersiz.";
            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        // TODO: IOrderService'e 'SubmitShippingInfoAsync(SubmitShippingViewModel model, string sellerId)' metodu eklenmeli.
        // Bu metot:
        // 1. Escrow'u 'Shipped' yapar.
        // 2. ShippingInfo'ya kargo/takip no ekler.
        // 3. INotificationService ile Alıcıya 'SendProductShippedNotificationAsync' bildirimi gönderir.

        // var (success, errorMessage) = await _orderService.SubmitShippingInfoAsync(model, GetUserId());

        bool success = true; // Sadece simülasyon
        string errorMessage = "Simülasyon hatası";

        if (success)
        {
            TempData["SuccessMessage"] = "Kargo bilgileri kaydedildi ve alıcıya bildirildi.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction(nameof(Details), new { id = model.ProductId });
    }

    /// <summary>
    /// Alıcının "Teslim Aldım" (POST) butonuna basmasını işler.
    /// </summary>
    // POST: /Orders/ConfirmDelivery
    [HttpPost("ConfirmDelivery")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDelivery(int productId) // Formdan 'productId' gelmeli
    {
        // TODO: IOrderService'e 'ConfirmDeliveryAsync(int productId, string buyerId)' metodu eklenmeli.
        // Bu metot:
        // 1. Escrow'u 'Completed' yapar.
        // 2. IWalletService'i çağırarak Satıcının cüzdanına parayı aktarır.
        // 3. INotificationService ile Satıcıya 'SendFundsReleasedNotificationAsync' bildirimi gönderir.

        // var (success, errorMessage) = await _orderService.ConfirmDeliveryAsync(productId, GetUserId());

        bool success = true; // Sadece simülasyon
        string errorMessage = "Simülasyon hatası";

        if (success)
        {
            TempData["SuccessMessage"] = "Teslimatı onayladığınız için teşekkür ederiz. Satıcıya ödemesi aktarılacaktır.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction(nameof(Details), new { id = productId });
    }
}