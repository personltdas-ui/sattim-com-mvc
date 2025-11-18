using Sattim.Web.Models.Product;
using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.User
{
    public class UserRating
    {
        [Required]
        public string RatedUserId { get; private set; }

        [Required]
        public string RaterUserId { get; private set; }

        [Required]
        public int ProductId { get; private set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; private set; }

        [StringLength(500)]
        public string? Comment { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? LastModifiedDate { get; private set; }

        [ForeignKey("RatedUserId")]
        public virtual ApplicationUser RatedUser { get; private set; }

        [ForeignKey("RaterUserId")]
        public virtual ApplicationUser RaterUser { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        private UserRating() { }

        public UserRating(int productId, string raterUserId, string ratedUserId, int rating, string? comment)
        {
            if (string.Equals(raterUserId, ratedUserId))
                throw new InvalidOperationException("Kullanıcı kendi kendini değerlendiremez.");

            if (rating < 1 || rating > 5)
                throw new ArgumentOutOfRangeException(nameof(rating), "Puan 1-5 arası olmalıdır.");

            if (productId <= 0)
                throw new ArgumentException("Geçersiz ProductId.", nameof(productId));

            if (string.IsNullOrWhiteSpace(raterUserId))
                throw new ArgumentException("Değerlendiren kullanıcı kimliği boş olamaz.", nameof(raterUserId));

            if (string.IsNullOrWhiteSpace(ratedUserId))
                throw new ArgumentException("Değerlendirilen kullanıcı kimliği boş olamaz.", nameof(ratedUserId));

            ProductId = productId;
            RaterUserId = raterUserId;
            RatedUserId = ratedUserId;
            Rating = rating;
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = null;
        }

        public void Update(int newRating, string? newComment)
        {
            if (newRating < 1 || newRating > 5)
                throw new ArgumentOutOfRangeException(nameof(newRating), "Puan 1-5 arası olmalıdır.");

            Rating = newRating;
            Comment = string.IsNullOrWhiteSpace(newComment) ? null : newComment;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}