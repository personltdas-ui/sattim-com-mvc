using Sattim.Web.Models.User;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Dispute
{
    /// <summary>
    /// Bir anlaşmazlık (Dispute) içindeki tek bir mesajı temsil eder.
    /// Bu mesajlar oluşturulduktan sonra değiştirilemez (immutable).
    /// </summary>
    public class DisputeMessage
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; private set; }

        public DateTime SentDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Mesajın ait olduğu anlaşmazlığın kimliği (Foreign Key).
        /// </summary>
        [Required]
        public int DisputeId { get; private set; }

        /// <summary>
        /// Mesajı gönderen kullanıcının kimliği (Foreign Key).
        /// </summary>
        [Required]
        public string SenderId { get; private set; }

        /// <summary>
        /// Navigasyon: Mesajın ait olduğu anlaşmazlık.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("DisputeId")]
        public virtual Dispute Dispute { get; private set; }

        /// <summary>
        /// Navigasyon: Mesajı gönderen kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private DisputeMessage() { }

        /// <summary>
        /// Yeni bir 'DisputeMessage' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public DisputeMessage(int disputeId, string senderId, string message)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için doğrulamalar eklendi.
            if (disputeId <= 0)
                throw new ArgumentException("Geçersiz anlaşmazlık kimliği.", nameof(disputeId));
            if (string.IsNullOrWhiteSpace(senderId))
                throw new ArgumentNullException(nameof(senderId), "Gönderen kullanıcı kimliği boş olamaz.");
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Mesaj içeriği boş olamaz.", nameof(message));

            DisputeId = disputeId;
            SenderId = senderId;
            Message = message;
            SentDate = DateTime.UtcNow; // Gönderim tarihi o an atanır
        }

        #endregion
    }
}