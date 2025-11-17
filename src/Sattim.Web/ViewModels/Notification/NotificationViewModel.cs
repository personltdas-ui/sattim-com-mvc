using Sattim.Web.Models.UI; // NotificationType enum'u için
using System;

namespace Sattim.Web.ViewModels.Notification
{
    /// <summary>
    /// Kullanıcının "Bildirimlerim" sayfasında gördüğü tek bir bildirim DTO'su.
    /// (GetUžserNotificationsAsync tarafından döndürülür)
    /// </summary>
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public NotificationType Type { get; set; }

        // Tıklandığında yönlendirilecek URL (Servis katmanında oluşturulur)
        public string LinkUrl { get; set; }
    }
}