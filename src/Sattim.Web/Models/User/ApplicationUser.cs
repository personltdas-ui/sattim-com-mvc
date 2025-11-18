using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.Models.User
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Tam ad alanı zorunludur.")]
        [StringLength(100)]
        public string FullName { get; private set; }

        public DateTime RegisteredDate { get; private set; }

        [StringLength(1000)]
        public string? ProfileImageUrl { get; private set; }

        public bool IsActive { get; private set; }

        protected ApplicationUser()
        {
        }

        public ApplicationUser(string userName, string email, string fullName) : base(userName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentNullException(nameof(fullName), "Tam ad boş olamaz.");
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentNullException(nameof(userName), "Kullanıcı adı boş olamaz.");
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email), "E-posta boş olamaz.");

            Email = email;
            NormalizedEmail = email.ToUpperInvariant();
            NormalizedUserName = userName.ToUpperInvariant();
            EmailConfirmed = false;

            FullName = fullName;
            RegisteredDate = DateTime.UtcNow;
            IsActive = true;
            LockoutEnd = null;
        }

        public void UpdateProfile(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Tam ad boş olamaz.", nameof(fullName));

            FullName = fullName;
        }

        public void UpdateProfileImage(string? imageUrl)
        {
            ProfileImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl;
        }

        public void Deactivate()
        {
            if (!IsActive) return;

            IsActive = false;
            LockoutEnd = DateTimeOffset.MaxValue;
        }

        public void Activate()
        {
            if (IsActive) return;

            IsActive = true;
            LockoutEnd = null;
        }
    }
}