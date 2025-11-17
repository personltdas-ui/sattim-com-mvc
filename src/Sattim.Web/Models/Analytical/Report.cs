using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Analytical
{
    /// <summary>
    /// Bir kullanıcı (Reporter) tarafından başka bir varlığı (Ürün, Kullanıcı vb.)
    /// raporlamasını temsil eder.
    /// </summary>
    public class Report
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        public ReportEntityType EntityType { get; private set; }

        [Required]
        [StringLength(50)]
        public string EntityId { get; private set; }

        [Required]
        public ReportReason Reason { get; private set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; private set; }

        [Required]
        public ReportStatus Status { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? ResolvedDate { get; private set; }

        [StringLength(2000)]
        public string? AdminNote { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        [Required]
        public string ReporterId { get; private set; }

        /// <summary>
        /// Navigasyon: Raporu oluşturan kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ReporterId")]
        public virtual ApplicationUser Reporter { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Report() { }

        /// <summary>
        /// Yeni bir 'Report' kaydı oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public Report(string reporterId, ReportEntityType entityType, string entityId, ReportReason reason, string description)
        {
            // Mükemmel Doğrulamalar (Zaten vardı)
            if (string.IsNullOrWhiteSpace(reporterId)) throw new ArgumentNullException(nameof(reporterId));
            if (string.IsNullOrWhiteSpace(entityId)) throw new ArgumentNullException(nameof(entityId));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentNullException(nameof(description));

            ReporterId = reporterId;
            EntityType = entityType;
            EntityId = entityId;
            Reason = reason;
            Description = description;
            Status = ReportStatus.Pending; // Varsayılan durum
            CreatedDate = DateTime.UtcNow;
        }

        // --- Durum Makinesi (State Machine) Metotları (Fail-Fast) ---

        public void PutUnderReview(string? adminNote = null)
        {
            // DÜZELTME: 'Fail-Fast' (Hızlı Hata Ver)
            if (Status != ReportStatus.Pending)
                throw new InvalidOperationException("Sadece 'Beklemede' olan bir rapor 'İncelemeye' alınabilir.");

            Status = ReportStatus.UnderReview;
            AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote;
        }

        public void Resolve(string adminNote)
        {
            if (string.IsNullOrWhiteSpace(adminNote))
                throw new ArgumentException("Çözüm notu zorunludur.", nameof(adminNote));
            if (Status != ReportStatus.Pending && Status != ReportStatus.UnderReview)
                throw new InvalidOperationException("Sadece 'Beklemede' veya 'İncelemede' olan bir rapor 'Çözülebilir'.");

            Status = ReportStatus.Resolved;
            AdminNote = adminNote;
            ResolvedDate = DateTime.UtcNow;
        }

        public void Reject(string adminNote)
        {
            if (string.IsNullOrWhiteSpace(adminNote))
                throw new ArgumentException("Reddetme notu zorunludur.", nameof(adminNote));
            if (Status != ReportStatus.Pending && Status != ReportStatus.UnderReview)
                throw new InvalidOperationException("Sadece 'Beklemede' veya 'İncelemede' olan bir rapor 'Reddedilebilir'.");

            Status = ReportStatus.Rejected;
            AdminNote = adminNote;
            ResolvedDate = DateTime.UtcNow;
        }

        #endregion
    }

    // Enum'lar (Değişiklik yok, gayet iyi)
    public enum ReportEntityType
    {
        Product,
        User,
        Bid,
        Message
    }

    public enum ReportReason
    {
        Spam,
        Inappropriate,
        Fraud,
        Duplicate,
        Other
    }

    public enum ReportStatus
    {
        Pending,
        UnderReview,
        Resolved,
        Rejected
    }
}