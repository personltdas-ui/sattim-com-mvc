using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Audit
{
    public class AuditLog
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(100)]
        public string Action { get; private set; }

        [StringLength(100)]
        public string? EntityName { get; private set; }

        [StringLength(50)]
        public string? EntityId { get; private set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; private set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; private set; }

        public DateTime Timestamp { get; private set; }

        [StringLength(50)]
        public string? IpAddress { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        public string? UserId { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; private set; }

        #endregion

        #region Yapıcı Metotlar

        private AuditLog() { }

        public AuditLog(string action, string? entityName, string? entityId,
                        string? oldValues, string? newValues,
                        string? ipAddress, string? userId)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentNullException(nameof(action), "Eylem (Action) boş olamaz.");

            if (string.IsNullOrWhiteSpace(entityId) != string.IsNullOrWhiteSpace(entityName))
            {
                throw new ArgumentException("EntityId ve EntityName ya birlikte doldurulmalı ya da birlikte boş bırakılmalıdır.");
            }

            Action = action;
            Timestamp = DateTime.UtcNow;

            EntityName = string.IsNullOrWhiteSpace(entityName) ? null : entityName;
            EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId;
            OldValues = string.IsNullOrWhiteSpace(oldValues) ? null : oldValues;
            NewValues = string.IsNullOrWhiteSpace(newValues) ? null : newValues;
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress;
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
        }

        #endregion
    }
}