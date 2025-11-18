using System.Collections.Generic;

namespace Sattim.Web.ViewModels.Management
{
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