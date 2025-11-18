using Sattim.Web.Models.UI;
using System;

namespace Sattim.Web.ViewModels.Notification
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public NotificationType Type { get; set; }

        public string LinkUrl { get; set; }
    }
}