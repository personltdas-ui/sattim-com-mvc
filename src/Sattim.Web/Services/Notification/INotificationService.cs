using Sattim.Web.Models.Analytical;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.Shipping;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet;
using Sattim.Web.ViewModels.Notification;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Notification
{
    public interface INotificationService
    {
        Task SendWelcomeNotificationAsync(ApplicationUser user);
        Task SendEmailConfirmationAsync(ApplicationUser user, string confirmationLink);
        Task SendPasswordResetAsync(ApplicationUser user, string resetLink);

        Task SendProductApprovedNotificationAsync(ApplicationUser seller, Models.Product.Product product);
        Task SendProductRejectedNotificationAsync(ApplicationUser seller, Models.Product.Product product, string reason);

        Task SendBidPlacedNotificationAsync(Models.Product.Product product, ApplicationUser bidder, decimal amount);
        Task SendBidOutbidNotificationAsync(ApplicationUser outbidUser, Models.Product.Product product);
        Task SendAuctionEndingSoonNotificationAsync(List<ApplicationUser> usersToNotify, Models.Product.Product product);

        Task SendAuctionWonNotificationAsync(ApplicationUser winner, Models.Product.Product product);
        Task SendAuctionLostNotificationAsync(List<string> loserIds, Models.Product.Product product);
        Task SendProductSoldNotificationAsync(ApplicationUser seller, Models.Product.Product product);
        Task SendAuctionNotSoldNotificationAsync(ApplicationUser seller, Models.Product.Product product);

        Task SendPaymentSuccessNotificationAsync(ApplicationUser buyer, Models.Product.Product product);
        Task SendSellerPaymentReceivedNotificationAsync(ApplicationUser seller, Models.Product.Product product);
        Task SendProductShippedNotificationAsync(ApplicationUser buyer, Models.Product.Product product, ShippingInfo shippingInfo);
        Task SendProductDeliveredNotificationAsync(ApplicationUser seller, Models.Product.Product product);

        Task SendFundsReleasedNotificationAsync(ApplicationUser seller, decimal netAmount);
        Task SendPayoutRequestedNotificationAsync(ApplicationUser user, PayoutRequest request);
        Task SendPayoutApprovedNotificationAsync(ApplicationUser user, PayoutRequest request);
        Task SendPayoutRejectedNotificationAsync(ApplicationUser user, PayoutRequest request, string reason);
        Task SendPayoutCompletedNotificationAsync(ApplicationUser user, PayoutRequest request);

        Task SendNewMessageNotificationAsync(ApplicationUser receiver, ApplicationUser sender, int messageId);
        Task SendDisputeOpenedNotificationAsync(ApplicationUser buyer, ApplicationUser seller, Models.Dispute.Dispute dispute);
        Task SendDisputeNewMessageNotificationAsync(ApplicationUser receiver, ApplicationUser sender, Models.Dispute.Dispute dispute);
        Task SendDisputeResolvedNotificationAsync(ApplicationUser user, int disputeId, string resolution);
        Task SendCommentNeedsApprovalNotificationAsync(BlogComment comment);
        Task SendNewReportToAdminsNotificationAsync(Models.Analytical.Report report);

        Task<List<NotificationViewModel>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);

        Task<int> GetUnreadNotificationCountAsync(string userId);

        Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId);

        Task<bool> MarkAllNotificationsAsReadAsync(string userId);
    }
}