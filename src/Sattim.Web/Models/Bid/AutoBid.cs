using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Bid
{
    public class AutoBid
    {
        #region Bileşik Anahtar Özellikleri

        [Required]
        public string UserId { get; private set; }

        [Required]
        public int ProductId { get; private set; }

        #endregion

        #region Diğer Özellikler

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal MaxAmount { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal IncrementAmount { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? LastModifiedDate { get; private set; }

        #endregion

        #region Navigasyon Özellikleri

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private AutoBid() { }

        public AutoBid(string userId, int productId, decimal maxAmount, decimal incrementAmount)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ProductId.", nameof(productId));

            UpdateSettings(maxAmount, incrementAmount);

            UserId = userId;
            ProductId = productId;
            IsActive = true;
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = null;
        }

        public void UpdateSettings(decimal newMaxAmount, decimal newIncrementAmount)
        {
            if (newMaxAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(newMaxAmount), "Maksimum tutar pozitif olmalıdır.");
            if (newIncrementAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(newIncrementAmount), "Artırım tutarı pozitif olmalıdır.");
            if (newIncrementAmount > newMaxAmount)
                throw new ArgumentException("Artırım tutarı, maksimum tutardan büyük olamaz.");

            MaxAmount = newMaxAmount;
            IncrementAmount = newIncrementAmount;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            LastModifiedDate = DateTime.UtcNow;
        }

        #endregion
    }
}