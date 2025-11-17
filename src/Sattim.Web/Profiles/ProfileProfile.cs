using AutoMapper;
using Sattim.Web.Models.User;
using Sattim.Web.ViewModels.Profile;

namespace Sattim.Web.Profiles
{
    public class ProfileProfile : Profile
    {
        public ProfileProfile()
        {
            // DTO -> Entity (Create)
            // (Not: 'AddNewAddressAsync' metodunda, UserId'yi
            // constructor'a manuel olarak verdiğimiz için bu eşlemeyi KULLANMIYORUZ,
            // ancak güncelleme (Update) için kullanabiliriz.)
            CreateMap<AddressViewModel, UserAddress>();

            // Entity -> DTO (Form doldurma için)
            CreateMap<UserAddress, AddressViewModel>();
        }
    }
}