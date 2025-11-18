using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Services.Order;
using Sattim.Web.ViewModels.Order;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Authorize]
[Route("Orders")]
public class OrdersController : BaseController
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(
        IOrderService orderService,
        UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  QUERY (Okuma) Eylemleri - Alıcı
    // ====================================================================

    [HttpGet("MyOrders")]
    public async Task<IActionResult> MyOrders()
    {
        var orders = await _orderService.GetMyOrdersAsync(GetUserId());
        return View(orders);
    }

    [HttpGet("Pay/{id}")]
    public async Task<IActionResult> Pay(int id)
    {
        try
        {
            var orderDetail = await _orderService.GetOrderDetailAsync(id, GetUserId());

            if (orderDetail.BuyerId != GetUserId() || orderDetail.Status != EscrowStatus.Pending)
            {
                TempData["ErrorMessage"] = "Bu sipariş için ödeme yapamazsınız.";
                return RedirectToAction(nameof(MyOrders));
            }

            var paymentForm = new OrderPaymentViewModel
            {
                ProductId = id
            };
            ViewBag.PaymentForm = paymentForm;

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

    [HttpGet("MySales")]
    public async Task<IActionResult> MySales()
    {
        var sales = await _orderService.GetMySalesAsync(GetUserId());
        return View(sales);
    }


    // ====================================================================
    //  QUERY (Okuma) Eylemleri - Ortak
    // ====================================================================

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var orderDetail = await _orderService.GetOrderDetailAsync(id, GetUserId());

            ViewBag.UserRoleInOrder = (orderDetail.BuyerId == GetUserId()) ? "Buyer" : "Seller";

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

    [HttpPost("SubmitPayment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitPayment(OrderPaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Ödeme bilgileri geçersiz.";
            return RedirectToAction(nameof(Pay), new { id = model.ProductId });
        }

        bool success = true;
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

    [HttpPost("SubmitShipping")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitShipping(SubmitShippingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Kargo bilgileri geçersiz.";
            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        bool success = true;
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

    [HttpPost("ConfirmDelivery")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDelivery(int productId)
    {
        bool success = true;
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