using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Controllers; // BaseController için
using Sattim.Web.Models.User;
using Sattim.Web.Services.Profile; // IProfileService
using Sattim.Web.Services.Storage; // IFileStorageService VARSAYIMI
using Sattim.Web.ViewModels.Profile;
using System.Threading.Tasks;

[Authorize] // Bu controller'daki tüm metotlar giriş yapmış kullanıcı gerektirir
[Route("Profile")] // URL: /Profile, /Profile/Addresses, /Profile/AddAddress
public class UserProfileController : BaseController
{
    private readonly IProfileService _profileService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileService; // VARSAYIM 1

    public UserProfileController(
        IProfileService profileService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        IFileStorageService fileService) // VARSAYIM 1
    {
        _profileService = profileService;
        _userManager = userManager;
        _mapper = mapper;
        _fileService = fileService;
    }

    // Kullanıcının ID'sini güvenli bir şekilde alır.
    // [Authorize] olduğu için 'User' hiçbir zaman null olmaz.
    private string GetUserId() => _userManager.GetUserId(User)!;

    // ====================================================================
    //  1. Profil Detayları ve Doğrulama
    // ====================================================================

    /// <summary>
    /// Profilim (Ana) Sayfası - GET
    /// Kullanıcının ana profil bilgilerini (Ad/Soyad, Bio, Adres)
    /// düzenleyebileceği formu gösterir.
    /// </summary>
    // GET: /Profile
    
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        // 1. Gerekli tüm veritabanı modellerini alın
        var user = await _userManager.FindByIdAsync(userId);
        var profile = await _profileService.GetUserProfileAsync(userId); // Bu UserProfile tipinde

        // 2. View'ın beklediği 'ProfileDetailsViewModel' nesnesini OLUŞTURUN
        var viewModel = new ProfileDetailsViewModel
        {
            // 3. Verileri 'user' ve 'profile' nesnelerinden 'viewModel'e aktarın
            FullName = user.FullName, // ApplicationUser'dan
            Bio = profile.Bio,                 // UserProfile'dan
            Address = profile.Address,         // UserProfile'dan
            City = profile.City,               // UserProfile'dan
            Country = profile.Country,         // UserProfile'dan
            PostalCode = profile.PostalCode    // UserProfile'dan
        };

        // 4. ViewBag verilerini de eklemeyi unutmayın
        ViewBag.ProfileImageUrl = user.ProfileImageUrl;
        ViewBag.IsVerified = profile.IsVerified;

        // 5. View'a, tam olarak beklediği 'viewModel' (ProfileDetailsViewModel) nesnesini gönderin
        return View(viewModel);
    }

    /// <summary>
    /// Profilim (Ana) Sayfası - POST
    /// Kullanıcının ana profil bilgilerini günceller.
    /// </summary>
    // POST: /Profile
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

    /// <summary>
    /// Profil resmini günceller.
    /// </summary>
    // POST: /Profile/UpdateImage
    [HttpPost("UpdateImage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfileImage(IFormFile profileImage)
    {
        if (profileImage == null || profileImage.Length == 0)
        {
            TempData["ErrorMessage"] = "Lütfen bir resim dosyası seçin.";
            return RedirectToAction(nameof(Index));
        }

        // VARSAYIM 1: Dosyayı kaydet ve URL'i al
        var newImageUrl = await _fileService.UploadFileAsync(profileImage, GetUserId());

        if (string.IsNullOrEmpty(newImageUrl))
        {
            TempData["ErrorMessage"] = "Resim yüklenirken hata oluştu.";
            return RedirectToAction(nameof(Index));
        }

        // Servis katmanına sadece URL'i gönder
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

    /// <summary>
    /// Kimlik kartı doğrulamasını gönderir.
    /// </summary>
    // POST: /Profile/SubmitIdCard
    [HttpPost("SubmitIdCard")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitIdCard(IFormFile idCardImage)
    {
        if (idCardImage == null || idCardImage.Length == 0)
        {
            TempData["ErrorMessage"] = "Lütfen bir kimlik dosyası seçin.";
            return RedirectToAction(nameof(Index));
        }

        // VARSAYIM 1: Dosyayı kaydet ve URL'i al
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


    // ====================================================================
    //  2. Adres Defteri Yönetimi
    // ====================================================================

    /// <summary>
    /// Kullanıcının tüm kayıtlı adreslerini (Adres Defteri) listeler.
    /// </summary>
    // GET: /Profile/Addresses
    [HttpGet("Addresses")]
    public async Task<IActionResult> Addresses()
    {
        var addresses = await _profileService.GetUserAddressesAsync(GetUserId());
        return View(addresses); // (View, IEnumerable<UserAddress> modeli alır)
    }

    /// <summary>
    /// Yeni adres ekleme formunu gösterir (Genellikle bir Modal veya Partial View).
    /// </summary>
    // GET: /Profile/AddAddress
    [HttpGet("AddAddress")]
    public IActionResult AddAddress()
    {
        // Boş formu döndürür (veya Partial View)
        return View(new AddressViewModel());
    }

    /// <summary>
    /// Yeni adresi kaydeder.
    /// </summary>
    // POST: /Profile/AddAddress
    [HttpPost("AddAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(AddressViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Formda eksik veya hatalı alanlar var.";
            // Hata durumunda formu tekrar göstermek için 'Addresses'
            // sayfasına yönlendirmek yerine View döndürmek daha iyi olabilir
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

    /// <summary>
    /// Mevcut bir adresi düzenleme formunu (dolu) gösterir.
    /// </summary>
    // GET: /Profile/EditAddress/5
    [HttpGet("EditAddress/{id}")]
    public async Task<IActionResult> EditAddress(int id)
    {
        var address = await _profileService.GetUserAddressAsync(id);

        // GÜVENLİK: Adres yoksa VEYA adres bu kullanıcıya ait değilse
        if (address == null || address.UserId != GetUserId())
        {
            TempData["ErrorMessage"] = "Adres bulunamadı veya yetkiniz yok.";
            return RedirectToAction(nameof(Addresses));
        }

        // VARSAYIM 2: Entity -> ViewModel'e eşle
        var model = _mapper.Map<AddressViewModel>(address);

        return View(model); // Formu dolu olarak döndür
    }

    /// <summary>
    /// Mevcut adresi günceller.
    /// </summary>
    // POST: /Profile/EditAddress/5
    [HttpPost("EditAddress/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAddress(int id, AddressViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Servis katmanı zaten 'userId' alarak güvenlik kontrolü yapıyor
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

    /// <summary>
    /// Bir adresi siler.
    /// </summary>
    // POST: /Profile/DeleteAddress
    [HttpPost("DeleteAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int addressId) // Formdan 'addressId' olarak gelmeli
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

    /// <summary>
    /// Bir adresi varsayılan olarak ayarlar.
    /// </summary>
    // POST: /Profile/SetDefaultAddress
    [HttpPost("SetDefaultAddress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(int addressId) // Formdan 'addressId' olarak gelmeli
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