using Microsoft.AspNetCore.Identity;
using Sattim.Web.Models.Security;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;

namespace Sattim.Web.Services.Account
{
    public class AccountService : IAccountService
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;


        private readonly IGenericRepository<Models.Wallet.Wallet> _walletRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<TwoFactorAuth> _2faRepo;


        private readonly IGenericRepository<SecurityLog> _securityLogRepo;


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

        public async Task<(IdentityResult Result, ApplicationUser User)> RegisterUserAsync(RegisterViewModel model, string ipAddress)
        {

            await using var transaction = await _context.Database.BeginTransactionAsync();


            var user = new ApplicationUser(
              userName: model.Email,
              email: model.Email,
              fullName: model.FullName
            );

            try
            {

                var identityResult = await _userManager.CreateAsync(user, model.Password);
                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync();

                    await LogSecurityEventAsync(SecurityEventType.FailedLogin, SeverityLevel.Warning, $"Kayıt başarısız: {identityResult.Errors.FirstOrDefault()?.Description}", ipAddress, null, model.Email);
                    return (identityResult, null);
                }

                _logger.LogInformation($"Kullanıcı (Identity) oluşturuldu: {user.Id}");


                await _userManager.AddToRoleAsync(user, "User");


                await _walletRepo.AddAsync(new Models.Wallet.Wallet(user.Id));
                await _profileRepo.AddAsync(new UserProfile(user.Id));
                await _2faRepo.AddAsync(new TwoFactorAuth(user.Id));


                await _securityLogRepo.AddAsync(new SecurityLog(
                  SecurityEventType.Register,
                  SeverityLevel.Info,
                  "Kullanıcı başarıyla kaydedildi.",
                  ipAddress,
                  user.Id
                ));


                await _context.SaveChangesAsync();


                await transaction.CommitAsync();

                _logger.LogInformation($"Kayıt işlemi tamamlandı (Commit). Kullanıcı: {user.Id}");
                return (identityResult, user);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogCritical(ex, $"Kayıt sırasında KRİTİK HATA (Rollback). Kullanıcı: {user.Email}");


                if (await _userManager.FindByEmailAsync(user.Email) != null)
                {
                    await _userManager.DeleteAsync(user);
                }

                return (IdentityResult.Failed(new IdentityError { Description = "Kayıt sırasında beklenmedik bir sistem hatası oluştu." }), null);
            }
        }

        public async Task<(Microsoft.AspNetCore.Identity.SignInResult Result, ApplicationUser User)> LoginUserAsync(LoginViewModel model, string ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);


            if (user == null || !user.IsActive)
            {
                await LogSecurityEventAsync(SecurityEventType.FailedLogin, SeverityLevel.Warning, "Giriş başarısız: Kullanıcı bulunamadı veya aktif değil.", ipAddress, user?.Id, model.Email);
                return (Microsoft.AspNetCore.Identity.SignInResult.Failed, null);
            }


            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                await LogSecurityEventAsync(SecurityEventType.FailedLogin, SeverityLevel.Warning, "Giriş başarısız: E-posta onaylanmamış.", ipAddress, user.Id, model.Email);
                return (Microsoft.AspNetCore.Identity.SignInResult.NotAllowed, user);
            }


            var result = await _signInManager.PasswordSignInAsync(
              user: user,
              password: model.Password,
              isPersistent: model.RememberMe,
              lockoutOnFailure: true
            );


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
                return IdentityResult.Success;
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                await LogSecurityEventAsync(SecurityEventType.PasswordChanged, SeverityLevel.Critical, "Şifre sıfırlandı.", "SYSTEM", user.Id, user.Email);
            }
            return result;
        }

        public async Task<(string sharedKey, string authenticatorUri)> LoadSharedKeyAndQrCodeAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);
            var appName = "Sattim.com";


            var sharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(sharedKey))
            {

                await _userManager.ResetAuthenticatorKeyAsync(user);
                sharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }


            var authenticatorUri = $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(user.Email)}?secret={sharedKey}&issuer={Uri.EscapeDataString(appName)}";

            _logger.LogInformation($"Kullanıcı {userId} için 2FA QR kodu oluşturuldu.");
            return (sharedKey, authenticatorUri);
        }

        public async Task<(IdentityResult Result, IEnumerable<string> Codes)> VerifyAndEnable2faAsync(string userId, string verificationCode)
        {
            var user = await GetUserByIdAsync(userId);


            var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
            if (!isTokenValid)
            {
                await LogSecurityEventAsync(SecurityEventType.TwoFactorDisabled, SeverityLevel.Error, "2FA etkinleştirme başarısız: Geçersiz token.", "SYSTEM", user.Id, user.Email);
                return (IdentityResult.Failed(new IdentityError { Description = "Doğrulama kodu geçersiz." }), null);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);

            var user2fa = await _2faRepo.GetByIdAsync(userId);
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            user2fa.Enable(
              await _userManager.GetAuthenticatorKeyAsync(user),
              System.Text.Json.JsonSerializer.Serialize(recoveryCodes)
            );
            _2faRepo.Update(user2fa);

            await LogSecurityEventAsync(SecurityEventType.TwoFactorEnabled, SeverityLevel.Critical, "2FA başarıyla etkinleştirildi.", "SYSTEM", user.Id, user.Email);
            await _2faRepo.UnitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Kullanıcı {userId} için 2FA etkinleştirildi.");
            return (IdentityResult.Success, recoveryCodes);
        }

        public async Task<IdentityResult> Disable2faAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);

            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!result.Succeeded) return result;

            var user2fa = await _2faRepo.GetByIdAsync(userId);
            user2fa.Disable();
            _2faRepo.Update(user2fa);

            await _userManager.ResetAuthenticatorKeyAsync(user);

            await LogSecurityEventAsync(SecurityEventType.TwoFactorDisabled, SeverityLevel.Critical, "2FA devre dışı bırakıldı.", "SYSTEM", user.Id, user.Email);
            await _2faRepo.UnitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Kullanıcı {userId} için 2FA devre dışı bırakıldı.");
            return result;
        }

        public async Task<(Microsoft.AspNetCore.Identity.SignInResult Result, ApplicationUser User)> LoginWith2faAsync(TwoFactorLoginViewModel model, string ipAddress)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                throw new InvalidOperationException("2FA doğrulama adımı için geçerli bir kullanıcı bulunamadı.");

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.TwoFactorCode, model.RememberMe, model.RememberMachine);

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

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(model.RecoveryCode);

            var logSeverity = result.Succeeded ? SeverityLevel.Critical : SeverityLevel.Error;
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

            user2fa.RegenerateBackupCodes(System.Text.Json.JsonSerializer.Serialize(newCodes));
            _2faRepo.Update(user2fa);
            await _2faRepo.UnitOfWork.SaveChangesAsync();

            await LogSecurityEventAsync(SecurityEventType.TwoFactorEnabled, SeverityLevel.Warning, "2FA yedek kodları yenilendi.", "SYSTEM", user.Id, user.Email);
            return newCodes;
        }

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
            return user.IsActive;
        }


        private async Task LogSecurityEventAsync(SecurityEventType type, SeverityLevel severity, string description, string ipAddress, string userId, string userEmail)
        {
            try
            {
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
    }
}