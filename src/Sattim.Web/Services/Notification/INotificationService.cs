using Sattim.Web.Models.Analytical; // Report için
using Sattim.Web.Models.Blog; // BlogComment için
using Sattim.Web.Models.Dispute; // Dispute için
using Sattim.Web.Models.Product;
using Sattim.Web.Models.Shipping;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet; // PayoutRequest için
using Sattim.Web.ViewModels.Notification;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Notification
{
    public interface INotificationService
    {
        // ====================================================================
        //  COMMANDS (Bildirim Gönderme)
        // ====================================================================

        // --- Hesap İşlemleri (IAccountService) ---
        Task SendWelcomeNotificationAsync(ApplicationUser user);
        Task SendEmailConfirmationAsync(ApplicationUser user, string confirmationLink);
        Task SendPasswordResetAsync(ApplicationUser user, string resetLink);

        // --- Ürün Moderasyon (IModerationService / IProductService) ---
        Task SendProductApprovedNotificationAsync(ApplicationUser seller, Models.Product.Product product);
        Task SendProductRejectedNotificationAsync(ApplicationUser seller, Models.Product.Product product, string reason);

        // --- Teklif & İhale (IBidService / IAuctionJobService) ---
        Task SendBidPlacedNotificationAsync(Models.Product.Product product, ApplicationUser bidder, decimal amount);
        Task SendBidOutbidNotificationAsync(ApplicationUser outbidUser, Models.Product.Product product);
        Task SendAuctionEndingSoonNotificationAsync(List<ApplicationUser> usersToNotify, Models.Product.Product product);

        // --- Sipariş & Satış (IOrderService) ---
        Task SendAuctionWonNotificationAsync(ApplicationUser winner, Models.Product.Product product);
        Task SendAuctionLostNotificationAsync(List<string> loserIds, Models.Product.Product product);
        Task SendProductSoldNotificationAsync(ApplicationUser seller, Models.Product.Product product);
        Task SendAuctionNotSoldNotificationAsync(ApplicationUser seller, Models.Product.Product product);

        // --- Ödeme & Kargo (IPaymentService / IShippingService) ---
        Task SendPaymentSuccessNotificationAsync(ApplicationUser buyer, Models.Product.Product product);
        Task SendSellerPaymentReceivedNotificationAsync(ApplicationUser seller, Models.Product.Product product);
        Task SendProductShippedNotificationAsync(ApplicationUser buyer, Models.Product.Product product, ShippingInfo shippingInfo);
        Task SendProductDeliveredNotificationAsync(ApplicationUser seller, Models.Product.Product product);

        // --- Cüzdan & Para Çekme (IWalletService) ---
        Task SendFundsReleasedNotificationAsync(ApplicationUser seller, decimal netAmount);
        Task SendPayoutRequestedNotificationAsync(ApplicationUser user, PayoutRequest request);
        Task SendPayoutApprovedNotificationAsync(ApplicationUser user, PayoutRequest request);
        Task SendPayoutRejectedNotificationAsync(ApplicationUser user, PayoutRequest request, string reason);
        Task SendPayoutCompletedNotificationAsync(ApplicationUser user, PayoutRequest request);

        // --- İletişim & Moderasyon (IDisputeService / IBlogService / IReportService) ---
        Task SendNewMessageNotificationAsync(ApplicationUser receiver, ApplicationUser sender, int messageId);
        Task SendDisputeOpenedNotificationAsync(ApplicationUser buyer, ApplicationUser seller, Models.Dispute.Dispute dispute); // YENİ
        Task SendDisputeNewMessageNotificationAsync(ApplicationUser receiver, ApplicationUser sender, Models.Dispute.Dispute dispute); // YENİ
        Task SendDisputeResolvedNotificationAsync(ApplicationUser user, int disputeId, string resolution);
        Task SendCommentNeedsApprovalNotificationAsync(BlogComment comment); // YENİ
        Task SendNewReportToAdminsNotificationAsync(Models.Analytical.Report report); // YENİ (Opsiyonel)


        // ====================================================================
        //  QUERIES (Bildirim Okuma)
        // ====================================================================

        /// <summary>
        /// Kullanıcının "Bildirimlerim" sayfasını getirir.
        /// </summary>
        Task<List<NotificationViewModel>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);

        /// <summary>
        /// Kullanıcının okunmamış bildirim sayısını getirir (Zil ikonu için).
        /// </summary>
        Task<int> GetUnreadNotificationCountAsync(string userId);

        /// <summary>
        /// Tek bir bildirimi okundu olarak işaretler.
        /// </summary>
        Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId);

        /// <summary>
        /// Kullanıcının tüm bildirimlerini okundu olarak işaretler.
        /// </summary>
        Task<bool> MarkAllNotificationsAsReadAsync(string userId);
    }
}