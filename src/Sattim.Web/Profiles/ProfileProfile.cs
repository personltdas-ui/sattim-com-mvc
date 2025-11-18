using AutoMapper;
using Sattim.Web.Models.User;
using Sattim.Web.ViewModels.Profile;

namespace Sattim.Web.Profiles
{
    public class ProfileProfile : Profile
    {
        public ProfileProfile()
        {
            CreateMap<AddressViewModel, UserAddress>();

            CreateMap<UserAddress, AddressViewModel>();
        }
    }
}