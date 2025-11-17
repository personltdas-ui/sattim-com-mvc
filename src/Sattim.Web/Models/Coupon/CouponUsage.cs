using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Coupon
{
    /// <summary>
    /// Bir kuponun, bir kullanıcı tarafından, belirli bir ürün satışı için
    /// kullanımını kaydeder. Product ile Bire-Bir (1-to-1) ilişkiye sahiptir.
    /// </summary>
    public class CouponUsage
    {
        #region Özellikler ve İlişkiler

        /// <summary>
        /// Bu tablonun Birincil Anahtarı (PK).
        /// Aynı zamanda Product tablosuna olan Yabancı Anahtardır (FK).
        /// Bu, bir ürün satışı için sadece bir kupon kullanılabileceğini garanti eder (1-to-1).
        /// </summary>
        [Key]
        [ForeignKey("Product")]
        [Required]
        public int ProductId { get; private set; }

        /// <summary>
        /// Kullanılan kuponun kimliği (Foreign Key).
        /// </summary>
        [Required]
        public int CouponId { get; private set; }

        /// <summary>
        /// Kuponu kullanan kullanıcının kimliği (Foreign Key).
        /// </summary>
        [Required]
        public string UserId { get; private set; }

        /// <summary>
        /// Bu kupon sayesinde uygulanan net indirim tutarı.
        /// </summary>
        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")] // Para birimi için net tip
        public decimal DiscountApplied { get; private set; }

        public DateTime UsedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: Kullanılan kupon.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("CouponId")]
        public virtual Coupon Coupon { get; private set; }

        /// <summary>
        /// Navigasyon: Kuponu kullanan kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        /// <summary>
        /// Navigasyon: Kuponun uygulandığı ürün satışı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        public virtual Product.Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private CouponUsage() { }

        /// <summary>
        /// Yeni bir 'CouponUsage' kaydı oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public CouponUsage(int productId, int couponId, string userId, decimal discountApplied)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için tüm ID'ler ve değerler doğrulandı.
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (couponId <= 0)
                throw new ArgumentException("Geçersiz kupon kimliği.", nameof(couponId));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");
            if (discountApplied <= 0)
                throw new ArgumentOutOfRangeException(nameof(discountApplied), "Uygulanan indirim pozitif olmalıdır.");

            ProductId = productId;
            CouponId = couponId;
            UserId = userId;
            DiscountApplied = discountApplied;
            UsedDate = DateTime.UtcNow; // Kullanım tarihi o an atanır
        }

        #endregion
    }
}