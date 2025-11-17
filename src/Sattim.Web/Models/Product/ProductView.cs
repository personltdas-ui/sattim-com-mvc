using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Product
{
    /// <summary>
    /// Bir ürünün aldığı tek bir görüntülemeyi (log kaydı) temsil eder.
    /// Bu, analitik sayımı için ham veridir.
    /// </summary>
    public class ProductView
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(50)]
        public string IpAddress { get; private set; }

        public DateTime ViewDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Görüntülenen ürünün kimliği (Foreign Key).
        /// </summary>
        [Required]
        public int ProductId { get; private set; }

        /// <summary>
        /// Görüntüleyen kullanıcı (giriş yaptıysa, Foreign Key).
        /// </summary>
        public string? UserId { get; private set; }

        /// <summary>
        /// Navigasyon: Görüntülenen ürün.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product Product { get; private set; }

        /// <summary>
        /// Navigasyon: Görüntüleyen kullanıcı (giriş yaptıysa).
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private ProductView() { }

        /// <summary>
        /// Yeni bir 'ProductView' log kaydı oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public ProductView(int productId, string ipAddress, string? userId = null)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID doğrulamaları eklendi.
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP adresi boş olamaz.");

            ProductId = productId;
            IpAddress = ipAddress;
            // İyileştirme: Boş string yerine 'null' kaydet
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
            ViewDate = DateTime.UtcNow; // Görüntüleme tarihi o an atanır
        }

        #endregion
    }
}