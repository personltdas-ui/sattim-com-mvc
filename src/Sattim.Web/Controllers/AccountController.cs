using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sattim.Web.Models.User; // ApplicationUser
using Sattim.Web.Services.Account; // IAccountService
using Sattim.Web.Services.Email; // IEmailService
using Sattim.Web.ViewModels.Account; // LoginViewModel, RegisterViewModel vb.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Controllers
{
    // [Authorize] attribute'u, [AllowAnonymous] ile
    // işaretlenmemiş TÜM metotlar için giriş yapılmasını zorunlu kılar.
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly IAccountService _accountService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;


        public AccountController(
            IAccountService accountService,
            IEmailService emailService,
            ILogger<AccountController> logger,
            SignInManager<ApplicationUser> signInManager)
        {
            _accountService = accountService;
            _emailService = emailService;
            _logger = logger;
            _signInManager = signInManager;
        }

        // ====================================================================
        //  GİRİŞ (LOGIN)
        // ====================================================================

        [HttpGet]
        [AllowAnonymous] // Bu metot, giriş yapılmamışken de erişilebilir olmalı
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";

            // 1. Controller'ın görevi sadece servisi çağırmaktır
            var (result, user) = await _accountService.LoginUserAsync(model, ipAddress);

            // 2. Servisten dönen sonucu (SignInResult) işle
            if (result.Succeeded)
            {
                _logger.LogInformation($"Kullanıcı girişi başarılı: {model.Email}");
                return RedirectToLocal(returnUrl); // Güvenli yönlendirme
            }
            if (result.RequiresTwoFactor)
            {
                // 2FA (İki Faktörlü) gerekiyorsa, 2FA sayfasına yönlendir
                return RedirectToAction(nameof(LoginWith2FA), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning($"Kullanıcı hesabı kilitli: {model.Email}");
                ModelState.AddModelError(string.Empty, "Hesabınız çok fazla başarısız deneme nedeniyle kilitlendi. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }
            if (result.IsNotAllowed)
            {
                // IAccountService'te e-posta onayı (IsEmailConfirmed) kontrolü
                // eklediysek, bu hata döner.
                _logger.LogWarning($"Kullanıcı girişi engellendi (onaysız e-posta?): {model.Email}");
                ModelState.AddModelError(string.Empty, "Giriş yapabilmek için lütfen e-posta adresinizi onaylayın.");
                return View(model);
            }
            else
            {
                // Genel hata (örn: şifre yanlış)
                ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi. Lütfen e-postanızı ve şifrenizi kontrol edin.");
                return View(model);
            }
        }

        // ====================================================================
        //  KAYIT (REGISTER)
        // ====================================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";

            // 1. Servisi Çağır (IAccountService, bu işlemi 'transactional' yapar)
            var (result, user) = await _accountService.RegisterUserAsync(model, ipAddress);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Yeni kullanıcı kaydedildi: {model.Email}");

                // 2. E-posta Onay Token'ı Oluştur (Servisten)
                var token = await _accountService.GenerateEmailConfirmationTokenAsync(user);

                // 3. Callback (Geri Çağrı) URL'ini Oluştur (Controller)
                var callbackUrl = Url.Action(
                    action: nameof(ConfirmEmail),
                    controller: "Account",
                    values: new { userId = user.Id, token = token },
                    protocol: Request.Scheme);

                // 4. E-posta Servisini Çağır
                await _emailService.SendTemplateEmailAsync(
                    toEmail: user.Email,
                    templateName: "EmailVerification",
                    placeholders: new Dictionary<string, string>
                    {
                        { "UserName", user.FullName },
                        { "ConfirmationLink", callbackUrl }
                    }
                );

                TempData["SuccessMessage"] = "Kaydınız başarıyla tamamlandı. Lütfen e-posta adresinizi onaylamak için gelen kutunuzu (veya spam) kontrol edin.";
                return RedirectToAction("Index", "Home");
            }

            // Hata varsa:
            AddErrorsToModelState(result);
            return View(model);
        }

        // ====================================================================
        //  ÇIKIŞ (LOGOUT)
        // ====================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // (BaseController'dan gelir, [Authorize] olduğu için null olamaz)
            var userId = GetRequiredUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";

            await _accountService.LogoutUserAsync(userId, ipAddress);
            _logger.LogInformation($"Kullanıcı çıkış yaptı: {User.Identity.Name}");

            return RedirectToAction("Index", "Home");
        }

        // ====================================================================
        //  E-POSTA & ŞİFRE YÖNETİMİ
        // ====================================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var result = await _accountService.ConfirmEmailAsync(userId, token);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "E-posta adresiniz başarıyla onaylandı! Artık giriş yapabilirsiniz.";
            }
            else
            {
                TempData["ErrorMessage"] = "E-posta adresiniz onaylanırken bir hata oluştu. Lütfen tekrar deneyin.";
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _accountService.GetUserByEmailAsync(model.Email);

            // GÜVENLİK: Kullanıcı yoksa veya e-postası onaysızsa,
            // bunu KÖTÜ NİYETLİ kişilere belli etmiyoruz. "Sessizce" başarılı oluyoruz.
            if (user == null)
            {
                _logger.LogWarning($"Var olmayan veya onaysız e-posta için şifre sıfırlama denemesi: {model.Email}");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // 1. Token Oluştur (Servis)
            var token = await _accountService.GeneratePasswordResetTokenAsync(user);

            // 2. URL Oluştur (Controller)
            var callbackUrl = Url.Action(
                action: nameof(ResetPassword),
                controller: "Account",
                values: new { email = user.Email, token = token },
                protocol: Request.Scheme);

            // 3. E-posta Gönder (Servis)
            await _emailService.SendTemplateEmailAsync(
                toEmail: user.Email,
                templateName: "PasswordReset",
                placeholders: new Dictionary<string, string>
                {
                    { "UserName", user.FullName },
                    { "ResetLink", callbackUrl }
                }
            );

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View(); // (Sadece "E-postanızı kontrol edin" diyen bir sayfa)
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (email == null || token == null)
            {
                return BadRequest("Geçersiz bir şifre sıfırlama talebi.");
            }
            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Servis, kullanıcıyı bulup şifreyi (token'ı doğrulayarak) sıfırlar
            var result = await _accountService.ResetPasswordAsync(model);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz.";
                return RedirectToAction(nameof(Login));
            }

            AddErrorsToModelState(result);
            return View(model);
        }

        // ====================================================================
        //  İKİ FAKTÖRLÜ KİMLİK DOĞRULAMA (2FA)
        // ====================================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2FA(string returnUrl, bool rememberMe)
        {
            // (IAccountService.LoginUserAsync'ten yönlendirildi)
            // Önceki girişten gelen kullanıcıyı doğrula
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogWarning("LoginWith2FA: 2FA kullanıcısı (cookie) bulunamadı.");
                return RedirectToAction(nameof(Login));
            }

            var model = new TwoFactorLoginViewModel { ReturnUrl = returnUrl, RememberMe = rememberMe };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2FA(TwoFactorLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";
            var (result, user) = await _accountService.LoginWith2faAsync(model, ipAddress);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Kullanıcı 2FA ile giriş yaptı: {user.Email}");
                return RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning($"Kullanıcı 2FA denemesi (kilitli): {user?.Email}");
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Geçersiz doğrulama kodu.");
                return View(model);
            }
        }

        // (LoginWithRecoveryCodeAsync metodu da buraya eklenebilir, benzer mantıkta)

        [HttpGet]
        public IActionResult Lockout()
        {
            return View(); // (Hesabınız kilitlendi sayfası)
        }

        // ====================================================================
        //  YARDIMCI METOTLAR
        // ====================================================================

        private void AddErrorsToModelState(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}