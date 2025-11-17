using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Favorite
{
    /// <summary>
    /// Bir kullanıcının bir ürünü favorilerine eklemesini temsil eder.
    /// (User ile Product arasında Çoka-Çok ilişki tablosu).
    /// </summary>
    public class Favorite
    {
        #region Bileşik Anahtar (Composite Key) Özellikleri

        // Bu iki alan birlikte bu tablonun Birincil Anahtarını (PK) oluşturur.
        // Bu yapılandırma DbContext -> OnModelCreating içinde yapılmalıdır.

        /// <summary>
        /// Favoriye ekleyen kullanıcının kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public string UserId { get; private set; }

        /// <summary>
        /// Favoriye eklenen ürünün kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public int ProductId { get; private set; }

        #endregion

        #region Diğer Özellikler (Properties)

        public DateTime AddedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: Favoriye ekleyen kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        /// <summary>
        /// Navigasyon: Favoriye eklenen ürün.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Favorite() { }

        /// <summary>
        /// Yeni bir 'Favorite' ilişki nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public Favorite(string userId, int productId)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için doğrulamalar eklendi.
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));

            UserId = userId;
            ProductId = productId;
            AddedDate = DateTime.UtcNow; // Eklenme tarihi o an atanır
        }

        #endregion
    }
}