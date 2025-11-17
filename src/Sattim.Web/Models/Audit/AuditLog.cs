using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey], [Column] için eklendi

namespace Sattim.Web.Models.Audit
{
    /// <summary>
    /// Sistemdeki önemli değişiklikleri (örn: "ProductCreated", "UserUpdated")
    /// kaydeden, değiştirilemez (immutable) bir denetim kaydı varlığı.
    /// </summary>
    public class AuditLog
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(100)]
        public string Action { get; private set; } // Örn: "ProductCreated"

        [StringLength(100)]
        public string? EntityName { get; private set; } // Örn: "Product"

        [StringLength(50)]
        public string? EntityId { get; private set; } // Örn: "123"

        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; private set; } // JSON

        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; private set; } // JSON

        public DateTime Timestamp { get; private set; }

        [StringLength(50)] // IPv6 uyumlu
        public string? IpAddress { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Eylemi gerçekleştiren kullanıcı (eğer varsa, Foreign Key).
        /// Sistem eylemleri için 'null' olabilir.
        /// </summary>
        public string? UserId { get; private set; }

        /// <summary>
        /// Navigasyon: Eylemi gerçekleştiren kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private AuditLog() { }

        /// <summary>
        /// Yeni bir 'AuditLog' kaydı oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public AuditLog(string action, string? entityName, string? entityId,
                        string? oldValues, string? newValues,
                        string? ipAddress, string? userId)
        {
            // DÜZELTME: Ana doğrulama
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentNullException(nameof(action), "Eylem (Action) boş olamaz.");

            // DÜZELTME: İlişkisel doğrulama eklendi ("ya hep ya hiç").
            if (string.IsNullOrWhiteSpace(entityId) != string.IsNullOrWhiteSpace(entityName))
            {
                throw new ArgumentException("EntityId ve EntityName ya birlikte doldurulmalı ya da birlikte boş bırakılmalıdır.");
            }

            Action = action;
            Timestamp = DateTime.UtcNow; // Zaman damgası O AN atanır

            // DÜZELTME: Tüm isteğe bağlı string'lerin 'boş' değil 'null' olmasını garantile
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