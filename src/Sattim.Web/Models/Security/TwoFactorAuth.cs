using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Key], [ForeignKey] için eklendi

namespace Sattim.Web.Models.Security
{
    /// <summary>
    /// Bir kullanıcı (ApplicationUser) için İki Faktörlü Kimlik Doğrulama (2FA)
    /// ayarlarını temsil eder. Kullanıcı ile Bire-Bir (1-to-1) ilişkiye sahiptir.
    /// </summary>
    public class TwoFactorAuth
    {
        #region Özellikler ve Bire-Bir İlişki

        /// <summary>
        /// Bu tablonun Birincil Anahtarı (PK).
        /// Aynı zamanda ApplicationUser'a olan Yabancı Anahtardır (FK).
        /// Bu, birebir ilişkiyi garanti eder.
        /// </summary>
        [Key]
        [Required]
        public string UserId { get; private set; }

        public bool IsEnabled { get; private set; }

        [StringLength(256)]
        public string? SecretKey { get; private set; }

        [StringLength(1000)] // JSON array
        public string? BackupCodes { get; private set; }

        public DateTime? EnabledDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: Bu ayarların ait olduğu kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private TwoFactorAuth() { }

        /// <summary>
        /// Yeni bir 'TwoFactorAuth' nesnesi oluşturur (varsayılan olarak devre dışı).
        /// Genellikle kullanıcı kaydı (registration) sırasında çağrılır.
        /// </summary>
        public TwoFactorAuth(string userId)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID doğrulaması eklendi.
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");

            UserId = userId;
            IsEnabled = false;
            SecretKey = null;
            BackupCodes = null;
            EnabledDate = null;
        }

        /// <summary>
        /// 2FA'yı etkinleştirir ve gerekli anahtarları kaydeder.
        /// </summary>
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

        /// <summary>
        /// 2FA'yı devre dışı bırakır ve tüm gizli verileri temizler.
        /// </summary>
        public void Disable()
        {
            if (!IsEnabled) return; // Zaten devre dışıysa işlem yapma

            IsEnabled = false;
            SecretKey = null;
            BackupCodes = null; // Güvenlik için yedek kodları da temizle
            EnabledDate = null;
        }

        /// <summary>
        /// Mevcut 2FA için yedek kodları yeniden oluşturur.
        /// </summary>
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