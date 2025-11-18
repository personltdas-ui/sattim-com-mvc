using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Product;
using Sattim.Web.ViewModels.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

[Authorize]
[Route("MyProducts")]
public class MyProductsController : BaseController
{
    private readonly IProductService _productService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyProductsController(IProductService productService, UserManager<ApplicationUser> userManager)
    {
        _productService = productService;
        _userManager = userManager;
    }

    private string GetUserId() => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index(ProductStatus? filter)
    {
        var myProducts = await _productService.GetMyProductsAsync(GetUserId(), filter);
        return View(myProducts);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var model = await _productService.GetProductForCreateAsync();
        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var freshModel = await _productService.GetProductForCreateAsync();
            freshModel.Title = model.Title;
            return View(freshModel);
        }

        var (success, productId, errorMessage) = await _productService.CreateProductAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Ürününüz başarıyla oluşturuldu ve onaya gönderildi.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }
        else
        {
            ModelState.AddModelError(string.Empty, errorMessage);
            return View(model);
        }
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _productService.GetProductForEditAsync(id, GetUserId());

        if (model == null)
        {
            TempData["ErrorMessage"] = "Ürün bulunamadı veya bu ürünü düzenleme yetkiniz yok.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, errorMessage) = await _productService.UpdateProductAsync(id, model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Ürün başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        else
        {
            ModelState.AddModelError(string.Empty, errorMessage);
            return View(model);
        }
    }

    [HttpPost("Cancel/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var (success, errorMessage) = await _productService.CancelProductAsync(id, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Ürün ilandan kaldırıldı.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("AddImages")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImages(int productId, List<IFormFile> images)
    {
        var (success, errorMessage) = await _productService.AddProductImagesAsync(productId, images, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Resimler eklendi.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost("DeleteImage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int productId)
    {
        var (success, errorMessage) = await _productService.DeleteProductImageAsync(imageId, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Resim silindi.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost("UpdateImageOrder")]
    public async Task<IActionResult> UpdateImageOrder(int productId, [FromBody] List<ImageOrderViewModel> imageOrders)
    {
        var success = await _productService.UpdateImageOrderAsync(productId, imageOrders, GetUserId());

        if (success)
        {
            return Json(new { success = true, message = "Sıralama güncellendi." });
        }

        return Json(new { success = false, message = "Hata oluştu." });
    }
}