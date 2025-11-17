using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Key] ve [ForeignKey] için eklendi

namespace Sattim.Web.Models.User
{
    /// <summary>
    /// ApplicationUser için 1'e 1 ilişkiyle genişletilmiş profil bilgilerini tutar.
    /// (Adres, Biyografi, Doğrulama Durumu vb.)
    /// </summary>
    public class UserProfile
    {
        #region Özellikler (Properties)

        /// <summary>
        /// Bu tablonun Birincil Anahtarı (PK).
        /// Aynı zamanda ApplicationUser'a olan Yabancı Anahtardır (FK).
        /// Bu, birebir ilişkiyi garanti eder.
        /// </summary>
        [Key]
        [Required]
        public string UserId { get; private set; }

        [StringLength(500)]
        public string? Address { get; private set; }

        [StringLength(100)]
        public string? City { get; private set; }

        [StringLength(100)]
        public string? Country { get; private set; }

        [StringLength(20)]
        public string? PostalCode { get; private set; }

        [StringLength(2000)]
        public string? Bio { get; private set; }

        /// <summary>
        /// Kimlik doğrulama için yüklenen (hassas) belgenin URL'si.
        /// </summary>
        [StringLength(1000)]
        public string? IdCardImageUrl { get; private set; }

        public bool IsVerified { get; private set; }
        public DateTime? VerifiedDate { get; private set; }

        // Bu alanlar dış bir Servis (örn. RatingService) tarafından hesaplanır
        // ve 'UpdateRating' metoduyla güncellenir.
        public int RatingCount { get; private set; }

        [Range(0, 5)]
        public decimal AverageRating { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        // DÜZELTME: 'virtual' eklendi.
        // Bu, EF Core'un Tembel Yükleme (Lazy Loading) proxy'si oluşturabilmesi için ZORUNLUDUR.
        // Performans endişeleri (N+1), sorgu katmanında .Include() veya .Select() ile çözülmelidir.
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core'un nesneyi veritabanından okurken 
        /// kullanması için gereken korumalı (veya özel) yapıcı metot.
        /// </summary>
        private UserProfile() { }

        /// <summary>
        /// Yeni bir kullanıcı profili oluşturmak için kullanılan birincil yapıcı metot.
        /// Her zaman geçerli bir 'UserId' ile oluşturulmasını sağlar.
        /// </summary>
        public UserProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            UserId = userId;
            IsVerified = false;
            RatingCount = 0;
            AverageRating = 0m;
        }

        /// <summary>
        /// Kullanıcının adres ve biyografi gibi temel profil detaylarını günceller.
        /// </summary>
        public void UpdateDetails(string? address, string? city, string? country, string? postalCode, string? bio)
        {
            Address = address;
            City = city;
            Country = country;
            PostalCode = postalCode;
            Bio = bio;
        }

        /// <summary>
        /// Kimlik kartı URL'sini ayarlar ve yeniden doğrulama gerektireceği için
        /// mevcut doğrulamayı sıfırlar.
        /// </summary>
        public void SetIdCardUrl(string? url)
        {
            IdCardImageUrl = url;
            // Yeni kimlik yüklendiğinde, durum 'doğrulanmamış' olarak sıfırlanmalıdır.
            Unverify();
        }

        /// <summary>
        /// Profili doğrular (Genellikle bir Admin/Moderator tarafından çağrılır).
        /// </summary>
        public void Verify()
        {
            if (IsVerified) return; // Zaten doğrulanmışsa tekrar işlem yapma

            // Doğrulama için bir kimlik kartının yüklenmiş olması gerekir.
            if (string.IsNullOrWhiteSpace(IdCardImageUrl))
                throw new InvalidOperationException("Kimlik kartı yüklenmeden doğrulama yapılamaz.");

            IsVerified = true;
            VerifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Profil doğrulamasını kaldırır.
        /// </summary>
        public void Unverify()
        {
            IsVerified = false;
            VerifiedDate = null;
        }

        /// <summary>
        /// Dış bir servisten gelen hesaplanmış yeni puanı günceller.
        /// </summary>
        public void UpdateRating(int newCount, decimal newAverage)
        {
            if (newCount < 0)
                throw new ArgumentOutOfRangeException(nameof(newCount), "Rating sayısı 0'dan küçük olamaz.");
            if (newAverage < 0 || newAverage > 5)
                throw new ArgumentOutOfRangeException(nameof(newAverage), "Ortalama puan 0 ile 5 arasında olmalıdır.");

            RatingCount = newCount;
            AverageRating = newAverage;
        }

        #endregion
    }
}