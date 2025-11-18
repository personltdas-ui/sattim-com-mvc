using Microsoft.AspNetCore.Identity;
using Sattim.Web.Models.Security;
using Sattim.Web.Models.User;
using System.Collections.Generic;
using System.Threading.Tasks;



namespace Sattim.Web.Services.Account
{
    public interface IAccountService
    {
        
        Task<(IdentityResult Result, ApplicationUser User)> RegisterUserAsync(RegisterViewModel model, string ipAddress);

        
        Task<(Microsoft.AspNetCore.Identity.SignInResult Result, ApplicationUser User)> LoginUserAsync(LoginViewModel model, string ipAddress);
        Task LogoutUserAsync(string userId, string ipAddress);

        
        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);
        Task<IdentityResult> ConfirmEmailAsync(string userId, string token);

        
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordViewModel model);

        
        Task<(string sharedKey, string authenticatorUri)> LoadSharedKeyAndQrCodeAsync(string userId);
        Task<(IdentityResult Result, IEnumerable<string> Codes)> VerifyAndEnable2faAsync(string userId, string verificationCode);
        Task<IdentityResult> Disable2faAsync(string userId);
        Task<(Microsoft.AspNetCore.Identity.SignInResult Result, ApplicationUser User)> LoginWith2faAsync(TwoFactorLoginViewModel model, string ipAddress);
        Task<(Microsoft.AspNetCore.Identity.SignInResult Result, ApplicationUser User)> LoginWithRecoveryCodeAsync(RecoveryCodeLoginViewModel model, string ipAddress);
        Task<IEnumerable<string>> RegenerateRecoveryCodesAsync(string userId);

        
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<TwoFactorAuth> GetUser2faDetailsAsync(string userId);
        Task<bool> IsUserActiveAsync(string userId);
    }
}