using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Coupon
{
    public class CouponUsage
    {
        #region Özellikler ve İlişkiler

        [Key]
        [ForeignKey("Product")]
        [Required]
        public int ProductId { get; private set; }

        [Required]
        public int CouponId { get; private set; }

        [Required]
        public string UserId { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountApplied { get; private set; }

        public DateTime UsedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri

        [ForeignKey("CouponId")]
        public virtual Coupon Coupon { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        public virtual Product.Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar

        private CouponUsage() { }

        public CouponUsage(int productId, int couponId, string userId, decimal discountApplied)
        {
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
            UsedDate = DateTime.UtcNow;
        }

        #endregion
    }
}