using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Product
{
    public class ProductView
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(50)]
        public string IpAddress { get; private set; }

        public DateTime ViewDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public int ProductId { get; private set; }

        public string? UserId { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; private set; }

        #endregion

        #region Yapıcı Metotlar

        private ProductView() { }

        public ProductView(int productId, string ipAddress, string? userId = null)
        {
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP adresi boş olamaz.");

            ProductId = productId;
            IpAddress = ipAddress;
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
            ViewDate = DateTime.UtcNow;
        }

        #endregion
    }
}