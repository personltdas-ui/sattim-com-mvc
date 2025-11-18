using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Analytical;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.Shipping;
using Sattim.Web.Models.UI;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet;
using Sattim.Web.Services.Email;
using Sattim.Web.ViewModels.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
          ApplicationDbContext context,
          IEmailService emailService,
          UserManager<ApplicationUser> userManager,
          ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
        }

        private async Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, string? entityId = null, string? entityType = null)
        {
            try
            {
                var notification = new Models.UI.Notification(
                  userId, title, message, type, entityId, entityType
                );
                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CreateNotificationAsync (UserId: {userId}, Title: {title}) başarısız oldu.");
            }
        }

        private async Task SendEmailFromTemplateAsync(ApplicationUser user, EmailTemplateType templateType, Dictionary<string, string> placeholders)
        {
            try
            {
                var template = await _context.EmailTemplates
                  .AsNoTracking()
                  .FirstOrDefaultAsync(t => t.Type == templateType && t.IsActive);

                if (template == null)
                {
                    _logger.LogError($"E-posta şablonu bulunamadı: {templateType}");
                    return;
                }

                string subject = template.Subject;
                string body = template.Body;

                placeholders.TryAdd("{{UserName}}", user.FullName);
                placeholders.TryAdd("{{SiteUrl}}", "https://www.sattim.com");

                foreach (var (key, value) in placeholders)
                {
                    subject = subject.Replace(key, value);
                    body = body.Replace(key, value);
                }

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SendEmailFromTemplateAsync (UserId: {user.Id}, Template: {templateType}) başarısız oldu.");
            }
        }

        private async Task NotifyAdminsAsync(string title, string message, NotificationType type, string? entityId = null, string? entityType = null)
        {
            try
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                var modUsers = await _userManager.GetUsersInRoleAsync("Moderator");
                var usersToNotify = adminUsers.Concat(modUsers).Distinct();

                foreach (var user in usersToNotify)
                {
                    await CreateNotificationAsync(user.Id, title, message, type, entityId, entityType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotifyAdminsAsync başarısız oldu.");
            }
        }

        public async Task SendWelcomeNotificationAsync(ApplicationUser user)
        {
            await CreateNotificationAsync(user.Id, "Hoş Geldiniz!", "Sattim'a başarıyla kaydoldunuz.", NotificationType.System);
            var placeholders = new Dictionary<string, string>();
            await SendEmailFromTemplateAsync(user, EmailTemplateType.Welcome, placeholders);
        }

        public async Task SendEmailConfirmationAsync(ApplicationUser user, string confirmationLink)
        {
            var placeholders = new Dictionary<string, string> { { "{{ConfirmationLink}}", confirmationLink } };
            await SendEmailFromTemplateAsync(user, EmailTemplateType.EmailVerification, placeholders);
        }

        public async Task SendPasswordResetAsync(ApplicationUser user, string resetLink)
        {
            var placeholders = new Dictionary<string, string> { { "{{ResetLink}}", resetLink } };
            await SendEmailFromTemplateAsync(user, EmailTemplateType.PasswordReset, placeholders);
        }

        public async Task SendProductApprovedNotificationAsync(ApplicationUser seller, Models.Product.Product product)
        {
            await CreateNotificationAsync(
              seller.Id, "Ürününüz Onaylandı", $"'{product.Title}' adlı ürününüz onaylandı ve açık artırmaya açıldı.",
              NotificationType.ProductApproved, product.Id.ToString(), "Product"
            );
        }

        public async Task SendProductRejectedNotificationAsync(ApplicationUser seller, Models.Product.Product product, string reason)
        {
            await CreateNotificationAsync(
             seller.Id, "Ürününüz Reddedildi", $"'{product.Title}' adlı ürününüz reddedildi. Sebep: {reason}",
             NotificationType.System, product.Id.ToString(), "Product"
           );
        }

        public async Task SendBidPlacedNotificationAsync(Models.Product.Product product, ApplicationUser bidder, decimal amount)
        {
            await CreateNotificationAsync(
              product.SellerId, "Yeni Teklif Aldınız!", $"'{product.Title}' adlı ürününüze {amount:C} tutarında yeni bir teklif verildi.",
              NotificationType.BidPlaced, product.Id.ToString(), "Product"
            );
        }

        public async Task SendBidOutbidNotificationAsync(ApplicationUser outbidUser, Models.Product.Product product)
        {
            string msg = $"'{product.Title}' adlı üründeki en yüksek teklifiniz geçildi. Yeni fiyat: {product.CurrentPrice:C}";
            await CreateNotificationAsync(outbidUser.Id, "Teklifiniz Geçildi!", msg, NotificationType.BidOutbid, product.Id.ToString(), "Product");

            var placeholders = new Dictionary<string, string>
      { { "{{ProductName}}", product.Title }, { "{{NewPrice}}", product.CurrentPrice.ToString("C") }, { "{{ProductLink}}", $"/Product/Detail/{product.Id}" } };
            await SendEmailFromTemplateAsync(outbidUser, EmailTemplateType.BidOutbid, placeholders);
        }

        public async Task SendAuctionEndingSoonNotificationAsync(List<ApplicationUser> usersToNotify, Models.Product.Product product)
        {
            foreach (var user in usersToNotify)
            {
                string msg = $"İzlediğiniz '{product.Title}' adlı ürünün ihalesi 24 saat içinde bitiyor.";
                await CreateNotificationAsync(user.Id, "İhale Bitiyor!", msg, NotificationType.AuctionEnding, product.Id.ToString(), "Product");

                var placeholders = new Dictionary<string, string>
        { { "{{ProductName}}", product.Title }, { "{{ProductLink}}", $"/Product/Detail/{product.Id}" } };
                await SendEmailFromTemplateAsync(user, EmailTemplateType.AuctionEnding, placeholders);
            }
        }

        public async Task SendAuctionWonNotificationAsync(ApplicationUser winner, Models.Product.Product product)
        {
            string msg = $"'{product.Title}' adlı ürünü {product.CurrentPrice:C} bedelle kazandınız. Lütfen ödemeyi tamamlayın.";
            await CreateNotificationAsync(winner.Id, "Tebrikler, İhaleyi Kazandınız!", msg, NotificationType.AuctionWon, product.Id.ToString(), "Product");

            var placeholders = new Dictionary<string, string>
      { { "{{ProductName}}", product.Title }, { "{{FinalPrice}}", product.CurrentPrice.ToString("C") }, { "{{PaymentLink}}", $"/Order/Pay/{product.Id}" } };
            await SendEmailFromTemplateAsync(winner, EmailTemplateType.AuctionWon, placeholders);
        }

        public async Task SendAuctionLostNotificationAsync(List<string> loserIds, Models.Product.Product product)
        {
            foreach (var userId in loserIds)
            {
                await CreateNotificationAsync(userId, "İhaleyi Kaybettiniz", $"'{product.Title}' adlı ürünün ihalesini kaybettiniz.", NotificationType.AuctionLost, product.Id.ToString(), "Product");
            }
        }

        public async Task SendProductSoldNotificationAsync(ApplicationUser seller, Models.Product.Product product)
        {
            string msg = $"'{product.Title}' adlı ürününüz {product.CurrentPrice:C} bedelle satıldı. Lütfen kargolama için hazırlanın.";
            await CreateNotificationAsync(seller.Id, "Ürününüz Satıldı!", msg, NotificationType.ProductSold, product.Id.ToString(), "Product");
        }

        public async Task SendAuctionNotSoldNotificationAsync(ApplicationUser seller, Models.Product.Product product)
        {
            string msg = $"'{product.Title}' adlı ürününüzün ihalesi (rezerv fiyata ulaşılamadığı veya teklif gelmediği için) satılmadan kapandı.";
            await CreateNotificationAsync(seller.Id, "Ürününüz Satılmadı", msg, NotificationType.AuctionLost, product.Id.ToString(), "Product");
        }

        public async Task SendPaymentSuccessNotificationAsync(ApplicationUser buyer, Models.Product.Product product)
        {
            string msg = $"'{product.Title}' adlı ürün için yaptığınız {product.CurrentPrice:C} tutarındaki ödeme başarıyla alındı.";
            await CreateNotificationAsync(buyer.Id, "Ödeme Başarılı", msg, NotificationType.PaymentReceived, product.Id.ToString(), "Product");

            var placeholders = new Dictionary<string, string>
      { { "{{ProductName}}", product.Title }, { "{{Amount}}", product.CurrentPrice.ToString("C") } };
            await SendEmailFromTemplateAsync(buyer, EmailTemplateType.PaymentConfirmation, placeholders);
        }

        public async Task SendSellerPaymentReceivedNotificationAsync(ApplicationUser seller, Models.Product.Product product)
        {
            string msg = $"'{product.Title}' adlı ürününüz için alıcı ödemeyi tamamladı. Lütfen ürünü kargolayın.";
            await CreateNotificationAsync(seller.Id, "Ödeme Alındı", msg, NotificationType.PaymentReceived, product.Id.ToString(), "Product");
        }

        public async Task SendProductShippedNotificationAsync(ApplicationUser buyer, Models.Product.Product product, ShippingInfo shippingInfo)
        {
            string msg = $"'{product.Title}' adlı ürününüz {shippingInfo.Carrier} firması ile kargolandı. Takip No: {shippingInfo.TrackingNumber}";
            await CreateNotificationAsync(buyer.Id, "Ürününüz Kargolandı!", msg, NotificationType.System, product.Id.ToString(), "Product");

            var placeholders = new Dictionary<string, string>
      { { "{{ProductName}}", product.Title }, { "{{Carrier}}", shippingInfo.Carrier }, { "{{TrackingNumber}}", shippingInfo.TrackingNumber } };
            await SendEmailFromTemplateAsync(buyer, EmailTemplateType.ShippingNotification, placeholders);
        }

        public async Task SendProductDeliveredNotificationAsync(ApplicationUser seller, Models.Product.Product product)
        {
            string msg = $"'{product.Title}' adlı ürününüz alıcıya teslim edildi. Satış tutarı cüzdanınıza aktarılacaktır.";
            await CreateNotificationAsync(seller.Id, "Ürün Teslim Edildi", msg, NotificationType.System, product.Id.ToString(), "Product");
        }

        public async Task SendFundsReleasedNotificationAsync(ApplicationUser seller, decimal netAmount)
        {
            string msg = $"Bir satıştan elde ettiğiniz {netAmount:C} tutarındaki gelir cüzdanınıza aktarıldı.";
            await CreateNotificationAsync(seller.Id, "Cüzdanınıza Para Aktarıldı", msg, NotificationType.PaymentReceived, null, "Wallet");
        }

        public async Task SendPayoutRequestedNotificationAsync(ApplicationUser user, PayoutRequest request)
        {
            string msg = $"{request.Amount:C} tutarındaki para çekme talebiniz alındı ve incelemeye gönderildi.";
            await CreateNotificationAsync(user.Id, "Para Çekme Talebi Alındı", msg, NotificationType.System, request.Id.ToString(), "PayoutRequest");
            await NotifyAdminsAsync("Yeni Para Çekme Talebi", $"{user.FullName}, {request.Amount:C} tutarında bir talep oluşturdu.", NotificationType.System, request.Id.ToString(), "PayoutRequest");
        }

        public async Task SendPayoutApprovedNotificationAsync(ApplicationUser user, PayoutRequest request)
        {
            string msg = $"{request.Amount:C} tutarındaki para çekme talebiniz onaylandı. Ödemeniz en kısa sürede banka hesabınıza aktarılacaktır.";
            await CreateNotificationAsync(user.Id, "Para Çekme Talebi Onaylandı", msg, NotificationType.System, request.Id.ToString(), "PayoutRequest");
        }

        public async Task SendPayoutRejectedNotificationAsync(ApplicationUser user, PayoutRequest request, string reason)
        {
            string msg = $"{request.Amount:C} tutarındaki para çekme talebiniz reddedildi. Tutar cüzdanınıza iade edildi. Sebep: {reason}";
            await CreateNotificationAsync(user.Id, "Para Çekme Talebi Reddedildi", msg, NotificationType.System, request.Id.ToString(), "PayoutRequest");
        }

        public async Task SendPayoutCompletedNotificationAsync(ApplicationUser user, PayoutRequest request)
        {
            string msg = $"{request.Amount:C} tutarındaki para çekme talebiniz başarıyla tamamlandı ve banka hesabınıza gönderildi.";
            await CreateNotificationAsync(user.Id, "Para Çekme İşlemi Tamamlandı", msg, NotificationType.PaymentReceived, request.Id.ToString(), "PayoutRequest");
        }

        public async Task SendNewMessageNotificationAsync(ApplicationUser receiver, ApplicationUser sender, int messageId)
        {
            string msg = $"{sender.FullName} size yeni bir mesaj gönderdi.";
            await CreateNotificationAsync(receiver.Id, "Yeni Mesajınız Var", msg, NotificationType.MessageReceived, messageId.ToString(), "Message");
        }

        public async Task SendDisputeOpenedNotificationAsync(ApplicationUser buyer, ApplicationUser seller, Models.Dispute.Dispute dispute)
        {
            string sellerMsg = $"Alıcı ({buyer.FullName}), '{dispute.Product.Title}' ürünü için bir ihtilaf başlattı.";
            await CreateNotificationAsync(seller.Id, "Hakkınızda İhtilaf Açıldı", sellerMsg, NotificationType.System, dispute.Id.ToString(), "Dispute");

            await NotifyAdminsAsync("Yeni İhtilaf Açıldı", $"Alıcı ({buyer.FullName}), '{dispute.Product.Title}' ürünü için bir ihtilaf başlattı.", NotificationType.System, dispute.Id.ToString(), "Dispute");
        }

        public async Task SendDisputeNewMessageNotificationAsync(ApplicationUser receiver, ApplicationUser sender, Models.Dispute.Dispute dispute)
        {
            string msg = $"İhtilaf (ID: {dispute.Id}) hakkında {sender.FullName} yeni bir mesaj gönderdi.";
            await CreateNotificationAsync(receiver.Id, "İhtilafa Yeni Mesaj", msg, NotificationType.System, dispute.Id.ToString(), "Dispute");
        }

        public async Task SendDisputeResolvedNotificationAsync(ApplicationUser user, int disputeId, string resolution)
        {
            string msg = $"Açtığınız ihtilaf (ID: {disputeId}) sonuçlandı. Sonuç: {resolution}";
            await CreateNotificationAsync(user.Id, "İhtilaf Sonuçlandı", msg, NotificationType.System, disputeId.ToString(), "Dispute");
        }

        public async Task SendCommentNeedsApprovalNotificationAsync(BlogComment comment)
        {
            var user = await _context.Users.FindAsync(comment.UserId);
            var post = await _context.BlogPosts.FindAsync(comment.BlogPostId);

            string msg = $"{user.FullName}, '{post.Title}' yazısına yeni bir yorum yaptı.";
            await NotifyAdminsAsync("Onay Bekleyen Yorum", msg, NotificationType.System, comment.Id.ToString(), "BlogComment");
        }

        public async Task SendNewReportToAdminsNotificationAsync(Models.Analytical.Report report)
        {
            var user = await _context.Users.FindAsync(report.ReporterId);
            string msg = $"{user.FullName} yeni bir şikayet (ID: {report.Id}) oluşturdu. Varlık: {report.EntityType}/{report.EntityId}";
            await NotifyAdminsAsync("Yeni Şikayet", msg, NotificationType.System, report.Id.ToString(), "Report");
        }

        public async Task<List<ViewModels.Notification.NotificationViewModel>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            var query = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
              .OrderByDescending(n => n.CreatedDate)
              .Take(50)
              .Select(n => new NotificationViewModel
              {
                  Id = n.Id,
                  Title = n.Title,
                  Message = n.Message,
                  Type = n.Type,
                  IsRead = n.IsRead,
                  CreatedDate = n.CreatedDate,
              })
              .ToListAsync();
        }

        private string GenerateClickUrl(NotificationType type, string? entityId, string? entityType)
        {
            if (string.IsNullOrEmpty(entityId))
                return "/Account/Notifications";

            if (entityType == "Product")
                return $"/Products/Detail/{entityId}";
            if (entityType == "Message")
                return $"/Messages/Detail/{entityId}";
            if (entityType == "Dispute")
                return $"/Dispute/Detail/{entityId}";
            if (entityType == "PayoutRequest")
                return $"/Wallet/Payouts";
            if (entityType == "BlogComment")
                return $"/Admin/Moderation/Comment/{entityId}";
            if (entityType == "Report")
                return $"/Admin/Moderation/Report/{entityId}";

            return "/Account/Notifications";
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _context.Notifications
              .AsNoTracking()
              .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null || notification.UserId != userId)
                return false;

            notification.MarkAsRead();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
        {
            var notificationsToMark = await _context.Notifications
              .Where(n => n.UserId == userId && !n.IsRead)
              .ToListAsync();

            if (!notificationsToMark.Any())
                return true;

            foreach (var notification in notificationsToMark)
            {
                notification.MarkAsRead();
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}