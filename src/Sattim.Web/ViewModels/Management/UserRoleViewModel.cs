namespace Sattim.Web.ViewModels.Management
{
    /// <summary>
    /// UserDetailViewModel içinde rol atama formunu
    /// (checkbox listesi) oluşturmak için kullanılır.
    /// </summary>
    public class UserRoleViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}