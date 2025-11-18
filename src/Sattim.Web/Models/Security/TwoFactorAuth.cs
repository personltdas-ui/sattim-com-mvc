using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Security
{
    public class TwoFactorAuth
    {
        #region Özellikler ve Bire-Bir İlişki

        [Key]
        [Required]
        public string UserId { get; private set; }

        public bool IsEnabled { get; private set; }

        [StringLength(256)]
        public string? SecretKey { get; private set; }

        [StringLength(1000)]
        public string? BackupCodes { get; private set; }

        public DateTime? EnabledDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private TwoFactorAuth() { }

        public TwoFactorAuth(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");

            UserId = userId;
            IsEnabled = false;
            SecretKey = null;
            BackupCodes = null;
            EnabledDate = null;
        }

        public void Enable(string secretKey, string backupCodesJson)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new ArgumentException("Gizli anahtar boş olamaz.", nameof(secretKey));
            if (string.IsNullOrWhiteSpace(backupCodesJson))
                throw new ArgumentException("Yedek kodlar boş olamaz.", nameof(backupCodesJson));

            IsEnabled = true;
            SecretKey = secretKey;
            BackupCodes = backupCodesJson;
            EnabledDate = DateTime.UtcNow;
        }

        public void Disable()
        {
            if (!IsEnabled) return;

            IsEnabled = false;
            SecretKey = null;
            BackupCodes = null;
            EnabledDate = null;
        }

        public void RegenerateBackupCodes(string newBackupCodesJson)
        {
            if (!IsEnabled)
                throw new InvalidOperationException("Devre dışı olan 2FA için yedek kodlar yenilenemez.");
            if (string.IsNullOrWhiteSpace(newBackupCodesJson))
                throw new ArgumentException("Yeni yedek kodlar boş olamaz.", nameof(newBackupCodesJson));

            BackupCodes = newBackupCodesJson;
        }

        #endregion
    }
}