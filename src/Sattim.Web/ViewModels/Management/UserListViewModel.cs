using System.Collections.Generic;

namespace Sattim.Web.ViewModels.Management
{
    /// <summary>
    /// Admin/UserManagement/Index sayfasındaki
    /// kullanıcı listesindeki tek bir satırı temsil eder.
    /// </summary>
    public class UserListViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}