using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Sattim.Web.Models.UI
{
    [Index(nameof(Email), IsUnique = true)]
    public class Newsletter
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime SubscribedDate { get; private set; }
        public DateTime? UnsubscribedDate { get; private set; }

        private Newsletter() { }

        public Newsletter(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email), "E-posta adresi boş olamaz.");

            Email = email;
            IsActive = true;
            SubscribedDate = DateTime.UtcNow;
            UnsubscribedDate = null;
        }

        public void Unsubscribe()
        {
            if (!IsActive) return;

            IsActive = false;
            UnsubscribedDate = DateTime.UtcNow;
        }

        public void Resubscribe()
        {
            if (IsActive) return;

            IsActive = true;
            UnsubscribedDate = null;
        }
    }
}