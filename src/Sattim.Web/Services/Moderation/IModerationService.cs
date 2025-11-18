using Sattim.Web.ViewModels.Dispute;
using Sattim.Web.ViewModels.Moderation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Moderation
{
    public interface IModerationService
    {
        Task<List<ViewModels.Moderation.ReportViewModel>> GetPendingReportsAsync();

        Task<bool> MarkReportAsUnderReviewAsync(int reportId, string adminId);

        Task<bool> ResolveReportAsync(int reportId, string adminId, string resolutionNote);

        Task<bool> RejectReportAsync(int reportId, string adminId, string rejectionNote);

        Task<List<ViewModels.Moderation.DisputeViewModel>> GetPendingDisputesAsync();

        Task<ViewModels.Moderation.DisputeDetailViewModel> GetDisputeDetailsAsync(int disputeId);

        Task<bool> AddDisputeMessageAsync(int disputeId, string adminId, string message);

        Task<bool> ResolveDisputeForSellerAsync(int disputeId, string adminId, string resolutionNote);

        Task<bool> ResolveDisputeForBuyerAsync(int disputeId, string adminId, string resolutionNote);

        Task<List<CommentModerationViewModel>> GetPendingCommentsAsync();

        Task<bool> ApproveCommentAsync(int commentId, string adminId);

        Task<bool> RejectCommentAsync(int commentId, string adminId);

        Task<List<ProductModerationViewModel>> GetPendingProductsAsync();
    }
}