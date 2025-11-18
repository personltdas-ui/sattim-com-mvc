using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.UI
{
    public class Notification
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(150)]
        public string Title { get; private set; }

        [Required]
        [StringLength(500)]
        public string Message { get; private set; }

        [Required]
        public NotificationType Type { get; private set; }

        [StringLength(50)]
        public string? RelatedEntityId { get; private set; }

        [StringLength(50)]
        public string? RelatedEntityType { get; private set; }

        public bool IsRead { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? ReadDate { get; private set; }

        [Required]
        public string UserId { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        private Notification() { }

        public Notification(string userId, string title, string message, NotificationType type, string? relatedEntityId = null, string? relatedEntityType = null)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrWhiteSpace(relatedEntityId) != string.IsNullOrWhiteSpace(relatedEntityType))
            {
                throw new ArgumentException("RelatedEntityId ve RelatedEntityType ya birlikte doldurulmalı ya da birlikte boş bırakılmalıdır.");
            }

            UserId = userId;
            Title = title;
            Message = message;
            Type = type;
            RelatedEntityId = string.IsNullOrWhiteSpace(relatedEntityId) ? null : relatedEntityId;
            RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType;

            IsRead = false;
            CreatedDate = DateTime.UtcNow;
            ReadDate = null;
        }

        public void MarkAsRead()
        {
            if (IsRead) return;

            IsRead = true;
            ReadDate = DateTime.UtcNow;
        }
    }

    public enum NotificationType
    {
        BidPlaced,
        BidOutbid,
        AuctionWon,
        AuctionLost,
        AuctionEnding,
        ProductSold,
        ProductApproved,
        MessageReceived,
        PaymentReceived,
        System
    }
}