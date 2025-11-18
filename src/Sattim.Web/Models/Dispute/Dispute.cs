using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Dispute
{
    public class Dispute
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        public DisputeReason Reason { get; private set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; private set; }

        [Required]
        public DisputeStatus Status { get; private set; }

        [StringLength(4000)]
        public string? Resolution { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? ResolvedDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public int ProductId { get; private set; }

        [Required]
        public string InitiatorId { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        [ForeignKey("InitiatorId")]
        public virtual ApplicationUser Initiator { get; private set; }

        public virtual ICollection<DisputeMessage> Messages { get; private set; } = new List<DisputeMessage>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private Dispute() { }

        public Dispute(int productId, string initiatorId, DisputeReason reason, string description)
        {
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(initiatorId))
                throw new ArgumentNullException(nameof(initiatorId), "Başlatan kullanıcı kimliği boş olamaz.");
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Açıklama boş olamaz.", nameof(description));

            ProductId = productId;
            InitiatorId = initiatorId;
            Reason = reason;
            Description = description;

            Status = DisputeStatus.Open;
            CreatedDate = DateTime.UtcNow;
        }

        public void PutUnderReview()
        {
            if (Status != DisputeStatus.Open)
                throw new InvalidOperationException("'Açık' olmayan bir anlaşmazlık 'İncelemeye' alınamaz.");

            Status = DisputeStatus.UnderReview;
        }

        public void Resolve(string resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
                throw new ArgumentException("Çözüm açıklaması boş olamaz.", nameof(resolution));

            if (Status != DisputeStatus.Open && Status != DisputeStatus.UnderReview)
                throw new InvalidOperationException("Sadece 'Açık' veya 'İncelemede' olan bir anlaşmazlık çözülebilir.");

            Resolution = resolution;
            Status = DisputeStatus.Resolved;
            ResolvedDate = DateTime.UtcNow;
        }

        public void Close()
        {
            if (Status != DisputeStatus.Resolved)
                throw new InvalidOperationException("Sadece 'Çözülmüş' bir anlaşmazlık 'Kapalı' duruma getirilebilir.");

            Status = DisputeStatus.Closed;
        }

        #endregion
    }

    public enum DisputeReason
    {
        ProductNotAsDescribed,
        ProductNotReceived,
        PaymentIssue,
        FraudulentSeller,
        FraudulentBuyer,
        Other
    }

    public enum DisputeStatus
    {
        Open,
        UnderReview,
        Resolved,
        Closed
    }
}