using Microsoft.AspNetCore.Mvc;
using Sattim.Web.Services.Management;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Sattim.Web.ViewModels.Management;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Sattim.Web.Models.User;
using System.Collections.Generic;

namespace Sattim.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Kullanıcı yönetimi sadece Admin'e özel
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserManagementController(IUserManagementService userManagementService,
                                        UserManager<ApplicationUser> userManager)
        {
            _userManagementService = userManagementService;
            _userManager = userManager;
        }

        /// <summary>
        /// Tüm kullanıcıları listeler.
        /// Rota: /Admin/UserManagement
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var users = await _userManagementService.GetAllUsersAsync();

            // Gelen 'ApplicationUser' listesini 'UserListViewModel'e çeviriyoruz.
            var viewModel = new List<UserListViewModel>();
            foreach (var user in users)
            {
                viewModel.Add(new UserListViewModel
                {
                    UserId = user.Id,
                    // FullName = user.UserProfile?.FullName ?? user.UserName, // Profil varsa AdSoyad, yoksa KullanıcıAdı
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    IsActive = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow,
                    Roles = (await _userManagementService.GetUserRolesAsync(user.Id)).ToList()
                });
            }

            return View(viewModel);
        }

        /// <summary>
        /// Tek bir kullanıcının tüm detaylarını (rolleri, profili vb.) gösterir.
        /// Rota: /Admin/UserManagement/Detail/some-guid
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManagementService.GetUserByIdAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var userRoles = (await _userManagementService.GetUserRolesAsync(id)).ToList();
            var allRoles = await _userManagementService.GetAllRolesAsync();

            var viewModel = new UserDetailViewModel
            {
                User = user,
                //Profile = user.UserProfile, // Servis ApplicationUser'a Profile'ı dahil etmeli (Include)
                CurrentRoles = userRoles,
                AllRoles = allRoles.Select(r => new UserRoleViewModel
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    IsSelected = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Kullanıcı rollerini günceller (Detail sayfasındaki formdan gelir).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoles(string userId, List<UserRoleViewModel> allRoles)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var user = await _userManagementService.GetUserByIdAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var currentRoles = (await _userManagementService.GetUserRolesAsync(userId)).ToList();
            var selectedRoles = allRoles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList();

            // Eklenecek roller (Yeni seçilip, mevcut olmayanlar)
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            // Kaldırılacak roller (Mevcut olup, yeni listede seçili olmayanlar)
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

            foreach (var role in rolesToAdd)
            {
                await _userManagementService.AddUserToRoleAsync(userId, role);
            }
            foreach (var role in rolesToRemove)
            {
                await _userManagementService.RemoveUserFromRoleAsync(userId, role);
            }

            TempData["SuccessMessage"] = "Kullanıcı rolleri güncellendi.";
            return RedirectToAction(nameof(Detail), new { id = userId });
        }

        /// <summary>
        /// Bir kullanıcıyı banlar (hesabı kilitler).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(string userId, string banReason)
        {
            if (string.IsNullOrEmpty(banReason))
            {
                TempData["ErrorMessage"] = "Banlamak için bir sebep girmelisiniz.";
                return RedirectToAction(nameof(Detail), new { id = userId });
            }

            var result = await _userManagementService.DeactivateUserAsync(userId, banReason);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Kullanıcı başarıyla banlandı.";
            }
            else
            {
                TempData["ErrorMessage"] = "Banlama başarısız: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
            return RedirectToAction(nameof(Detail), new { id = userId });
        }

        /// <summary>
        /// Kullanıcının banını açar.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(string userId)
        {
            string adminNote = $"Admin ({User.Identity.Name}) tarafından ban kaldırıldı.";
            var result = await _userManagementService.ActivateUserAsync(userId, adminNote);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Kullanıcının banı başarıyla kaldırıldı.";
            }
            else
            {
                TempData["ErrorMessage"] = "Ban kaldırma başarısız: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
            return RedirectToAction(nameof(Detail), new { id = userId });
        }

        /// <summary>
        /// Kullanıcının kimlik/profil doğrulamasını onaylar.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyIdentity(string userId)
        {
            var adminId = _userManager.GetUserId(User);
            var result = await _userManagementService.VerifyUserProfileAsync(userId, adminId);

            if (result)
                TempData["SuccessMessage"] = "Profil başarıyla doğrulandı.";
            else
                TempData["ErrorMessage"] = "Profil doğrulama başarısız.";

            return RedirectToAction(nameof(Detail), new { id = userId });
        }

        /// <summary>
        /// Kullanıcının kimlik/profil doğrulamasını reddeder.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectIdentity(string userId, string rejectReason)
        {
            if (string.IsNullOrEmpty(rejectReason))
            {
                TempData["ErrorMessage"] = "Reddetmek için bir sebep girmelisiniz.";
                return RedirectToAction(nameof(Detail), new { id = userId });
            }

            var adminId = _userManager.GetUserId(User);
            var result = await _userManagementService.RejectUserProfileAsync(userId, adminId, rejectReason);

            if (result)
                TempData["SuccessMessage"] = "Profil başarıyla reddedildi.";
            else
                TempData["ErrorMessage"] = "Profil reddetme başarısız.";

            return RedirectToAction(nameof(Detail), new { id = userId });
        }
    }
}