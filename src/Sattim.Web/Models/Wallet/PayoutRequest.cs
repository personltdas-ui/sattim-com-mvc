using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey], [Column] için eklendi

namespace Sattim.Web.Models.Wallet
{
    /// <summary>
    /// Kullanıcıların cüzdanlarından ('Wallet') banka hesaplarına
    /// para çekme taleplerini yönetir.
    /// </summary>
    public class PayoutRequest
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")] // Para birimi için net tip
        public decimal Amount { get; private set; } // Çekilmek istenen tutar

        [Required]
        public PayoutStatus Status { get; private set; }

        // --- Banka Bilgileri (Değişmez) ---
        [Required]
        [StringLength(100)]
        public string BankName { get; private set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; private set; }

        [Required]
        [StringLength(34)] // IBAN
        public string IBAN { get; private set; }

        // --- Zaman Damgaları ---
        public DateTime RequestedDate { get; private set; }
        public DateTime? CompletedDate { get; private set; }

        [StringLength(1000)]
        public string? AdminNote { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        [Required]
        public string UserId { get; private set; }

        /// <summary>
        /// Bu talebi karşılayan cüzdan işlemi (para çıkışı).
        /// 'Rejected' talepler için null olabilir.
        /// </summary>
        public int? WalletTransactionId { get; private set; }

        /// <summary>
        /// Navigasyon: Talebi yapan kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        /// <summary>
        /// Navigasyon: İlişkili cüzdan işlemi.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("WalletTransactionId")]
        public virtual WalletTransaction? WalletTransaction { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private PayoutRequest() { }

        /// <summary>
        /// Yeni bir 'PayoutRequest' (Para Çekme Talebi) oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public PayoutRequest(string userId, decimal amount, string bankName, string fullName, string iban)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID doğrulaması eklendi.
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Tutar pozitif olmalıdır.");
            if (string.IsNullOrWhiteSpace(iban)) throw new ArgumentNullException(nameof(iban));
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentNullException(nameof(fullName));
            if (string.IsNullOrWhiteSpace(bankName)) throw new ArgumentNullException(nameof(bankName));

            UserId = userId;
            Amount = amount;
            BankName = bankName;
            FullName = fullName;
            IBAN = iban;
            Status = PayoutStatus.Pending; // Her zaman 'Beklemede' başlar
            RequestedDate = DateTime.UtcNow;
        }

        // --- Durum Makinesi (State Machine) Metotları (Fail-Fast) ---

        /// <summary>
        /// Talebi onaylar (Admin tarafından).
        /// </summary>
        public void Approve(string? adminNote = null) // Not opsiyonel olabilir
        {
            // DÜZELTME: 'Fail-Fast' (Hızlı Hata Ver)
            if (Status != PayoutStatus.Pending)
                throw new InvalidOperationException("Sadece 'Beklemede' olan bir talep 'Onaylanabilir'.");

            Status = PayoutStatus.Approved;
            AdminNote = adminNote;
            CompletedDate = null; // Onaylandı ama henüz tamamlanmadı
        }

        /// <summary>
        /// Talebi reddeder (Admin tarafından). Para cüzdana geri yüklenmelidir (Servis Katmanı).
        /// </summary>
        public void Reject(string adminNote)
        {
            if (string.IsNullOrWhiteSpace(adminNote))
                throw new ArgumentException("Reddetme notu zorunludur.", nameof(adminNote));
            if (Status != PayoutStatus.Pending && Status != PayoutStatus.Approved)
                throw new InvalidOperationException("Sadece 'Beklemede' veya 'Onaylanmış' bir talep 'Reddedilebilir'.");

            Status = PayoutStatus.Rejected;
            AdminNote = adminNote;
            CompletedDate = DateTime.UtcNow; // Reddetme de bir "tamamlanma" türüdür
        }

        /// <summary>
        /// Talebi tamamlar (Banka transferi yapıldı).
        /// </summary>
        public void Complete(string? adminNote = null) // Not opsiyonel olabilir
        {
            // DÜZELTME: 'Fail-Fast' (Hızlı Hata Ver)
            if (Status != PayoutStatus.Approved)
                throw new InvalidOperationException("Sadece 'Onaylanmış' bir talep 'Tamamlanabilir'.");

            Status = PayoutStatus.Completed;
            AdminNote = adminNote;
            CompletedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Bu talebi, cüzdandan parayı düşen 'WalletTransaction' kaydına bağlar.
        /// (Mükemmel alan mantığı, zaten vardı).
        /// </summary>
        public void LinkTransaction(int transactionId)
        {
            if (WalletTransactionId.HasValue)
                throw new InvalidOperationException("Bu talep zaten bir cüzdan işlemine bağlı.");
            if (transactionId <= 0)
                throw new ArgumentException("Geçersiz transaction kimliği.", nameof(transactionId));

            WalletTransactionId = transactionId;
        }

        #endregion
    }

    public enum PayoutStatus
    {
        Pending,   // Admin onayı bekleniyor (Para cüzdandan DÜŞTÜ)
        Approved,  // Onaylandı, banka transferi yapılacak
        Rejected,  // Reddedildi (Para cüzdana iade edilmeli - Servis Katmanı)
        Completed  // Para gönderildi (İşlem bitti)
    }
}