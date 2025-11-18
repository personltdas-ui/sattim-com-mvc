using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Wallet
{
    public class PayoutRequest
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; private set; }

        [Required]
        public PayoutStatus Status { get; private set; }

        [Required]
        [StringLength(100)]
        public string BankName { get; private set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; private set; }

        [Required]
        [StringLength(34)]
        public string IBAN { get; private set; }

        public DateTime RequestedDate { get; private set; }
        public DateTime? CompletedDate { get; private set; }

        [StringLength(1000)]
        public string? AdminNote { get; private set; }

        [Required]
        public string UserId { get; private set; }

        public int? WalletTransactionId { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        [ForeignKey("WalletTransactionId")]
        public virtual WalletTransaction? WalletTransaction { get; private set; }

        private PayoutRequest() { }

        public PayoutRequest(string userId, decimal amount, string bankName, string fullName, string iban)
        {
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
            Status = PayoutStatus.Pending;
            RequestedDate = DateTime.UtcNow;
        }

        public void Approve(string? adminNote = null)
        {
            if (Status != PayoutStatus.Pending)
                throw new InvalidOperationException("Sadece 'Beklemede' olan bir talep 'Onaylanabilir'.");

            Status = PayoutStatus.Approved;
            AdminNote = adminNote;
            CompletedDate = null;
        }

        public void Reject(string adminNote)
        {
            if (string.IsNullOrWhiteSpace(adminNote))
                throw new ArgumentException("Reddetme notu zorunludur.", nameof(adminNote));
            if (Status != PayoutStatus.Pending && Status != PayoutStatus.Approved)
                throw new InvalidOperationException("Sadece 'Beklemede' veya 'Onaylanmış' bir talep 'Reddedilebilir'.");

            Status = PayoutStatus.Rejected;
            AdminNote = adminNote;
            CompletedDate = DateTime.UtcNow;
        }

        public void Complete(string? adminNote = null)
        {
            if (Status != PayoutStatus.Approved)
                throw new InvalidOperationException("Sadece 'Onaylanmış' bir talep 'Tamamlanabilir'.");

            Status = PayoutStatus.Completed;
            AdminNote = adminNote;
            CompletedDate = DateTime.UtcNow;
        }

        public void LinkTransaction(int transactionId)
        {
            if (WalletTransactionId.HasValue)
                throw new InvalidOperationException("Bu talep zaten bir cüzdan işlemine bağlı.");
            if (transactionId <= 0)
                throw new ArgumentException("Geçersiz transaction kimliği.", nameof(transactionId));

            WalletTransactionId = transactionId;
        }
    }

    public enum PayoutStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed
    }
}