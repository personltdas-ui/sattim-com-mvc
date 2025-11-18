using Sattim.Web.ViewModels.Dispute;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Dispute
{

    public interface IDisputeService
    {
        Task<List<DisputeSummaryViewModel>> GetMyDisputesAsync(string userId);

        Task<DisputeDetailViewModel> GetMyDisputeDetailsAsync(int disputeId, string userId);

        Task<(bool Success, int? DisputeId, string ErrorMessage)> OpenDisputeAsync(OpenDisputeViewModel model, string buyerId);

        Task<(bool Success, string ErrorMessage)> AddDisputeMessageAsync(AddDisputeMessageViewModel model, string userId);
    }
}