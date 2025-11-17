using AutoMapper;
using Microsoft.EntityFrameworkCore; // Transaction için
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // ApplicationDbContext
using Sattim.Web.Models.User;
using Sattim.Web.Services.Repositories; // Gerekli tüm Repolar
using Sattim.Web.ViewModels.Profile; // Arayüzün istediği DTO'lar
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Arayüzünüzle aynı namespace'i kullanır
namespace Sattim.Web.Services.Profile
{
    public class ProfileService : IProfileService
    {
        // Gerekli Jenerik Repolar
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<UserAddress> _addressRepo;
        private readonly IGenericRepository<ApplicationUser> _userRepo;

        // Yardımcılar
        private readonly ApplicationDbContext _context; // Transaction yönetimi için
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

        // ====================================================================
        //  1. Profil Detayları (UserProfile Modeli)
        // ====================================================================

        public async Task<UserProfile> GetUserProfileAsync(string userId)
        {
            var profile = await _profileRepo.GetByIdAsync(userId);
            if (profile == null)
            {
                _logger.LogWarning($"GetUserProfileAsync: Profil bulunamadı. (Kullanıcı: {userId}). Yeni profil oluşturuluyor.");
                // Savunma amaçlı (Defensive) kodlama: Eğer SeedData
                // çalışmadıysa, o an oluştur.
                var newProfile = new UserProfile(userId);
                await _profileRepo.AddAsync(newProfile);
                await _profileRepo.UnitOfWork.SaveChangesAsync();
                return newProfile;
            }
            return profile;
        }

        /// <summary>
        /// Transactional: Hem 'ApplicationUser' (FullName) hem de
        /// 'UserProfile' (Bio, Adres) varlıklarını günceller.
        /// </summary>
        public async Task<bool> UpdateProfileDetailsAsync(string userId, ProfileDetailsViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Varlıkları Al (Takip et)
                var user = await _userRepo.GetByIdAsync(userId);
                var profile = await _profileRepo.GetByIdAsync(userId);

                if (user == null || profile == null)
                    throw new KeyNotFoundException("Kullanıcı veya profil kaydı bulunamadı.");

                // 2. İş Mantığını Modele Devret
                user.UpdateProfile(model.FullName);
                profile.UpdateDetails(model.Address, model.City, model.Country, model.PostalCode, model.Bio);

                // 3. Değişiklikleri Bildir
                _userRepo.Update(user);
                _profileRepo.Update(profile);

                // 4. Kaydet ve Onayla
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

                // İş Mantığını Modele Devret
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

        // ====================================================================
        //  2. Adres Defteri (UserAddress Modeli)
        // ====================================================================

        public async Task<IEnumerable<UserAddress>> GetUserAddressesAsync(string userId)
        {
            // Jenerik repo, AsNoTracking() ile okur (performanslı)
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
                // 1. İş Mantığını Modele Devret (Constructor doğrular)
                var address = new UserAddress(
                    userId,
                    model.Title,
                    model.FullName,
                    model.Address,
                    model.City,
                    model.PostalCode,
                    model.Phone
                );

                // 2. İlk eklenen adres, varsayılan (Default) adres olmalı mı?
                if (!await _addressRepo.AnyAsync(a => a.UserId == userId && a.IsDefault))
                {
                    address.SetAsDefault();
                }

                // 3. Ekle ve Kaydet
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

        /// <summary>
        /// DÜZELTİLDİ: Artık 'userId' parametresi alıyor
        /// </summary>
        public async Task<bool> UpdateAddressAsync(int addressId, AddressViewModel model, string userId)
        {
            try
            {
                // 1. Varlığı Al (Takip et - 'AsNoTracking' KULLANMA)
                var address = await _addressRepo.GetByIdAsync(addressId);

                // 2. GÜVENLİK KONTROLÜ
                // Adres yoksa VEYA adres bu kullanıcıya ait değilse
                if (address == null || address.UserId != userId)
                {
                    _logger.LogWarning($"Adres güncelleme yetkisi yok. AdresID: {addressId}, Kullanıcı: {userId}");
                    return false; // Hata (Yetkisiz)
                }

                // 3. İş Mantığını Modele Devret
                // (Modelin 'Update' metodu tüm doğrulamaları yapar)
                address.Update(
                    model.Title,
                    model.FullName,
                    model.Address,
                    model.City,
                    model.PostalCode,
                    model.Phone
                );

                // 4. Değişikliği Bildir ve Kaydet
                _addressRepo.Update(address);
                await _addressRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Adres güncellendi (AdresID: {addressId}, Kullanıcı: {userId})");
                return true; // Başarılı
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Adres güncellenirken hata (AdresID: {addressId})");
                return false; // Hata (Genel)
            }
        }

        public async Task<bool> DeleteAddressAsync(int addressId, string userId)
        {
            try
            {
                var address = await _addressRepo.GetByIdAsync(addressId);

                // Güvenlik Kontrolü
                if (address == null || address.UserId != userId)
                {
                    _logger.LogWarning($"Adres silme yetkisi yok. AdresID: {addressId}, Kullanıcı: {userId}");
                    return false;
                }

                // İş Kuralı: Varsayılan (Default) adres silinemez
                // (Önce başkasını varsayılan yapmalı)
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

        /// <summary>
        /// Transactional: Diğer tüm adresleri 'IsDefault = false' yapar,
        /// seçileni 'IsDefault = true' yapar.
        /// </summary>
        public async Task<bool> SetDefaultAddressAsync(string userId, int addressId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Transaction içinde olduğumuz için DbContext'i kullanıyoruz
                // ve varlıkları 'Takip Et' (Track) ediyoruz.
                var allAddresses = await _context.UserAddresses
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                var newDefault = allAddresses.FirstOrDefault(a => a.Id == addressId);

                // Güvenlik: Adres yoksa veya bu kullanıcıya ait değilse
                if (newDefault == null)
                {
                    _logger.LogWarning($"Varsayılan adres ayarlanırken hata: Adres (ID: {addressId}) kullanıcıya (ID: {userId}) ait değil.");
                    await transaction.RollbackAsync();
                    return false;
                }

                // 2. İş Mantığını Modele Devret
                foreach (var addr in allAddresses)
                {
                    addr.RemoveAsDefault();
                }
                newDefault.SetAsDefault();

                // 3. Değişiklikleri Kaydet (Tüm adresler güncellenir) ve Onayla
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

        // ====================================================================
        //  3. Profil Doğrulama (UserProfile Modeli)
        // ====================================================================

        public async Task<bool> SubmitIdCardAsync(string userId, string idCardImageUrl)
        {
            // (Not: Gerçek dünyada 'idCardImageUrl' bir 'IFormFile' olmalı
            // ve burada 'IFileStorageService' çağrılmalıdır, ancak
            // arayüz 'string' istediği için, URL'in zaten
            // yüklendiğini varsayıyoruz.)

            try
            {
                var profile = await GetUserProfileAsync(userId); // (Bulamazsa oluşturur)

                // İş Mantığını Modele Devret
                // (Model metodu, 'IsVerified'i 'false' yapar ve Admin'in
                // onayını bekleme durumuna alır)
                profile.SetIdCardUrl(idCardImageUrl);

                _profileRepo.Update(profile);
                await _profileRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Kimlik kartı yüklendi (Kullanıcı: {userId}). Onay bekleniyor.");

                // (Burada 'IModerationService' veya 'INotificationService'
                // çağrılıp Admin'e bildirim gönderilmelidir)

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