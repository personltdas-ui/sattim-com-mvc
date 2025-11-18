using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.Payment;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Escrow
{
    public class Escrow
    {
        #region Özellikler ve Bire-Bir İlişki

        [Key]
        [ForeignKey("Product")]
        public int ProductId { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; private set; }

        [Required]
        public EscrowStatus Status { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? ReleasedDate { get; private set; }
        public DateTime? RefundedDate { get; private set; }

        [StringLength(1000)]
        public string? DisputeReason { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public string BuyerId { get; private set; }

        [Required]
        public string SellerId { get; private set; }

        [ForeignKey("BuyerId")]
        public virtual ApplicationUser Buyer { get; private set; }

        [ForeignKey("SellerId")]
        public virtual ApplicationUser Seller { get; private set; }

        public virtual Product.Product Product { get; private set; }

        public virtual ICollection<Payment.Payment> Payments { get; private set; } = new List<Payment.Payment>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private Escrow() { }

        public Escrow(int productId, string buyerId, string sellerId, decimal amount)
        {
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(buyerId))
                throw new ArgumentNullException(nameof(buyerId), "Alıcı kimliği boş olamaz.");
            if (string.IsNullOrWhiteSpace(sellerId))
                throw new ArgumentNullException(nameof(sellerId), "Satıcı kimliği boş olamaz.");
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Tutar pozitif olmalıdır.");
            if (string.Equals(buyerId, sellerId))
                throw new InvalidOperationException("Alıcı ve satıcı aynı kişi olamaz.");

            ProductId = productId;
            BuyerId = buyerId;
            SellerId = sellerId;
            Amount = amount;
            Status = EscrowStatus.Pending;
            CreatedDate = DateTime.UtcNow;
        }

        public void Fund()
        {
            if (Status != EscrowStatus.Pending)
                throw new InvalidOperationException("'Beklemede' olmayan bir hesap fonlanamaz.");

            Status = EscrowStatus.Funded;
        }

        public void Release()
        {
            if (Status != EscrowStatus.Funded && Status != EscrowStatus.Disputed)
                throw new InvalidOperationException("Sadece 'Fonlanmış' veya 'İhtilaflı' bir hesaptan para serbest bırakılabilir.");

            Status = EscrowStatus.Released;
            ReleasedDate = DateTime.UtcNow;
        }

        public void Refund()
        {
            if (Status != EscrowStatus.Funded && Status != EscrowStatus.Disputed)
                throw new InvalidOperationException("Sadece 'Fonlanmış' veya 'İhtilaflı' bir hesaptan para iade edilebilir.");

            Status = EscrowStatus.Refunded;
            RefundedDate = DateTime.UtcNow;
        }

        public void OpenDispute(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("İhtilaf nedeni boş olamaz.", nameof(reason));
            if (Status != EscrowStatus.Funded)
                throw new InvalidOperationException("Sadece 'Fonlanmış' bir hesap için ihtilaf açılabilir.");

            Status = EscrowStatus.Disputed;
            DisputeReason = reason;
        }

        public void ResolveByReleasing()
        {
            if (Status != EscrowStatus.Disputed)
                throw new InvalidOperationException("Sadece 'İhtilaflı' bir durum bu metotla çözülebilir.");

            Status = EscrowStatus.Released;
            ReleasedDate = DateTime.UtcNow;
        }

        public void ResolveByRefunding()
        {
            if (Status != EscrowStatus.Disputed)
                throw new InvalidOperationException("Sadece 'İhtilaflı' bir durum bu metotla çözülebilir.");

            Status = EscrowStatus.Refunded;
            RefundedDate = DateTime.UtcNow;
        }

        #endregion
    }

    public enum EscrowStatus
    {
        Pending,
        Funded,
        Released,
        Refunded,
        Disputed,
        Shipped
    }
}