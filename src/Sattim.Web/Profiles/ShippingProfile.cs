using AutoMapper;
using Sattim.Web.Models.Shipping;
using Sattim.Web.ViewModels.Shipping;

namespace Sattim.Web.Profiles
{
    public class ShippingProfile : Profile
    {
        public ShippingProfile()
        {
            CreateMap<ShippingInfo, ShippingDetailViewModel>();
        }
    }
}