using AutoMapper;
using Sattim.Web.Models.UI;
using Sattim.Web.ViewModels.Notification;

namespace Sattim.Web.Profiles
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            // Notification (Entity) -> NotificationViewModel (DTO)
            // Not: 'LinkUrl' alanı, Entity'de bulunmadığı için
            // Servis katmanında (NotificationService) manuel olarak atanacaktır.
            CreateMap<Notification, NotificationViewModel>();
        }
    }
}