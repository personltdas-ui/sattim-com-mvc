using Sattim.Web.Models.Payment;
using Sattim.Web.Models.User;
using System.Threading.Tasks;

namespace Sattim.Web.Repositories.Interface
{
    public interface IGatewayService
    {
        Task<(bool Success, string HtmlContent, string ErrorMessage)> CreateCheckoutFormAsync(
            Payment payment,
            ApplicationUser user);
    }
}