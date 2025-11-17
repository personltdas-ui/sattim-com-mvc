using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // Transaction için
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // ApplicationDbContext için
using Sattim.Web.Models.Security;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet;
using Sattim.Web.Services.Repositories; // IGenericRepository için
using System;
using System.Collections.Generic;
using System.Linq; // FirstOrDefault
using System.Threading.Tasks;

namespace Sattim.Web.Services.Account
{
    public class AccountService : IAccountService
    {
        // 1. Identity'nin KENDİ servisleri
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        // 2. Bizim 1-1 ilişkiler için JENERİK repolarımız
        private readonly IGenericRepository<Models.Wallet.Wallet> _walletRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<TwoFactorAuth> _2faRepo;

        // 3. Bizim Loglama repomuz
        private readonly IGenericRepository<SecurityLog> _securityLogRepo;

        // 4. Transaction yönetimi için DbContext'in kendisi
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IGenericRepository<Models.Wallet.Wallet> walletRepo,
            IGenericRepository<UserProfile> profileRepo,
            IGenericRepository<TwoFactorAuth> twoFactorRepo,
            IGenericRepository<SecurityLog> securityLogRepo,
            ApplicationDbContext context,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _walletRepo = walletRepo;
            _profileRepo = profileRepo;
            _2faRepo = twoFactorRepo;
            _securityLogRepo = securityLogRepo;
            _context = context;
            _logger = logger;
        }

        #region --- Kayıt (Transactional) ---

        /// <summary>
        /// Kullanıcıyı ve ilgili tüm 1-1 varlıklarını (Wallet, Profile, 2FA)
        /// tek bir transaction (bütünleşik işlem) içinde oluşturur.
        /// </summary>
        public async Task<(IdentityResult Result, ApplicationUser User)> RegisterUserAsync(RegisterViewModel model, string ipAddress)
        {
            // 1. Transaction'ı Başlat
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 2. Varlığı Oluştur (Modelimizin Constructor'ı ile)
            var user = new ApplicationUser(
                userName: model.Email,
                email: model.Email,
                fullName: model.FullName
            );

            try
            {
                // 3. Identity: Kullanıcıyı (hash'lenmiş şifreyle) oluştur
                var identityResult = await _userManager.CreateAsync(user, model.Password);
                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync(); // Hata varsa işlemi geri al
                    // Başarısız kayıt denemesini log'la
                    await LogSecurityEventAsync(SecurityEventType.FailedLogin, SeverityLevel.Warning, $"Kayıt başarısız: {identityResult.Errors.FirstOrDefault()?.Description}", ipAddress, null, model.Email);
                    return (identityResult, null);
                }

                _logger.LogInformation($"Kullanıcı (Identity) oluşturuldu: {user.Id}");

                // 4. Identity: Rolü ata
                await _userManager.AddToRoleAsync(user, "User");

                // 5. Bizim 1-1 Varlıklarımız: İlişkili kayıtları oluştur
                await _walletRepo.AddAsync(new Models.Wallet.Wallet(user.Id));
                await _profileRepo.AddAsync(new UserProfile(user.Id));
                await _2faRepo.AddAsync(new TwoFactorAuth(user.Id));

                // 6. Loglama: Başarılı kaydı log'la
                await _securityLogRepo.AddAsync(new SecurityLog(
                    SecurityEventType.Register,
                    SeverityLevel.Info,
                    "Kullanıcı başarıyla kaydedildi.",
                    ipAddress,
                    user.Id
                ));

                // 7. Tüm değişiklikleri (Kullanıcı, Rol, Cüzdan, Profil, Log) kaydet
                await _context.SaveChangesAsync();

                // 8. Transaction'ı Onayla
                await transaction.CommitAsync();

                _logger.LogInformation($"Kayıt işlemi tamamlandı (Commit). Kullanıcı: {user.Id}");
                return (identityResult, user);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // KRİTİK HATA: Hepsini geri al
                _logger.LogCritical(ex, $"Kayıt sırasında KRİTİK HATA (Rollback). Kullanıcı: {user.Email}");

                // Kullanıcı oluşturulduysa (ama 1-1 ilişkilerde hata olduysa) Identity'den sil
                if (await _userManager.FindByEmailAsync(user.Email) != null)
                {
                    await _userManager.DeleteAsync(user);
                }

                return (IdentityResult.Failed(new IdentityError { Description = "Kayıt sırasında beklenmedik bir sistem hatası oluştu." }), null);
            }
        }

        #endregion

        #region --- Giriş / Çıkış ve Loglama ---

        public async Task<(Microsoft.AspNetCore.Identity.SignInResult Result, ApplicationUser User)> LoginUserAsync(LoginViewModel model, string ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            // 1. Kendi Özel İş Kuralımız: Kullanıcı 'IsActive' mi?
            if (user == null || !user.IsActive)
            {
                await LogSecurityEventAsync(SecurityEventType.FailedLogin, SeverityLevel.Warning, "Giriş başarısız: Kullanıcı bulunamadı veya aktif değil.", ipAddress, user?.Id, model.Email);
                return (Microsoft.AspNetCore.Identity.SignInResult.Failed, null);
            }

            // 2. Kendi Özel İş Kuralımız: E-posta onaylı mı? (Opsiyonel ama önerilir)
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                await LogSecurityEventAsync(SecurityEventType.FailedLogin, SeverityLevel.Warning, "Giriş başarısız: E-posta onaylanmamış.", ipAddress, user.Id, model.Email);
                return (Microsoft.AspNetCore.Identity.SignInResult.NotAllowed, user); // 'NotAllowed'
            }

            // 3. Identity: Giriş yapmayı dene (Cookie oluştur)
            var result = await _signInManager.PasswordSignInAsync(
                user: user,
                password: model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: true // 5 kez yanlış girerse kilitle
            );

            // 4. Loglama
            if (result.Succeeded)
            {
                await LogSecurityEventAsync(SecurityEventType.Login, SeverityLevel.Info, "Kullanıcı girişi başarılı.", ipAddress, user.Id, model.Email);
            }
            else
            {
                string reason = result.IsLockedOut ? "Hesap kilitlendi." : (result.RequiresTwoFactor ? "2FA gerekli." : "Şifre yanlış.");
                await LogSecurityEventAsync(SecurityEventType.FailedLogin, SeverityLevel.Warning, $"Giriş başarısız: {reason}", ipAddress, user.Id, model.Email);
            }

            return (result, user);
        }

        public async Task LogoutUserAsync(string userId, string ipAddress)
        {
            await _signInManager.SignOutAsync();
            await LogSecurityEventAsync(SecurityEventType.Logout, SeverityLevel.Info, "Kullanıcı çıkışı başarılı.", ipAddress, userId, null);
        }

        #endregion

        #region --- E-posta ve Şifre (Identity'yi Kullanır) ---
        // Bu metotlar SADECE Identity'nin yerleşik token'larını çağırır.
        // Repository gerekmez.

        public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user)
        {
            return await _userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        public async Task<IdentityResult> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "Kullanıcı bulunamadı." });

            return await _userManager.ConfirmEmailAsync(user, token);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Güvenlik: Kullanıcının var olup olmadığını belli etme
                return IdentityResult.Success;
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                await LogSecurityEventAsync(SecurityEventType.PasswordChanged, SeverityLevel.Critical, "Şifre sıfırlandı.", "SYSTEM", user.Id, user.Email);
            }
            return result;
        }

        #endregion

        #region --- 2FA (Identity ve Kendi Modelimizi Kullanır) ---

        public async Task<(string sharedKey, string authenticatorUri)> LoadSharedKeyAndQrCodeAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);
            var appName = "Sattim.com"; // SiteSettings'ten çekilebilir

            // 1. Identity'nin hafızasındaki anahtarı al
            var sharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(sharedKey))
            {
                // 2. Yoksa, yeni bir anahtar oluştur ve Identity'ye kaydet
                await _userManager.ResetAuthenticatorKeyAsync(user);
                sharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            // 3. QR Kod URL'ini oluştur
            var authenticatorUri = $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(user.Email)}?secret={sharedKey}&issuer={Uri.EscapeDataString(appName)}";

            _logger.LogInformation($"Kullanıcı {userId} için 2FA QR kodu oluşturuldu.");
            return (sharedKey, authenticatorUri);
        }

        public async Task<(IdentityResult Result, IEnumerable<string> Codes)> VerifyAndEnable2faAsync(string userId, string verificationCode)
        {
            var user = await GetUserByIdAsync(userId);

            // 1. Gelen kodun, kullanıcının 'sharedKey'i ile eşleşip eşleşmediğini doğrula
            var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
            if (!isTokenValid)
            {
                await LogSecurityEventAsync(SecurityEventType.TwoFactorDisabled, SeverityLevel.Error, "2FA etkinleştirme başarısız: Geçersiz token.", "SYSTEM", user.Id, user.Email);
                return (IdentityResult.Failed(new IdentityError { Description = "Doğrulama kodu geçersiz." }), null);
            }

            // 2. Identity'de 2FA'yı AÇ
            await _userManager.SetTwoFactorEnabledAsync(user, true);

            // 3. Kendi 2FA Modelimizi Güncelle (Model Metoduyla)
            var user2fa = await _2faRepo.GetByIdAsync(userId);
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            user2fa.Enable(
                await _userManager.GetAuthenticatorKeyAsync(user), // Identity'nin anahtarını kendi tablomuza da kaydet
                System.Text.Json.JsonSerializer.Serialize(recoveryCodes) // Kodları JSON'a çevir
            );
            _2faRepo.Update(user2fa);

            // 4. Log'la ve Kaydet
            await LogSecurityEventAsync(SecurityEventType.TwoFactorEnabled, SeverityLevel.Critical, "2FA başarıyla etkinleştirildi.", "SYSTEM", user.Id, user.Email);
            await _2faRepo.UnitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Kullanıcı {userId} için 2FA etkinleştirildi.");
            return (IdentityResult.Success, recoveryCodes);
        }

        public async Task<IdentityResult> Disable2faAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);

            // 1. Identity'de 2FA'yı KAPAT
            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!result.Succeeded) return result;

            // 2. Kendi 2FA Modelimizi Güncelle (Model Metoduyla)
            var user2fa = await _2faRepo.GetByIdAsync(userId);
            user2fa.Disable();
            _2faRepo.Update(user2fa);

            // 3. Identity'nin anahtarını sıfırla (temizle)
            await _userManager.ResetAuthenticatorKeyAsync(user);

            // 4. Log'la ve Kaydet
            await LogSecurityEventAsync(SecurityEventType.TwoFactorDisabled, SeverityLevel.Critical, "2FA devre dışı bırakıldı.", "SYSTEM", user.Id, user.Email);
            await _2faRepo.UnitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Kullanıcı {userId} için 2FA devre dışı bırakıldı.");
            return result;
        }

        public async Task<(Microsoft.AspNetCore.Identity.SignInResult Result, ApplicationUser User)> LoginWith2faAsync(TwoFactorLoginViewModel model, string ipAddress)
        {
            // 1. Önceki girişten (PasswordSignInAsync) gelen kullanıcıyı al
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                throw new InvalidOperationException("2FA doğrulama adımı için geçerli bir kullanıcı bulunamadı.");

            // 2. Kodu doğrula
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.TwoFactorCode, model.RememberMe, model.RememberMachine);

            // 3. Loglama
            var logSeverity = result.Succeeded ? SeverityLevel.Info : SeverityLevel.Error;
            var logMessage = result.Succeeded ? "2FA girişi başarılı." : "2FA girişi başarısız: Kod geçersiz.";
            await LogSecurityEventAsync(SecurityEventType.Login, logSeverity, logMessage, ipAddress, user.Id, user.Email);

            return (result, user);
        }

        public async Task<(Microsoft.AspNetCore.Identity.SignInResult Result, ApplicationUser User)> LoginWithRecoveryCodeAsync(RecoveryCodeLoginViewModel model, string ipAddress)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                throw new InvalidOperationException("2FA doğrulama adımı için geçerli bir kullanıcı bulunamadı.");

            // 2. Yedek kodu doğrula
            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(model.RecoveryCode);

            // 3. Loglama
            var logSeverity = result.Succeeded ? SeverityLevel.Critical : SeverityLevel.Error; // Yedek kod kullanımı KRİTİKTİR
            var logMessage = result.Succeeded ? "2FA girişi YEDEK KOD ile başarılı." : "2FA girişi başarısız: Yedek kod geçersiz.";
            await LogSecurityEventAsync(SecurityEventType.Login, logSeverity, logMessage, ipAddress, user.Id, user.Email);

            return (result, user);
        }

        public async Task<IEnumerable<string>> RegenerateRecoveryCodesAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);
            var user2fa = await _2faRepo.GetByIdAsync(userId);
            if (user == null || user2fa == null || !user2fa.IsEnabled)
                throw new InvalidOperationException("2FA etkin olmayan bir kullanıcı için yedek kod üretilemez.");

            var newCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            // Kendi modelimizi de güncelle
            user2fa.RegenerateBackupCodes(System.Text.Json.JsonSerializer.Serialize(newCodes));
            _2faRepo.Update(user2fa);
            await _2faRepo.UnitOfWork.SaveChangesAsync();

            await LogSecurityEventAsync(SecurityEventType.TwoFactorEnabled, SeverityLevel.Warning, "2FA yedek kodları yenilendi.", "SYSTEM", user.Id, user.Email);
            return newCodes;
        }

        #endregion

        #region --- Yardımcılar ---

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");
            return user;
        }

        public async Task<TwoFactorAuth> GetUser2faDetailsAsync(string userId)
        {
            var user2fa = await _2faRepo.GetByIdAsync(userId);
            if (user2fa == null)
                throw new KeyNotFoundException("2FA ayarları bulunamadı.");
            return user2fa;
        }

        public async Task<bool> IsUserActiveAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);
            return user.IsActive; // Bizim özel IsActive bayrağımız
        }

        // Özel Loglama Yardımcı Metodu
        private async Task LogSecurityEventAsync(SecurityEventType type, SeverityLevel severity, string description, string ipAddress, string userId, string userEmail)
        {
            try
            {
                // 'UserId' null ise (örn: şifre yanlış girildi), e-postadan bulmayı dene
                if (string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    var user = await _userManager.FindByEmailAsync(userEmail);
                    if (user != null) userId = user.Id;
                }

                await _securityLogRepo.AddAsync(new SecurityLog(
                    type,
                    severity,
                    description,
                    ipAddress,
                    userId
                ));
                await _securityLogRepo.UnitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Güvenlik loglaması ({type}) yazılırken KRİTİK HATA.");
            }
        }

        #endregion
    }
}