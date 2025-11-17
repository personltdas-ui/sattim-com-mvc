using Sattim.Web.Models.User;
using Sattim.Web.Models.Product; // Product namespace'i için eklendi
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Message
{
    /// <summary>
    /// İki kullanıcı (Sender, Receiver) arasındaki, isteğe bağlı olarak
    /// bir ürünle (Product) ilişkilendirilmiş tek bir mesajı temsil eder.
    /// </summary>
    public class Message
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; private set; }

        public bool IsRead { get; private set; }
        public DateTime SentDate { get; private set; }
        public DateTime? ReadDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        [Required]
        public string SenderId { get; private set; }

        [Required]
        public string ReceiverId { get; private set; }

        /// <summary>
        /// Mesajın ilgili olduğu ürün (isteğe bağlı, Foreign Key).
        /// </summary>
        public int? ProductId { get; private set; }

        /// <summary>
        /// Navigasyon: Mesajı gönderen kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; private set; }

        /// <summary>
        /// Navigasyon: Mesajı alan kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser Receiver { get; private set; }

        /// <summary>
        /// Navigasyon: Mesajın ilgili olduğu ürün (isteğe bağlı).
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product.Product? Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Message() { }

        /// <summary>
        /// Yeni bir 'Message' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public Message(string senderId, string receiverId, string content, int? productId = null)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için tüm ID'ler ve değerler doğrulandı.
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

        /// <summary>
        /// Mesajı 'Okundu' olarak işaretler.
        /// </summary>
        public void MarkAsRead()
        {
            if (IsRead) return; // Zaten okunduysa tekrar işlem yapma

            IsRead = true;
            ReadDate = DateTime.UtcNow;
        }

        #endregion
    }
}