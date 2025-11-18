using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Commission
{
    public class Commission
    {
        #region Özellikler ve Bire-Bir İlişki

        [Key]
        [ForeignKey("Product")]
        public int ProductId { get; private set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductPrice { get; private set; }

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; private set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; private set; }

        [Required]
        public CommissionStatus Status { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? PaidDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri

        public virtual Product.Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private Commission() { }

        public Commission(int productId, decimal productPrice, decimal commissionRate)
        {
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (productPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(productPrice), "Ürün fiyatı pozitif olmalıdır.");
            if (commissionRate < 0 || commissionRate > 100)
                throw new ArgumentOutOfRangeException(nameof(commissionRate), "Komisyon oranı 0-100 arası olmalıdır.");

            ProductId = productId;
            ProductPrice = productPrice;
            CommissionRate = commissionRate;

            CommissionAmount = CalculateCommission(productPrice, commissionRate);

            Status = CommissionStatus.Pending;
            CreatedDate = DateTime.UtcNow;
            PaidDate = null;
        }

        private decimal CalculateCommission(decimal price, decimal rate)
        {
            var amount = price * (rate / 100m);
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }

        public void MarkAsCollected()
        {
            if (Status == CommissionStatus.Collected) return;

            if (Status != CommissionStatus.Pending)
                throw new InvalidOperationException("'Beklemede' olmayan bir komisyon 'Tahsil Edildi' olarak işaretlenemez.");

            Status = CommissionStatus.Collected;
            PaidDate = DateTime.UtcNow;
        }

        public void Waive()
        {
            if (Status == CommissionStatus.Waived) return;

            if (Status != CommissionStatus.Pending)
                throw new InvalidOperationException("'Beklemede' olmayan bir komisyondan 'Feragat Edilemez'.");

            Status = CommissionStatus.Waived;
            PaidDate = null;
        }

        #endregion
    }

    public enum CommissionStatus
    {
        Pending,
        Collected,
        Waived
    }
}