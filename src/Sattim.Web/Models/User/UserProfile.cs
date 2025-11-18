using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.User
{
    public class UserProfile
    {
        [Key]
        [Required]
        public string UserId { get; private set; }

        [StringLength(500)]
        public string? Address { get; private set; }

        [StringLength(100)]
        public string? City { get; private set; }

        [StringLength(100)]
        public string? Country { get; private set; }

        [StringLength(20)]
        public string? PostalCode { get; private set; }

        [StringLength(2000)]
        public string? Bio { get; private set; }

        [StringLength(1000)]
        public string? IdCardImageUrl { get; private set; }

        public bool IsVerified { get; private set; }
        public DateTime? VerifiedDate { get; private set; }

        public int RatingCount { get; private set; }

        [Range(0, 5)]
        public decimal AverageRating { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        private UserProfile() { }

        public UserProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            UserId = userId;
            IsVerified = false;
            RatingCount = 0;
            AverageRating = 0m;
        }

        public void UpdateDetails(string? address, string? city, string? country, string? postalCode, string? bio)
        {
            Address = address;
            City = city;
            Country = country;
            PostalCode = postalCode;
            Bio = bio;
        }

        public void SetIdCardUrl(string? url)
        {
            IdCardImageUrl = url;
            Unverify();
        }

        public void Verify()
        {
            if (IsVerified) return;

            if (string.IsNullOrWhiteSpace(IdCardImageUrl))
                throw new InvalidOperationException("Kimlik kartı yüklenmeden doğrulama yapılamaz.");

            IsVerified = true;
            VerifiedDate = DateTime.UtcNow;
        }

        public void Unverify()
        {
            IsVerified = false;
            VerifiedDate = null;
        }

        public void UpdateRating(int newCount, decimal newAverage)
        {
            if (newCount < 0)
                throw new ArgumentOutOfRangeException(nameof(newCount), "Rating sayısı 0'dan küçük olamaz.");
            if (newAverage < 0 || newAverage > 5)
                throw new ArgumentOutOfRangeException(nameof(newAverage), "Ortalama puan 0 ile 5 arasında olmalıdır.");

            RatingCount = newCount;
            AverageRating = newAverage;
        }
    }
}