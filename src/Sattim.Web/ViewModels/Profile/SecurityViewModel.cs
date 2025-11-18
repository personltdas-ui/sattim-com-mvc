namespace Sattim.Web.ViewModels.Profile
{
    public class SecurityViewModel
    {
        public bool IsEnabled { get; set; }
        public bool HasAuthenticator { get; set; }
        public int RecoveryCodesLeft { get; set; }
    }
}