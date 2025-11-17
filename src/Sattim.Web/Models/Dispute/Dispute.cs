using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.Collections.Generic; // ICollection için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Dispute
{
    /// <summary>
    /// Bir kullanıcı (Initiator) tarafından bir ürün/satış (Product) hakkında
    /// başlatılan bir anlaşmazlığı temsil eder.
    /// </summary>
    public class Dispute
    {
        #region Özellikler (Properties)

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

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Anlaşmazlığın ilgili olduğu ürün (Foreign Key).
        /// </summary>
        [Required]
        public int ProductId { get; private set; }

        /// <summary>
        /// Anlaşmazlığı başlatan kullanıcı (Foreign Key).
        /// </summary>
        [Required]
        public string InitiatorId { get; private set; }

        /// <summary>
        /// Navigasyon: İlgili ürün.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        /// <summary>
        /// Navigasyon: Başlatan kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("InitiatorId")]
        public virtual ApplicationUser Initiator { get; private set; }

        /// <summary>
        /// Navigasyon: Bu anlaşmazlıktaki mesajlar (1'e Çok).
        /// DÜZELTME: 'IEnumerable<T>' -> 'ICollection<T>' ve 'virtual' eklendi.
        /// </summary>
        public virtual ICollection<DisputeMessage> Messages { get; private set; } = new List<DisputeMessage>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Dispute() { }

        /// <summary>
        /// Yeni bir 'Dispute' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public Dispute(int productId, string initiatorId, DisputeReason reason, string description)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için doğrulamalar eklendi.
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(initiatorId))
                throw new ArgumentNullException(nameof(initiatorId), "Başlatan kullanıcı kimliği boş olamaz.");
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Açıklama boş olamaz.", nameof(description));
            // Not: 'reason' enum olduğu için varsayılan olarak geçerli kabul edilir.

            ProductId = productId;
            InitiatorId = initiatorId;
            Reason = reason;
            Description = description;

            Status = DisputeStatus.Open; // Her zaman 'Açık' olarak başlar
            CreatedDate = DateTime.UtcNow;
        }

        // --- Durum Makinesi Metotları ---

        /// <summary>
        /// Anlaşmazlığı 'İncelemede' durumuna alır.
        /// </summary>
        public void PutUnderReview()
        {
            if (Status != DisputeStatus.Open)
                throw new InvalidOperationException("'Açık' olmayan bir anlaşmazlık 'İncelemeye' alınamaz.");

            Status = DisputeStatus.UnderReview;
        }

        /// <summary>
        /// Anlaşmazlığı bir çözüm açıklamasıyla 'Çözüldü' olarak işaretler.
        /// </summary>
        public void Resolve(string resolution)
        {
            // DÜZELTME: Çözüm metni doğrulaması eklendi.
            if (string.IsNullOrWhiteSpace(resolution))
                throw new ArgumentException("Çözüm açıklaması boş olamaz.", nameof(resolution));

            if (Status != DisputeStatus.Open && Status != DisputeStatus.UnderReview)
                throw new InvalidOperationException("Sadece 'Açık' veya 'İncelemede' olan bir anlaşmazlık çözülebilir.");

            Resolution = resolution;
            Status = DisputeStatus.Resolved;
            ResolvedDate = DateTime.UtcNow;
        }

        /// <Example>
        /// Anlaşmazlığı 'Kapalı' olarak işaretler (Kullanıcı onayı veya otomatik).
        /// </summary>
        public void Close()
        {
            if (Status != DisputeStatus.Resolved)
                throw new InvalidOperationException("Sadece 'Çözülmüş' bir anlaşmazlık 'Kapalı' duruma getirilebilir.");

            Status = DisputeStatus.Closed;
        }

        #endregion
    }

    // Enum'lar (Değişiklik yok, gayet iyi)
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