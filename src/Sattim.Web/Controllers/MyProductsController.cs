using Microsoft.AspNetCore.Authorization; // GÜVENLİK
using Microsoft.AspNetCore.Http; // IFormFile
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.Product; // ProductStatus enum
using Sattim.Web.Models.User;
using Sattim.Web.Services.Product;
using Sattim.Web.ViewModels.Product;
using System.Collections.Generic; // List<IFormFile>
using System.Threading.Tasks;

[Authorize] // Bu controller'daki tüm metotlar giriş yapmış kullanıcı gerektirir
[Route("MyProducts")] // URL: /MyProducts/Index, /MyProducts/Create vb.
public class MyProductsController : BaseController
{
    private readonly IProductService _productService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyProductsController(IProductService productService, UserManager<ApplicationUser> userManager)
    {
        _productService = productService;
        _userManager = userManager;
    }

    private string GetUserId() => _userManager.GetUserId(User)!; // Authorize olduğu için null olamaz

    // GET: /MyProducts/Index?filter=Active
    [HttpGet]
    public async Task<IActionResult> Index(ProductStatus? filter)
    {
        var myProducts = await _productService.GetMyProductsAsync(GetUserId(), filter);
        return View(myProducts);
    }

    // GET: /MyProducts/Create
    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        // Form için gerekli verileri (kategoriler vb.) servisten al
        var model = await _productService.GetProductForCreateAsync();
        return View(model);
    }

    // POST: /MyProducts/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Model geçerli değilse, formu tekrar doldur (kategoriler eksik olabilir)
            var freshModel = await _productService.GetProductForCreateAsync();
            freshModel.Title = model.Title; // vb. kullanıcının girdiği verileri koru
            return View(freshModel);
        }

        var (success, productId, errorMessage) = await _productService.CreateProductAsync(model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Ürününüz başarıyla oluşturuldu ve onaya gönderildi.";
            // Kullanıcıyı resim eklemesi için Edit sayfasına yönlendirmek daha iyi olabilir
            return RedirectToAction(nameof(Edit), new { id = productId });
        }
        else
        {
            ModelState.AddModelError(string.Empty, errorMessage);
            return View(model);
        }
    }

    // GET: /MyProducts/Edit/5
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _productService.GetProductForEditAsync(id, GetUserId());

        if (model == null)
        {
            // Başka birinin ürününü düzenlemeye çalışırsa veya ürün yoksa
            TempData["ErrorMessage"] = "Ürün bulunamadı veya bu ürünü düzenleme yetkiniz yok.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // POST: /MyProducts/Edit/5
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        if (id != model.Id) // Formdaki Id ile URL'deki Id eşleşmeli
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
            return View(model); // Hata varsa formu tekrar göster
        }
    }

    // POST: /MyProducts/Cancel/5
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

    // --- Resim Yönetimi (Genellikle Edit sayfasının içinden çağrılır) ---

    // POST: /MyProducts/AddImages
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

        // Resim eklendikten sonra Edit sayfasına geri dön
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    // POST: /MyProducts/DeleteImage
    [HttpPost("DeleteImage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int productId) // Yönlendirme için productId gerekli
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

    // NOT: UpdateImageOrder metodu (sıralama) genellikle
    // JavaScript (AJAX/Fetch) ile çağrılır ve [FromBody] bekler.
    // Bu yüzden API Controller gibi davranması daha doğrudur.

    // POST: /MyProducts/UpdateImageOrder
    [HttpPost("UpdateImageOrder")]
    public async Task<IActionResult> UpdateImageOrder(int productId, [FromBody] List<ImageOrderViewModel> imageOrders)
    {
        // [FromBody] kullandığımız için ValidateAntiForgeryToken kullanamayız,
        // (veya özel bir token gönderme mekanizması gerekir).

        var success = await _productService.UpdateImageOrderAsync(productId, imageOrders, GetUserId());

        if (success)
        {
            return Json(new { success = true, message = "Sıralama güncellendi." });
        }

        return Json(new { success = false, message = "Hata oluştu." });
    }
}