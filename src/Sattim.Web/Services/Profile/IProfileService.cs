using Sattim.Web.Models.User;
using Sattim.Web.ViewModels.Profile;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Profile
{
    public interface IProfileService
    {
        Task<UserProfile> GetUserProfileAsync(string userId);
        Task<bool> UpdateProfileDetailsAsync(string userId, ProfileDetailsViewModel model);
        Task<bool> UpdateProfileImageAsync(string userId, string newImageUrl);

        Task<IEnumerable<UserAddress>> GetUserAddressesAsync(string userId);
        Task<UserAddress> GetUserAddressAsync(int addressId);
        Task<bool> AddNewAddressAsync(string userId, AddressViewModel model);

        Task<bool> UpdateAddressAsync(int addressId, AddressViewModel model, string userId);
        Task<bool> DeleteAddressAsync(int addressId, string userId);
        Task<bool> SetDefaultAddressAsync(string userId, int addressId);

        Task<bool> SubmitIdCardAsync(string userId, string idCardImageUrl);
    }
}