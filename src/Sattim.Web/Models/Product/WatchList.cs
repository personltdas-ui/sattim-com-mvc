using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Product
{
    /// <summary>
    /// Bir kullanıcının bir ürünü 'İzleme Listesine' almasını temsil eder.
    /// Bildirim ayarlarını da içerir. (Çoka-Çok ilişki).
    /// </summary>
    public class WatchList
    {
        #region Bileşik Anahtar (Composite Key) Özellikleri

        // Bu iki alan birlikte bu tablonun Birincil Anahtarını (PK) oluşturur.
        // Bu yapılandırma DbContext -> OnModelCreating içinde yapılmalıdır.

        /// <summary>
        /// İzleyen kullanıcının kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public string UserId { get; private set; }

        /// <summary>
        /// İzlenen ürünün kimliği (PK parçası, FK).
        /// </summary>
        [Required]
        public int ProductId { get; private set; }

        #endregion

        #region Diğer Özellikler (Properties)

        public bool NotifyOnNewBid { get; private set; }
        public bool NotifyBeforeEnd { get; private set; }
        public DateTime AddedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: İzleyen kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        /// <summary>
        /// Navigasyon: İzlenen ürün.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private WatchList() { }

        /// <summary>
        /// Yeni bir 'WatchList' ilişki nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public WatchList(string userId, int productId)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için doğrulamalar eklendi.
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));

            UserId = userId;
            ProductId = productId;
            AddedDate = DateTime.UtcNow;

            // Varsayılan bildirim ayarları
            NotifyOnNewBid = true;
            NotifyBeforeEnd = true;
        }

        /// <summary>
        /// İzleme listesi için bildirim ayarlarını günceller.
        /// </summary>
        public void UpdateNotificationSettings(bool notifyOnBid, bool notifyBeforeEnd)
        {
            NotifyOnNewBid = notifyOnBid;
            NotifyBeforeEnd = notifyBeforeEnd;
        }

        #endregion
    }
}