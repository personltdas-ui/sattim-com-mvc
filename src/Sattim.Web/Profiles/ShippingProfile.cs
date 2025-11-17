using AutoMapper;
using Sattim.Web.Models.Shipping;
using Sattim.Web.ViewModels.Shipping;

namespace Sattim.Web.Profiles
{
    public class ShippingProfile : Profile
    {
        public ShippingProfile()
        {
            // ShippingInfo (Entity) -> ShippingDetailViewModel (DTO)
            // (Tüm özellik adları birebir eşleştiği için
            // manuel 'ForMember' tanımına gerek yoktur)
            CreateMap<ShippingInfo, ShippingDetailViewModel>();
        }
    }
}