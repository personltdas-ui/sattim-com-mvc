using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.Models.User
{
    /// <summary>
    /// ASP.NET Core Identity temel kullanıcısını genişleten uygulama kullanıcısı.
    /// Bu sınıf, mimarideki "God Object" anti-pattern'ını çözmek için
    /// sadeleştirilmiştir. Sadece kimlik doğrulama ve temel kullanıcı profili
    /// verilerini yönetir.
    ///
    /// Diğer tüm varlıklar (Product, Wallet, Bid, Message vb.) bu sınıfa
    /// referans vermez; bunun yerine bu sınıfın 'Id'sini (UserId)
    /// Yabancı Anahtar (Foreign Key) olarak tutarlar.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        #region Temel Özellikler (Properties)

        [Required(ErrorMessage = "Tam ad alanı zorunludur.")]
        [StringLength(100)]
        public string FullName { get; private set; }

        public DateTime RegisteredDate { get; private set; }

        [StringLength(1000)] // URL'ler için makul bir sınır
        public string? ProfileImageUrl { get; private set; }

        /// <summary>
        /// Kullanıcının platformda aktif olup olmadığını belirten "soft-delete" bayrağı.
        /// Bu, Identity'nin 'LockoutEnd' özelliğinden ayrı bir iş mantığı bayrağıdır.
        /// </summary>
        public bool IsActive { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Sadece Entity Framework Core tarafından kullanılmak üzere korunan parametresiz yapıcı metot.
        /// </summary>
        protected ApplicationUser()
        {
            // EF Core'un proxy oluşturması için gereklidir.
        }

        /// <summary>
        /// Yeni bir ApplicationUser nesnesi oluşturmak için kullanılan birincil yapıcı metot.
        /// Gerekli alanların (invariant'ların) doldurulmasını zorunlu kılar.
        /// </summary>
        /// <param name="userName">Kullanıcı adı (Identity için zorunlu)</param>
        /// <param name="email">E-posta (Identity için zorunlu)</param>
        /// <param name="fullName">Kullanıcının tam adı</param>
        public ApplicationUser(string userName, string email, string fullName) : base(userName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentNullException(nameof(fullName), "Tam ad boş olamaz.");
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentNullException(nameof(userName), "Kullanıcı adı boş olamaz.");
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email), "E-posta boş olamaz.");

            // IdentityUser özelliklerini ayarla
            Email = email;
            NormalizedEmail = email.ToUpperInvariant();
            NormalizedUserName = userName.ToUpperInvariant();
            EmailConfirmed = false; // Varsayılan olarak e-posta onaysızdır.

            // Bu sınıfa ait özellikleri ayarla
            FullName = fullName;
            RegisteredDate = DateTime.UtcNow;
            IsActive = true; // Yeni kullanıcı varsayılan olarak aktiftir.
            LockoutEnd = null; // Kilitli değil.
        }

        #endregion

        #region Davranışsal Metotlar (Domain Methods)

        /// <summary>
        /// Kullanıcının profil bilgilerini günceller.
        /// </summary>
        public void UpdateProfile(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Tam ad boş olamaz.", nameof(fullName));

            FullName = fullName;
        }

        /// <summary>
        /// Kullanıcının profil resmini günceller.
        /// </summary>
        public void UpdateProfileImage(string? imageUrl)
        {
            ProfileImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl;
        }

        /// <summary>
        /// Kullanıcıyı devre dışı bırakır. 
        /// Bu metot, hem Identity'nin oturum açmasını engeller (Lockout)
        /// hem de iş mantığı bayrağını (IsActive) ayarlar.
        /// </summary>
        public void Deactivate()
        {
            if (!IsActive) return; // Zaten pasifse işlem yapma

            IsActive = false;
            // Identity'nin bu kullanıcı için oturum açmasını engelle
            LockoutEnd = DateTimeOffset.MaxValue;
        }

        /// <summary>
        /// Devre dışı bırakılmış bir kullanıcıyı yeniden etkinleştirir.
        /// </summary>
        public void Activate()
        {
            if (IsActive) return; // Zaten aktifse işlem yapma

            IsActive = true;
            LockoutEnd = null; // Identity kilidini kaldır
        }

        #endregion

        #region Navigasyon Özellikleri (Temizlendi)

        // ===================================================================================
        // MİMARİ BAŞARI: GOD OBJECT ÇÖZÜLDÜ
        // ===================================================================================
        // Bu sınıftan kaldırılan tüm 'ICollection<T>' ve 'virtual T' navigasyon
        // özellikleri, artık ilgili oldukları diğer varlıklar (Product, Wallet,
        // UserProfile, Bid, Message vb.) tarafından 'UserId' (veya SellerId,
        // BuyerId) olarak tutulmaktadır.
        //
        // Örnek:
        // public virtual ICollection<Product> ProductsForSale { get; private set; } -> KALDIRILDI
        // public virtual Wallet.Wallet Wallet { get; private set; } -> KALDIRILDI
        // public virtual UserProfile Profile { get; private set; } -> KALDIRILDI
        // public virtual ICollection<Message> MessagesSent { get; private set; } -> KALDIRILDI
        // (ve diğer 20+ özellik...)
        // ===================================================================================

        #endregion
    }
}