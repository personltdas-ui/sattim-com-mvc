using Microsoft.AspNetCore.Identity;
using Sattim.Web.Models.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Management
{
    public interface IUserManagementService
    {
        
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();

        
        Task<ApplicationUser> GetUserByIdAsync(string userId);

        
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);
        Task<IdentityResult> AddUserToRoleAsync(string userId, string roleName);
        Task<IdentityResult> RemoveUserFromRoleAsync(string userId, string roleName);
        Task<IEnumerable<IdentityRole>> GetAllRolesAsync();

        Task<IdentityResult> DeactivateUserAsync(string userId, string adminNote);
               
        Task<IdentityResult> ActivateUserAsync(string userId, string adminNote);
                
        Task<bool> VerifyUserProfileAsync(string userId, string adminId);
                
        Task<bool> RejectUserProfileAsync(string userId, string adminId, string reason);
    }
}