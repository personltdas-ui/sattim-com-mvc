using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Favorite
{
    public class Favorite
    {
        #region Bileşik Anahtar Özellikleri

        [Required]
        public string UserId { get; private set; }

        [Required]
        public int ProductId { get; private set; }

        #endregion

        #region Diğer Özellikler

        public DateTime AddedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar

        private Favorite() { }

        public Favorite(string userId, int productId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));

            UserId = userId;
            ProductId = productId;
            AddedDate = DateTime.UtcNow;
        }

        #endregion
    }
}