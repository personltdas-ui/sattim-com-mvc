using Sattim.Web.Models.User;
using System.Collections.Generic;

namespace Sattim.Web.ViewModels.Management
{
    public class UserDetailViewModel
    {
        public ApplicationUser User { get; set; }
        public UserProfile Profile { get; set; }
        public List<string> CurrentRoles { get; set; }

        public List<UserRoleViewModel> AllRoles { get; set; }
    }
}