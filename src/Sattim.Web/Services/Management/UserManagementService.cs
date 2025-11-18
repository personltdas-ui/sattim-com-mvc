using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Security;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Management
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly IUserRepository _userRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<SecurityLog> _securityLogRepo;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
          UserManager<ApplicationUser> userManager,
          RoleManager<IdentityRole> roleManager,
          IUserRepository userRepo,
          IGenericRepository<UserProfile> profileRepo,
          IGenericRepository<SecurityLog> securityLogRepo,
          ApplicationDbContext context,
          ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepo = userRepo;
            _profileRepo = profileRepo;
            _securityLogRepo = securityLogRepo;
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
        {
            return await _userRepo.GetAllAsync();
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Admin panel: GetUserByIdAsync başarısız. Kullanıcı bulunamadı: {userId}");
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");
            }
            return user;
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IdentityResult> AddUserToRoleAsync(string userId, string roleName)
        {
            var user = await GetUserByIdAsync(userId);

            await LogAdminActionAsync(
              SecurityEventType.ProfileApproved,
              SeverityLevel.Warning,
              $"Kullanıcıya '{roleName}' rolü eklendi.",
              "ADMIN_ID_BURAYA_GELMELI",
              userId
            );

            return await _userManager.AddToRoleAsync(user, roleName);
        }

        public async Task<IdentityResult> RemoveUserFromRoleAsync(string userId, string roleName)
        {
            var user = await GetUserByIdAsync(userId);

            await LogAdminActionAsync(
              SecurityEventType.ProfileRejected,
              SeverityLevel.Warning,
              $"Kullanıcıdan '{roleName}' rolü kaldırıldı.",
              "ADMIN_ID_BURAYA_GELMELI",
              userId
            );

            return await _userManager.RemoveFromRoleAsync(user, roleName);
        }

        public async Task<IEnumerable<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.AsNoTracking().ToListAsync();
        }

        public async Task<IdentityResult> DeactivateUserAsync(string userId, string adminNote)
        {
            var user = await GetUserByIdAsync(userId);

            user.Deactivate();

            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            if (!result.Succeeded)
            {
                _logger.LogError($"Kullanıcı {userId} için Identity kilidi (Lockout) ayarlanamadı.");
                return result;
            }

            await _securityLogRepo.AddAsync(new SecurityLog(
              SecurityEventType.AccountLocked,
              SeverityLevel.Critical,
              $"Hesap admin tarafından kilitlendi (Banlandı). Neden: {adminNote}",
              "SYSTEM",
              user.Id
            ));

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Kullanıcı {userId} devre dışı bırakıldı (Banlandı).");
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> ActivateUserAsync(string userId, string adminNote)
        {
            var user = await GetUserByIdAsync(userId);

            user.Activate();

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
            {
                _logger.LogError($"Kullanıcı {userId} için Identity kilidi (Lockout) kaldırılamadı.");
                return result;
            }

            await _securityLogRepo.AddAsync(new SecurityLog(
              SecurityEventType.AccountUnlocked,
              SeverityLevel.Warning,
              $"Hesap kilidi (Ban) admin tarafından kaldırıldı. Not: {adminNote}",
              "SYSTEM",
              user.Id
            ));

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Kullanıcı {userId} etkinleştirildi (Ban kaldırıldı).");
            return IdentityResult.Success;
        }

        public async Task<bool> VerifyUserProfileAsync(string userId, string adminId)
        {
            try
            {
                var profile = await _profileRepo.GetByIdAsync(userId);
                if (profile == null)
                    throw new KeyNotFoundException("Kullanıcı profili bulunamadı.");

                profile.Verify();
                _profileRepo.Update(profile);

                await LogAdminActionAsync(
                  SecurityEventType.ProfileApproved,
                  SeverityLevel.Info,
                  "Profil kimlik onayı başarıyla yapıldı.",
                  adminId,
                  userId);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Profil onaylandı (Kullanıcı: {userId}). Onaylayan: {adminId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Profil onaylanırken hata (Kullanıcı: {userId}).");
                return false;
            }
        }

        public async Task<bool> RejectUserProfileAsync(string userId, string adminId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reddetme nedeni zorunludur.");

            try
            {
                var profile = await _profileRepo.GetByIdAsync(userId);
                if (profile == null)
                    throw new KeyNotFoundException("Kullanıcı profili bulunamadı.");

                profile.Unverify();
                _profileRepo.Update(profile);

                await LogAdminActionAsync(
                  SecurityEventType.ProfileRejected,
                  SeverityLevel.Warning,
                  $"Profil kimlik onayı reddedildi. Neden: {reason}",
                  adminId,
                  userId);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Profil reddedildi (Kullanıcı: {userId}). Reddeden: {adminId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Profil reddedilirken hata (Kullanıcı: {userId}).");
                return false;
            }
        }

        private async Task LogAdminActionAsync(SecurityEventType type, SeverityLevel severity, string description, string adminId, string targetUserId)
        {
            await _securityLogRepo.AddAsync(new SecurityLog(
              type,
              severity,
              $"Admin (ID: {adminId}) eylemi: {description}",
              "SYSTEM",
              targetUserId
            ));
        }
    }
}