using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Bid
{
    public class Bid
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; private set; }

        public DateTime BidDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public int ProductId { get; private set; }

        [Required]
        public string BidderId { get; private set; }

        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        [ForeignKey("BidderId")]
        public virtual ApplicationUser Bidder { get; private set; }

        #endregion

        #region Yapıcı Metotlar

        private Bid() { }

        public Bid(int productId, string bidderId, decimal amount)
        {
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ProductId.", nameof(productId));
            if (string.IsNullOrWhiteSpace(bidderId))
                throw new ArgumentNullException(nameof(bidderId), "Teklifi verenin kimliği boş olamaz.");
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Teklif tutarı pozitif olmalıdır.");

            ProductId = productId;
            BidderId = bidderId;
            Amount = amount;
            BidDate = DateTime.UtcNow;
        }

        #endregion
    }
}