using Sattim.Web.ViewModels.Payment;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Payment
{
    public interface IPaymentService
    {
        Task<CheckoutViewModel> CreateGatewayCheckoutAsync(int escrowId, string userId);

        Task<(bool Success, string ErrorMessage)> PayWithWalletAsync(int escrowId, string userId);

        Task<bool> ProcessPaymentConfirmationAsync(PaymentConfirmationViewModel confirmation);
    }
}