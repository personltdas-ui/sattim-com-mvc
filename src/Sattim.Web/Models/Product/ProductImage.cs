using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Product
{
    public class ProductImage
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(1000)]
        public string ImageUrl { get; private set; }

        public bool IsPrimary { get; private set; }

        [Range(0, 100)]
        public int DisplayOrder { get; private set; }

        public DateTime UploadedDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public int ProductId { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private ProductImage() { }

        public ProductImage(int productId, string imageUrl, int displayOrder = 0)
        {
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentNullException(nameof(imageUrl), "Resim URL'i boş olamaz.");

            ProductId = productId;
            ImageUrl = imageUrl;
            DisplayOrder = (displayOrder < 0) ? 0 : displayOrder;
            IsPrimary = false;
            UploadedDate = DateTime.UtcNow;
        }

        public void SetAsPrimary(bool isPrimary)
        {
            IsPrimary = isPrimary;
        }

        public void UpdateDisplayOrder(int newOrder)
        {
            if (newOrder < 0)
                throw new ArgumentOutOfRangeException(nameof(newOrder), "Sıralama 0'dan küçük olamaz.");

            DisplayOrder = newOrder;
        }

        #endregion
    }
}