using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Profile;
using Sattim.Web.Services.Storage;
using Sattim.Web.ViewModels.Profile;
using System.Threading.Tasks;

[Authorize]
[Route("Profile")]
public class UserProfileController : BaseController
{
    private readonly IProfileService _profileService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileService;

    public UserProfileController(
        IProfileService profileService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        IFileStorageService fileService)
    {
        _profileService = profileService;
        _userManager = userManager;
        _mapper = mapper;
        _fileService = fileService;
    }

    private string GetUserId() => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        var user = await _userManager.FindByIdAsync(userId);
        var profile = await _profileService.GetUserProfileAsync(userId);

        var viewModel = new ProfileDetailsViewModel
        {
            FullName = user.FullName,
            Bio = profile.Bio,
            Address = profile.Address,
            City = profile.City,
            Country = profile.Country,
            PostalCode = profile.PostalCode
        };

        ViewBag.ProfileImageUrl = user.ProfileImageUrl;
        ViewBag.IsVerified = profile.IsVerified;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileDetailsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(GetUserId());
            ViewBag.ProfileImageUrl = user.ProfileImageUrl;
            return View(model);
        }

        var success = await _profileService.UpdateProfileDetailsAsync(GetUserId(), model);

        if (success)
        {
            TempData["SuccessMessage"] = "Profiliniz başarıyla güncellendi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Profil güncellenirken bir hata oluştu.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("UpdateImage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfileImage(IFormFile profileImage)
    {
        if (profileImage == null || profileImage.Length == 0)
        {
            TempData["ErrorMessage"] = "Lütfen bir resim dosyası seçin.";
            return RedirectToAction(nameof(Index));
        }

        var newImageUrl = await _fileService.UploadFileAsync(profileImage, GetUserId());

        if (string.IsNullOrEmpty(newImageUrl))
        {
            TempData["ErrorMessage"] = "Resim yüklenirken hata oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var success = await _profileService.UpdateProfileImageAsync(GetUserId(), newImageUrl);

        if (success)
        {
            TempData["SuccessMessage"] = "Profil resmi güncellendi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Resim güncellenirken bir hata oluştu.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("SubmitIdCard")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitIdCard(IFormFile idCardImage)
    {
        if (idCardImage == null || idCardImage.Length == 0)
        {
            TempData["ErrorMessage"] = "Lütfen bir kimlik dosyası seçin.";
            return RedirectToAction(nameof(Index));
        }

        var idCardUrl = await _fileService.UploadFileAsync(idCardImage, GetUserId());

        if (string.IsNullOrEmpty(idCardUrl))
        {
            TempData["ErrorMessage"] = "Kimlik yüklenirken hata oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var success = await _profileService.SubmitIdCardAsync(GetUserId(), idCardUrl);

        if (success)
        {
            TempData["SuccessMessage"] = "Kimlik kartı onaya gönderildi. Durumu bu sayfadan takip edebilirsiniz.";
        }
        else
        {
            TempData["ErrorMessage"] = "Kimlik gönderilirken bir hata oluştu.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Addresses")]
    public async Task<IActionResult> Addresses()
    {
        var addresses = await _profileService.GetUserAddressesAsync(GetUserId());
        return View(addresses);
    }

    [HttpGet("AddAddress")]
    public IActionResult AddAddress()
    {
        return View(new AddressViewModel());
    }

    [HttpPost("AddAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(AddressViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Formda eksik veya hatalı alanlar var.";
            return View(model);
        }

        var success = await _profileService.AddNewAddressAsync(GetUserId(), model);

        if (success)
        {
            TempData["SuccessMessage"] = "Yeni adres başarıyla eklendi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Adres eklenirken bir hata oluştu.";
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpGet("EditAddress/{id}")]
    public async Task<IActionResult> EditAddress(int id)
    {
        var address = await _profileService.GetUserAddressAsync(id);

        if (address == null || address.UserId != GetUserId())
        {
            TempData["ErrorMessage"] = "Adres bulunamadı veya yetkiniz yok.";
            return RedirectToAction(nameof(Addresses));
        }

        var model = _mapper.Map<AddressViewModel>(address);

        return View(model);
    }

    [HttpPost("EditAddress/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAddress(int id, AddressViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _profileService.UpdateAddressAsync(id, model, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Adres başarıyla güncellendi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Adres güncellenemedi veya yetkiniz yok.";
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("DeleteAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int addressId)
    {
        var success = await _profileService.DeleteAddressAsync(addressId, GetUserId());

        if (success)
        {
            TempData["SuccessMessage"] = "Adres silindi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Adres silinemedi. (Varsayılan adres olabilir veya yetkiniz yok).";
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("SetDefaultAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(int addressId)
    {
        var success = await _profileService.SetDefaultAddressAsync(GetUserId(), addressId);

        if (success)
        {
            TempData["SuccessMessage"] = "Varsayılan adres güncellendi.";
        }
        else
        {
            TempData["ErrorMessage"] = "İşlem sırasında bir hata oluştu.";
        }

        return RedirectToAction(nameof(Addresses));
    }
}