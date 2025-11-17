using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Security
{
    /// <summary>
    /// Kullanıcılarla ilgili (veya anonim) güvenlik olaylarını kaydeden
    /// değiştirilemez (immutable) bir log varlığı.
    /// </summary>
    public class SecurityLog
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        public SecurityEventType EventType { get; private set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; private set; }

        [Required]
        [StringLength(50)] // IPv6 uyumlu
        public string IpAddress { get; private set; }

        [StringLength(500)]
        public string? UserAgent { get; private set; }

        [Required]
        public SeverityLevel Severity { get; private set; }

        public DateTime Timestamp { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Olayın ilgili olduğu kullanıcı (eğer varsa, Foreign Key).
        /// Örn: 'FailedLogin' için null olabilir.
        /// </summary>
        public string? UserId { get; private set; }

        /// <summary>
        /// Navigasyon: Olayın ilgili olduğu kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private SecurityLog() { }

        /// <summary>
        /// Yeni bir 'SecurityLog' kaydı oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public SecurityLog(SecurityEventType eventType, SeverityLevel severity, string description, string ipAddress, string? userId = null, string? userAgent = null)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için doğrulamalar eklendi.
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentNullException(nameof(description), "Açıklama boş olamaz.");
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP adresi boş olamaz.");

            // İyileştirme: Boş string'leri 'null' olarak kaydet
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;

            EventType = eventType;
            Description = description;
            IpAddress = ipAddress;
            Severity = severity;
            Timestamp = DateTime.UtcNow; // Zaman damgası O AN atanır, dışarıdan alınmaz
        }

        #endregion
    }

    // Enum'lar (Değişiklik yok, gayet iyi)
    public enum SecurityEventType
    {
        Login,
        Logout,
        Register,
        FailedLogin,
        PasswordChanged,
        ProfileRejected,
        ProfileApproved,
        IdCardUploaded,
        EmailChanged,
        TwoFactorEnabled,
        TwoFactorDisabled,
        SuspiciousActivity,
        AccountLocked,
        AccountUnlocked
    }

    public enum SeverityLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }
}