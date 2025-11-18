using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Security
{
    public class SecurityLog
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        public SecurityEventType EventType { get; private set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; private set; }

        [Required]
        [StringLength(50)]
        public string IpAddress { get; private set; }

        [StringLength(500)]
        public string? UserAgent { get; private set; }

        [Required]
        public SeverityLevel Severity { get; private set; }

        public DateTime Timestamp { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        public string? UserId { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; private set; }

        #endregion

        #region Yapıcı Metotlar

        private SecurityLog() { }

        public SecurityLog(SecurityEventType eventType, SeverityLevel severity, string description, string ipAddress, string? userId = null, string? userAgent = null)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentNullException(nameof(description), "Açıklama boş olamaz.");
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP adresi boş olamaz.");

            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;

            EventType = eventType;
            Description = description;
            IpAddress = ipAddress;
            Severity = severity;
            Timestamp = DateTime.UtcNow;
        }

        #endregion
    }

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