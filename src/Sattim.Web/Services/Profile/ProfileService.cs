using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.ViewModels.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Profile
{
    public class ProfileService : IProfileService
    {
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<UserAddress> _addressRepo;
        private readonly IGenericRepository<ApplicationUser> _userRepo;

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            IGenericRepository<UserProfile> profileRepo,
            IGenericRepository<UserAddress> addressRepo,
            IGenericRepository<ApplicationUser> userRepo,
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<ProfileService> logger)
        {
            _profileRepo = profileRepo;
            _addressRepo = addressRepo;
            _userRepo = userRepo;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserProfile> GetUserProfileAsync(string userId)
        {
            var profile = await _profileRepo.GetByIdAsync(userId);
            if (profile == null)
            {
                _logger.LogWarning($"GetUserProfileAsync: Profil bulunamadı. (Kullanıcı: {userId}). Yeni profil oluşturuluyor.");
                var newProfile = new UserProfile(userId);
                await _profileRepo.AddAsync(newProfile);
                await _profileRepo.UnitOfWork.SaveChangesAsync();
                return newProfile;
            }
            return profile;
        }

        public async Task<bool> UpdateProfileDetailsAsync(string userId, ProfileDetailsViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                var profile = await _profileRepo.GetByIdAsync(userId);

                if (user == null || profile == null)
                    throw new KeyNotFoundException("Kullanıcı veya profil kaydı bulunamadı.");

                user.UpdateProfile(model.FullName);
                profile.UpdateDetails(model.Address, model.City, model.Country, model.PostalCode, model.Bio);

                _userRepo.Update(user);
                _profileRepo.Update(profile);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Profil detayları güncellendi (Kullanıcı: {userId})");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Profil detayları güncellenirken hata (Kullanıcı: {userId}, Rollback).");
                return false;
            }
        }

        public async Task<bool> UpdateProfileImageAsync(string userId, string newImageUrl)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null) return false;

                user.UpdateProfileImage(newImageUrl);

                _userRepo.Update(user);
                await _userRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Profil resmi güncellendi (Kullanıcı: {userId})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Profil resmi güncellenirken hata (Kullanıcı: {userId})");
                return false;
            }
        }

        // --------------------------------------------------------------------
        //  2. Adres Defteri (UserAddress Modeli)
        // --------------------------------------------------------------------

        public async Task<IEnumerable<UserAddress>> GetUserAddressesAsync(string userId)
        {
            return await _addressRepo.FindAsync(a => a.UserId == userId);
        }

        public async Task<UserAddress> GetUserAddressAsync(int addressId)
        {
            var address = await _addressRepo.GetByIdAsync(addressId);
            if (address == null)
                throw new KeyNotFoundException("Adres bulunamadı.");
            return address;
        }

        public async Task<bool> AddNewAddressAsync(string userId, AddressViewModel model)
        {
            try
            {
                var address = new UserAddress(
                    userId,
                    model.Title,
                    model.FullName,
                    model.Address,
                    model.City,
                    model.PostalCode,
                    model.Phone
                );

                if (!await _addressRepo.AnyAsync(a => a.UserId == userId && a.IsDefault))
                {
                    address.SetAsDefault();
                }

                await _addressRepo.AddAsync(address);
                await _addressRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Yeni adres eklendi (Kullanıcı: {userId}, AdresID: {address.Id})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Yeni adres eklenirken hata (Kullanıcı: {userId})");
                return false;
            }
        }

        public async Task<bool> UpdateAddressAsync(int addressId, AddressViewModel model, string userId)
        {
            try
            {
                var address = await _addressRepo.GetByIdAsync(addressId);

                if (address == null || address.UserId != userId)
                {
                    _logger.LogWarning($"Adres güncelleme yetkisi yok. AdresID: {addressId}, Kullanıcı: {userId}");
                    return false;
                }

                address.Update(
                    model.Title,
                    model.FullName,
                    model.Address,
                    model.City,
                    model.PostalCode,
                    model.Phone
                );

                _addressRepo.Update(address);
                await _addressRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Adres güncellendi (AdresID: {addressId}, Kullanıcı: {userId})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Adres güncellenirken hata (AdresID: {addressId})");
                return false;
            }
        }

        public async Task<bool> DeleteAddressAsync(int addressId, string userId)
        {
            try
            {
                var address = await _addressRepo.GetByIdAsync(addressId);

                if (address == null || address.UserId != userId)
                {
                    _logger.LogWarning($"Adres silme yetkisi yok. AdresID: {addressId}, Kullanıcı: {userId}");
                    return false;
                }

                if (address.IsDefault)
                {
                    _logger.LogWarning($"Varsayılan adres silinemez (AdresID: {addressId})");
                    return false;
                }

                _addressRepo.Remove(address);
                await _addressRepo.UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Adres silinirken hata (AdresID: {addressId})");
                return false;
            }
        }

        public async Task<bool> SetDefaultAddressAsync(string userId, int addressId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var allAddresses = await _context.UserAddresses
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                var newDefault = allAddresses.FirstOrDefault(a => a.Id == addressId);

                if (newDefault == null)
                {
                    _logger.LogWarning($"Varsayılan adres ayarlanırken hata: Adres (ID: {addressId}) kullanıcıya (ID: {userId}) ait değil.");
                    await transaction.RollbackAsync();
                    return false;
                }

                foreach (var addr in allAddresses)
                {
                    addr.RemoveAsDefault();
                }
                newDefault.SetAsDefault();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Varsayılan adres güncellendi (Kullanıcı: {userId}, AdresID: {addressId})");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Varsayılan adres ayarlanırken hata (Kullanıcı: {userId}, Rollback).");
                return false;
            }
        }

        // --------------------------------------------------------------------
        //  3. Profil Doğrulama (UserProfile Modeli)
        // --------------------------------------------------------------------

        public async Task<bool> SubmitIdCardAsync(string userId, string idCardImageUrl)
        {
            try
            {
                var profile = await GetUserProfileAsync(userId);

                profile.SetIdCardUrl(idCardImageUrl);

                _profileRepo.Update(profile);
                await _profileRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Kimlik kartı yüklendi (Kullanıcı: {userId}). Onay bekleniyor.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kimlik kartı yüklenirken hata (Kullanıcı: {userId})");
                return false;
            }
        }
    }
}