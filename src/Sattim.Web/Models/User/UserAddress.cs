using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.User
{
    /// <summary>
    /// Kullanıcının kargo veya fatura için kaydedilmiş adreslerini temsil eder.
    /// Bir Kullanıcı (ApplicationUser) birden fazla adrese sahip olabilir (1'e Çok).
    /// </summary>
    public class UserAddress
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(100)]
        public string Title { get; private set; } // Örn: "Ev Adresim", "Ofis"

        [Required]
        [StringLength(150)]
        public string FullName { get; private set; } // Alıcı Adı Soyadı

        [Required]
        [StringLength(500)]
        public string Address { get; private set; }

        [Required]
        [StringLength(100)]
        public string City { get; private set; }

        [Required]
        [StringLength(20)]
        public string PostalCode { get; private set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; private set; }

        /// <summary>
        /// Bu adresin kullanıcının varsayılan adresi olup olmadığını belirtir.
        /// Bu durumu yönetme mantığı (diğerlerini false yapmak) Service katmanındadır.
        /// </summary>
        public bool IsDefault { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Bu adresin sahibi olan kullanıcının kimliği (Foreign Key).
        /// </summary>
        [Required]
        public string UserId { get; private set; }

        /// <summary>
        /// Navigasyon özelliği. 'UserId'ye karşılık gelen ApplicationUser'ı yükler.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli olan parametresiz özel yapıcı metot.
        /// </summary>
        private UserAddress() { }

        /// <summary>
        /// Yeni bir 'UserAddress' nesnesi oluşturur ve tüm zorunlu alanların
        /// geçerli olmasını sağlar.
        /// </summary>
        public UserAddress(string userId, string title, string fullName, string address, string city, string postalCode, string phone)
        {
            // Gerekli doğrulamalar (Invariant'lar)
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            // 'Update' metodu bu doğrulamaları içerdiği için DRY (Don't Repeat Yourself)
            // prensibine uymak adına 'Update' metodunu buradan çağırıyoruz.
            Update(title, fullName, address, city, postalCode, phone);

            UserId = userId;
            IsDefault = false; // Her yeni adres varsayılan olarak 'false' başlar.
        }

        /// <summary>
        /// Adres bilgilerini güvenli bir şekilde günceller ve doğrular.
        /// </summary>
        public void Update(string title, string fullName, string address, string city, string postalCode, string phone)
        {
            // DÜZELTME: Kapsüllemeyi korumak için doğrulamalar eklendi.
            // Nesnenin geçersiz bir duruma (örn. boş başlık) güncellenmesini engeller.
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Başlık boş olamaz.", nameof(title));
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Tam ad boş olamaz.", nameof(fullName));
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Adres boş olamaz.", nameof(address));
            if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("Şehir boş olamaz.", nameof(city));
            if (string.IsNullOrWhiteSpace(postalCode)) throw new ArgumentException("Posta kodu boş olamaz.", nameof(postalCode));
            if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Telefon boş olamaz.", nameof(phone));

            Title = title;
            FullName = fullName;
            Address = address;
            City = city;
            PostalCode = postalCode;
            Phone = phone;
        }

        /// <summary>
        /// Bu adresi varsayılan olarak ayarlar (Domain Metodu).
        /// </summary>
        public void SetAsDefault()
        {
            IsDefault = true;
        }

        /// <summary>
        /// Bu adresin varsayılan olma durumunu kaldırır (Domain Metodu).
        /// </summary>
        public void RemoveAsDefault()
        {
            IsDefault = false;
        }

        #endregion
    }
}