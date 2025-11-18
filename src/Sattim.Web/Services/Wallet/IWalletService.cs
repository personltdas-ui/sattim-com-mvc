using Sattim.Web.ViewModels.Wallet;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Wallet
{
    public interface IWalletService
    {
        Task<WalletDashboardViewModel> GetWalletDashboardAsync(string userId);

        Task<(bool Success, string ErrorMessage)> RequestPayoutAsync(PayoutRequestViewModel model, string userId);

        Task<(bool Success, string ErrorMessage)> ReleaseFundsToSellerAsync(int escrowId, string adminOrSystemUserId);

        Task<(bool Success, string ErrorMessage)> ApprovePayoutAsync(int payoutRequestId, string adminId);

        Task<(bool Success, string ErrorMessage)> CompletePayoutAsync(int payoutRequestId, string adminId);

        Task<(bool Success, string ErrorMessage)> RejectPayoutAsync(int payoutRequestId, string adminId, string reason);

        Task<(bool Success, string ErrorMessage)> RefundEscrowToBuyerAsync(int escrowId, string adminId, string reason);
    }
}