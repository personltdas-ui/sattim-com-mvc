using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Sattim.Web.Models.UI
{
    [Index(nameof(Name), IsUnique = true)]
    public class EmailTemplate
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(100)]
        public string Name { get; private set; }

        [Required]
        [StringLength(255)]
        public string Subject { get; private set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Body { get; private set; }

        [Required]
        public EmailTemplateType Type { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? ModifiedDate { get; private set; }

        private EmailTemplate() { }

        public EmailTemplate(string name, EmailTemplateType type, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "Şablon adı boş olamaz.");

            UpdateTemplate(subject, body);

            Name = name;
            Type = type;
            IsActive = true;
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = null;
        }

        public void UpdateTemplate(string newSubject, string newBody)
        {
            if (string.IsNullOrWhiteSpace(newSubject))
                throw new ArgumentException("Konu boş olamaz.", nameof(newSubject));
            if (string.IsNullOrWhiteSpace(newBody))
                throw new ArgumentException("İçerik boş olamaz.", nameof(newBody));

            Subject = newSubject;
            Body = newBody;
            ModifiedDate = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive) return;

            IsActive = true;
            ModifiedDate = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive) return;

            IsActive = false;
            ModifiedDate = DateTime.UtcNow;
        }
    }

    public enum EmailTemplateType
    {
        Welcome,
        BidNotification,
        BidOutbid,
        AuctionWon,
        AuctionLost,
        AuctionEnding,
        PaymentConfirmation,
        ShippingNotification,
        PasswordReset,
        EmailVerification
    }
}