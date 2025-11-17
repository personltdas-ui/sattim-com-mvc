using Sattim.Web.Models.User; // UserProfile
using System.Collections.Generic;

namespace Sattim.Web.ViewModels.Management
{
    /// <summary>
    /// Admin/UserManagement/Detail sayfasını doldurur.
    /// Bir kullanıcı hakkındaki TÜM bilgileri birleştirir.
    /// </summary>
    public class UserDetailViewModel
    {
        public ApplicationUser User { get; set; }
        public UserProfile Profile { get; set; }
        public List<string> CurrentRoles { get; set; }

        // Rol yönetimi formu için
        public List<UserRoleViewModel> AllRoles { get; set; }
    }
}