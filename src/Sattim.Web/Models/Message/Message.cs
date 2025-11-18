using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Message
{
    public class Message
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; private set; }

        public bool IsRead { get; private set; }
        public DateTime SentDate { get; private set; }
        public DateTime? ReadDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public string SenderId { get; private set; }

        [Required]
        public string ReceiverId { get; private set; }

        public int? ProductId { get; private set; }

        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; private set; }

        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser Receiver { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product.Product? Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private Message() { }

        public Message(string senderId, string receiverId, string content, int? productId = null)
        {
            if (string.IsNullOrWhiteSpace(senderId))
                throw new ArgumentNullException(nameof(senderId), "Gönderen kullanıcı kimliği boş olamaz.");
            if (string.IsNullOrWhiteSpace(receiverId))
                throw new ArgumentNullException(nameof(receiverId), "Alıcı kullanıcı kimliği boş olamaz.");
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Mesaj içeriği boş olamaz.", nameof(content));
            if (string.Equals(senderId, receiverId))
                throw new InvalidOperationException("Gönderici ve alıcı aynı kişi olamaz.");
            if (productId.HasValue && productId.Value <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));

            SenderId = senderId;
            ReceiverId = receiverId;
            Content = content;
            ProductId = productId;

            SentDate = DateTime.UtcNow;
            IsRead = false;
            ReadDate = null;
        }

        public void MarkAsRead()
        {
            if (IsRead) return;

            IsRead = true;
            ReadDate = DateTime.UtcNow;
        }

        #endregion
    }
}