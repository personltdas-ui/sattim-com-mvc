using Microsoft.AspNetCore.Identity;
using Sattim.Web.Models.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Management
{
    public interface IUserManagementService
    {
        /// <summary>
        /// Admin paneli için tüm kullanıcıları listeler.
        /// </summary>
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();

        /// <summary>
        /// Tek bir kullanıcıyı (ve ilişkili verilerini) getirir.
        /// </summary>
        Task<ApplicationUser> GetUserByIdAsync(string userId);

        // --- Rol Yönetimi ---
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);
        Task<IdentityResult> AddUserToRoleAsync(string userId, string roleName);
        Task<IdentityResult> RemoveUserFromRoleAsync(string userId, string roleName);
        Task<IEnumerable<IdentityRole>> GetAllRolesAsync();

        // --- Hesap Durumu (Ban/Unban) ---
        /// <summary>
        /// Kullanıcıyı devre dışı bırakır (Banlar).
        /// 'ApplicationUser.Deactivate()' ve Identity 'Lockout'unu tetikler.
        /// </summary>
        Task<IdentityResult> DeactivateUserAsync(string userId, string adminNote);

        /// <summary>
        /// Kullanıcının banını açar.
        /// 'ApplicationUser.Activate()' ve Identity 'Lockout'unu kaldırır.
        /// </summary>
        Task<IdentityResult> ActivateUserAsync(string userId, string adminNote);

        // --- Profil Doğrulama (Admin Tarafı) ---
        /// <summary>
        /// Kullanıcının yüklediği kimliği onaylar.
        /// 'UserProfile.Verify()' metodunu çağırır.
        /// </summary>
        Task<bool> VerifyUserProfileAsync(string userId, string adminId);

        /// <summary>
        /// Kullanıcının yüklediği kimliği reddeder.
        /// 'UserProfile.Unverify()' metodunu çağırır.
        /// </summary>
        Task<bool> RejectUserProfileAsync(string userId, string adminId, string reason);
    }
}