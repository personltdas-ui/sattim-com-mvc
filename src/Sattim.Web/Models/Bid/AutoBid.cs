using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Bid
{
    /// <summary>
    /// Bir kullanıcının belirli bir ürün için otomatik teklif ayarlarını temsil eder.
    /// Bir kullanıcı bir ürüne sadece bir adet 'AutoBid' ayarı yapabilir.
    /// </summary>
    public class AutoBid
    {
        #region Bileşik Anahtar (Composite Key) Özellikleri

        // Bu iki alan birlikte bu tablonun Birincil Anahtarını (PK) oluşturur.
        // Bu yapılandırma DbContext -> OnModelCreating içinde yapılmalıdır.

        /// <summary>
        /// Ayarı yapan kullanıcının kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public string UserId { get; private set; }

        /// <summary>
        /// Ayarın yapıldığı ürünün kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public int ProductId { get; private set; }

        #endregion

        #region Diğer Özellikler (Properties)

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal MaxAmount { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal IncrementAmount { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? LastModifiedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        // DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private AutoBid() { }

        /// <summary>
        /// Yeni bir 'AutoBid' ayarı oluşturur ve alan kurallarını zorunlu kılar.
        /// </summary>
        public AutoBid(string userId, int productId, decimal maxAmount, decimal incrementAmount)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ProductId.", nameof(productId));

            // Ayarları 'UpdateSettings' üzerinden yaparak doğrulamayı tek bir yerde topluyoruz (DRY).
            UpdateSettings(maxAmount, incrementAmount);

            UserId = userId;
            ProductId = productId;
            IsActive = true; // Varsayılan olarak aktif
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = null;
        }

        /// <summary>
        /// Otomatik teklif ayarlarını günceller ve kuralları zorunlu kılar.
        /// </summary>
        public void UpdateSettings(decimal newMaxAmount, decimal newIncrementAmount)
        {
            // DÜZELTME: Alan (Domain) kuralları burada zorunlu kılındı.
            if (newMaxAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(newMaxAmount), "Maksimum tutar pozitif olmalıdır.");
            if (newIncrementAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(newIncrementAmount), "Artırım tutarı pozitif olmalıdır.");
            if (newIncrementAmount > newMaxAmount)
                throw new ArgumentException("Artırım tutarı, maksimum tutardan büyük olamaz.");

            MaxAmount = newMaxAmount;
            IncrementAmount = newIncrementAmount;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive) return; // Zaten aktifse işlem yapma
            IsActive = true;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive) return; // Zaten pasifse işlem yapma
            IsActive = false;
            LastModifiedDate = DateTime.UtcNow;
        }

        #endregion
    }
}