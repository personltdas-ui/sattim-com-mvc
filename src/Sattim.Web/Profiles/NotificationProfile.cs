using AutoMapper;
using Sattim.Web.Models.UI;
using Sattim.Web.ViewModels.Notification;

namespace Sattim.Web.Profiles
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, NotificationViewModel>();
        }
    }
}