using Sattim.Web.Models.Wallet;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey], [Column] için eklendi

namespace Sattim.Web.Models.Wallet
{
    /// <summary>
    /// Bir cüzdanda (Wallet) gerçekleşen, değiştirilemez (immutable) tek bir
    /// finansal işlemi (örn: Para Yatırma, Para Çekme) temsil eder.
    /// </summary>
    public class WalletTransaction
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; private set; } // Pozitif (Giriş) veya Negatif (Çıkış)

        [Required]
        public WalletTransactionType Type { get; private set; }

        [Required]
        [StringLength(500)]
        public string Description { get; private set; }

        // --- İsteğe bağlı ilişki ---
        [StringLength(50)]
        public string? RelatedEntityId { get; private set; }

        [StringLength(50)]
        public string? RelatedEntityType { get; private set; }

        public DateTime CreatedDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Bu işlemin ait olduğu Cüzdanın kimliği (Foreign Key).
        /// Cüzdanın PK'si 'UserId' olduğu için bu alan da 'WalletUserId'dir.
        /// </summary>
        [Required]
        public string WalletUserId { get; private set; }

        /// <summary>
        /// Navigasyon: İşlemin ait olduğu Cüzdan (Wallet).
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("WalletUserId")]
        public virtual Wallet Wallet { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private WalletTransaction() { }

        /// <summary>
        /// Yeni bir 'WalletTransaction' (işlem dekontu) oluşturur ve kuralları zorunlu kılar.
        /// Bu, bir işlem yaratmanın tek geçerli yoludur.
        /// </summary>
        public WalletTransaction(string walletUserId, decimal amount, WalletTransactionType type, string description, string? relatedEntityId = null, string? relatedEntityType = null)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için tüm ID'ler ve değerler doğrulandı.
            if (string.IsNullOrWhiteSpace(walletUserId))
                throw new ArgumentNullException(nameof(walletUserId), "Cüzdan kullanıcı kimliği boş olamaz.");
            if (amount == 0m)
                throw new ArgumentOutOfRangeException(nameof(amount), "İşlem tutarı sıfır olamaz.");
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentNullException(nameof(description), "Açıklama boş olamaz.");

            // DÜZELTME: İlişkisel doğrulama eklendi ("ya hep ya hiç").
            if (string.IsNullOrWhiteSpace(relatedEntityId) != string.IsNullOrWhiteSpace(relatedEntityType))
            {
                throw new ArgumentException("RelatedEntityId ve RelatedEntityType ya birlikte doldurulmalı ya da birlikte boş bırakılmalıdır.");
            }

            WalletUserId = walletUserId;
            Amount = amount;
            Type = type;
            Description = description;
            RelatedEntityId = string.IsNullOrWhiteSpace(relatedEntityId) ? null : relatedEntityId;
            RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType;
            CreatedDate = DateTime.UtcNow; // Zaman damgası o an atanır
        }

        #endregion
    }

    // Enum (Değişiklik yok, gayet iyi)
    public enum WalletTransactionType
    {
        Deposit,    // Para yükleme (+)
        Withdrawal, // Para çekme (-)
        Payment,    // Ödeme yapma (örn: ürün satın alma) (-)
        Refund,     // İade (+)
        Commission, // Komisyon kesintisi (-)
        Bonus       // Bonus/Promosyon (+)
    }
}