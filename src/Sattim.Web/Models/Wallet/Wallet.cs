using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Key], [ForeignKey], [Column] için eklendi

namespace Sattim.Web.Models.Wallet
{
    /// <summary>
    /// Bir kullanıcının (ApplicationUser) site içi bakiyesini temsil eder.
    /// Kullanıcı ile Bire-Bir (1-to-1) ilişkiye sahiptir.
    /// Bakiye, sadece 'Deposit' ve 'Withdraw' metotları ile yönetilir.
    /// </summary>
    public class Wallet
    {
        #region Özellikler ve Bire-Bir İlişki

        /// <summary>
        /// Bu tablonun Birincil Anahtarı (PK).
        /// Aynı zamanda ApplicationUser'a olan Yabancı Anahtardır (FK).
        /// Bu, birebir ilişkiyi garanti eder.
        /// </summary>
        [Key]
        public string UserId { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")] // Para birimi için net tip
        public decimal Balance { get; private set; }

        public DateTime LastUpdated { get; private set; }

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: Bu cüzdanın ait olduğu kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        // Not: 'Transactions' koleksiyonunun buradan kaldırılması
        // performans için mükemmel bir mimari karardır.
        // İşlemler, 'WalletTransactionRepository' üzerinden sorgulanmalıdır.

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Wallet() { }

        /// <summary>
        /// Yeni bir 'Wallet' nesnesi oluşturur (varsayılan olarak 0 bakiye ile).
        /// Genellikle kullanıcı kaydı (registration) sırasında çağrılır.
        /// </summary>
        public Wallet(string userId)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID doğrulaması eklendi.
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");

            UserId = userId;
            Balance = 0m; // Bakiye her zaman sıfırdan başlar
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Cüzdana para yatırır (Bakiye artırır).
        /// </summary>
        public void Deposit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Yatırılan tutar pozitif olmalıdır.");

            Balance += amount;
            LastUpdated = DateTime.UtcNow;

            // Not: Bu işlem, bir 'WalletTransaction' (işlem kaydı)
            // oluşturulmasını tetiklemelidir (Bu mantık Servis Katmanındadır).
        }

        /// <summary>
        /// Cüzdandan para çeker (Bakiye azaltır) ve yeterli bakiye olup olmadığını kontrol eder.
        /// </summary>
        public void Withdraw(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Çekilen tutar pozitif olmalıdır.");
            if (Balance < amount)
                throw new InvalidOperationException("Yetersiz bakiye."); // Kritik iş kuralı

            Balance -= amount;
            LastUpdated = DateTime.UtcNow;

            // Not: Bu işlem, bir 'WalletTransaction' (işlem kaydı)
            // oluşturulmasını tetiklemelidir (Bu mantık Servis Katmanındadır).
        }

        #endregion
    }
}