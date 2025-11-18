using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Dispute
{
    public class DisputeMessage
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; private set; }

        public DateTime SentDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public int DisputeId { get; private set; }

        [Required]
        public string SenderId { get; private set; }

        [ForeignKey("DisputeId")]
        public virtual Dispute Dispute { get; private set; }

        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; private set; }

        #endregion

        #region Yapıcı Metotlar

        private DisputeMessage() { }

        public DisputeMessage(int disputeId, string senderId, string message)
        {
            if (disputeId <= 0)
                throw new ArgumentException("Geçersiz anlaşmazlık kimliği.", nameof(disputeId));
            if (string.IsNullOrWhiteSpace(senderId))
                throw new ArgumentNullException(nameof(senderId), "Gönderen kullanıcı kimliği boş olamaz.");
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Mesaj içeriği boş olamaz.", nameof(message));

            DisputeId = disputeId;
            SenderId = senderId;
            Message = message;
            SentDate = DateTime.UtcNow;
        }

        #endregion
    }
}