using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Product
{
    public class ProductReview
    {
        #region Bileşik Anahtar Özellikleri

        [Required]
        public int ProductId { get; private set; }

        [Required]
        public string ReviewerId { get; private set; }

        #endregion

        #region Diğer Özellikler

        [StringLength(2000)]
        public string? Comment { get; private set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; private set; }

        public DateTime ReviewDate { get; private set; }
        public DateTime? LastModifiedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri

        [ForeignKey("ProductId")]
        public virtual Product Product { get; private set; }

        [ForeignKey("ReviewerId")]
        public virtual ApplicationUser Reviewer { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private ProductReview() { }

        public ProductReview(int productId, string reviewerId, int rating, string? comment)
        {
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(reviewerId))
                throw new ArgumentNullException(nameof(reviewerId), "Yorumu yapan kullanıcı kimliği boş olamaz.");

            UpdateReview(rating, comment);

            ProductId = productId;
            ReviewerId = reviewerId;
            ReviewDate = DateTime.UtcNow;
            LastModifiedDate = null;
        }

        public void UpdateReview(int newRating, string? newComment)
        {
            if (newRating < 1 || newRating > 5)
                throw new ArgumentOutOfRangeException(nameof(newRating), "Puan 1-5 arası olmalıdır.");

            Rating = newRating;
            Comment = string.IsNullOrWhiteSpace(newComment) ? null : newComment;
            LastModifiedDate = DateTime.UtcNow;
        }

        #endregion
    }
}