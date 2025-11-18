using Sattim.Web.Models.Wallet;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Wallet
{
    public class WalletTransaction
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; private set; }

        [Required]
        public WalletTransactionType Type { get; private set; }

        [Required]
        [StringLength(500)]
        public string Description { get; private set; }

        [StringLength(50)]
        public string? RelatedEntityId { get; private set; }

        [StringLength(50)]
        public string? RelatedEntityType { get; private set; }

        public DateTime CreatedDate { get; private set; }

        [Required]
        public string WalletUserId { get; private set; }

        [ForeignKey("WalletUserId")]
        public virtual Wallet Wallet { get; private set; }

        private WalletTransaction() { }

        public WalletTransaction(string walletUserId, decimal amount, WalletTransactionType type, string description, string? relatedEntityId = null, string? relatedEntityType = null)
        {
            if (string.IsNullOrWhiteSpace(walletUserId))
                throw new ArgumentNullException(nameof(walletUserId), "Cüzdan kullanıcı kimliği boş olamaz.");
            if (amount == 0m)
                throw new ArgumentOutOfRangeException(nameof(amount), "İşlem tutarı sıfır olamaz.");
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentNullException(nameof(description), "Açıklama boş olamaz.");

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
            CreatedDate = DateTime.UtcNow;
        }
    }

    public enum WalletTransactionType
    {
        Deposit,
        Withdrawal,
        Payment,
        Refund,
        Commission,
        Bonus
    }
}