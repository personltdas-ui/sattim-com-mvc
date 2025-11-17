using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // Transaction ve AsNoTracking için
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // ApplicationDbContext için
using Sattim.Web.Models.Security; // SecurityLog için
using Sattim.Web.Models.User;
using Sattim.Web.Services.Repositories; // Repolar için
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Management
{
    /// <summary>
    /// Admin Paneli için kullanıcı yönetimi (Kullanıcılar, Roller, Banlama,
    /// Profil Onaylama) işlemlerini yöneten servis.
    /// </summary>
    public class UserManagementService : IUserManagementService
    {
        // Identity Servisleri (Yazma işlemleri için ZORUNLU)
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // Repository'ler (Okuma ve Domain Varlıklarını Yazma için)
        private readonly IUserRepository _userRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<SecurityLog> _securityLogRepo;

        // Transaction (Bütünleşik İşlem) Yönetimi için
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

        // ====================================================================
        //  Kullanıcı Okuma İşlemleri
        // ====================================================================

        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
        {
            // IUserRepository'mizdeki (veya IGenericRepository<ApplicationUser>)
            // AsNoTracking() kullanan metodu çağırıyoruz.
            return await _userRepo.GetAllAsync();
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            // UserManager, Identity varlıklarını bulmak için birincil yöntemdir.
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Admin panel: GetUserByIdAsync başarısız. Kullanıcı bulunamadı: {userId}");
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");
            }
            return user;
        }

        // ====================================================================
        //  Rol Yönetimi
        // ====================================================================

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IdentityResult> AddUserToRoleAsync(string userId, string roleName)
        {
            var user = await GetUserByIdAsync(userId);

            // Loglama (Admin eylemi)
            await LogAdminActionAsync(
                SecurityEventType.ProfileApproved, // (Daha spesifik bir Enum eklenebilir, örn: RoleChanged)
                SeverityLevel.Warning,
                $"Kullanıcıya '{roleName}' rolü eklendi.",
                "ADMIN_ID_BURAYA_GELMELI", // (Bu metodu çağıran adminin ID'si)
                userId
            );

            return await _userManager.AddToRoleAsync(user, roleName);
        }

        public async Task<IdentityResult> RemoveUserFromRoleAsync(string userId, string roleName)
        {
            var user = await GetUserByIdAsync(userId);

            await LogAdminActionAsync(
                SecurityEventType.ProfileRejected, // (Daha spesifik bir Enum eklenebilir)
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

        // ====================================================================
        //  Hesap Durumu (Ban/Unban) - (Transactional)
        // ====================================================================

        public async Task<IdentityResult> DeactivateUserAsync(string userId, string adminNote)
        {
            // Bu işlem hem ApplicationUser (bizim modelimiz) hem de
            // IdentityUser (UserManager) üzerinde değişiklik yapar.
            // Bu yüzden tek bir SaveChanges() çağrısı ile yönetilmelidir.

            var user = await GetUserByIdAsync(userId);

            // 1. İş Mantığını Modele Devret (IsActive = false)
            user.Deactivate();

            // 2. Identity Kilidini Ayarla (Modelin yaptığını doğrula)
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            if (!result.Succeeded)
            {
                _logger.LogError($"Kullanıcı {userId} için Identity kilidi (Lockout) ayarlanamadı.");
                return result; // Başarısız
            }

            // 3. Loglama
            await _securityLogRepo.AddAsync(new SecurityLog(
                SecurityEventType.AccountLocked,
                SeverityLevel.Critical,
                $"Hesap admin tarafından kilitlendi (Banlandı). Neden: {adminNote}",
                "SYSTEM", // (Admin'in IP/ID'si buraya gelmeli)
                user.Id
            ));

            // 4. Değişiklikleri Tek Seferde Kaydet
            // (Hem user.IsActive değişikliği hem de SecurityLog kaydı)
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Kullanıcı {userId} devre dışı bırakıldı (Banlandı).");
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> ActivateUserAsync(string userId, string adminNote)
        {
            var user = await GetUserByIdAsync(userId);

            // 1. İş Mantığını Modele Devret (IsActive = true)
            user.Activate();

            // 2. Identity Kilidini Kaldır
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
            {
                _logger.LogError($"Kullanıcı {userId} için Identity kilidi (Lockout) kaldırılamadı.");
                return result; // Başarısız
            }

            // 3. Loglama
            await _securityLogRepo.AddAsync(new SecurityLog(
                SecurityEventType.AccountUnlocked,
                SeverityLevel.Warning,
                $"Hesap kilidi (Ban) admin tarafından kaldırıldı. Not: {adminNote}",
                "SYSTEM",
                user.Id
            ));

            // 4. Değişiklikleri Tek Seferde Kaydet
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Kullanıcı {userId} etkinleştirildi (Ban kaldırıldı).");
            return IdentityResult.Success;
        }

        // ====================================================================
        //  Profil Doğrulama (Admin Tarafı)
        // ====================================================================

        public async Task<bool> VerifyUserProfileAsync(string userId, string adminId)
        {
            try
            {
                var profile = await _profileRepo.GetByIdAsync(userId);
                if (profile == null)
                    throw new KeyNotFoundException("Kullanıcı profili bulunamadı.");

                // 1. İş Mantığını Modele Devret
                profile.Verify(); // (Model, IsVerified = true yapar ve tarihi ayarlar)
                _profileRepo.Update(profile);

                // 2. Loglama
                await LogAdminActionAsync(
                    SecurityEventType.ProfileApproved,
                    SeverityLevel.Info,
                    "Profil kimlik onayı başarıyla yapıldı.",
                    adminId,
                    userId);

                // 3. Kaydet
                await _context.SaveChangesAsync(); // (Hem profil hem log kaydı)
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

                // 1. İş Mantığını Modele Devret
                profile.Unverify(); // (Model, IsVerified = false yapar)
                _profileRepo.Update(profile);

                // 2. Loglama
                await LogAdminActionAsync(
                    SecurityEventType.ProfileRejected,
                    SeverityLevel.Warning,
                    $"Profil kimlik onayı reddedildi. Neden: {reason}",
                    adminId,
                    userId);

                // 3. Kaydet
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

        // --- Yardımcı Metot ---
        private async Task LogAdminActionAsync(SecurityEventType type, SeverityLevel severity, string description, string adminId, string targetUserId)
        {
            // (Bu metot adminId'den IP adresini de alabilir)
            await _securityLogRepo.AddAsync(new SecurityLog(
                type,
                severity,
                $"Admin (ID: {adminId}) eylemi: {description}",
                "SYSTEM", // (Admin IP'si veya "SYSTEM"
                targetUserId
            ));
        }
    }
}