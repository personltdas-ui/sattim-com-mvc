using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Bid
{
    /// <summary>
    /// Bir ürüne yapılan tek bir teklifi temsil eder.
    /// Bu model "değişmez" (immutable) olarak tasarlanmıştır;
    /// bir teklif yapıldıktan sonra tutarı veya kimin yaptığı değiştirilemez.
    /// </summary>
    public class Bid
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        /// <summary>
        /// Teklif tutarı. decimal.MaxValue'ya kadar izin verir.
        /// </summary>
        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; private set; }

        /// <summary>
        /// Teklifin tam olarak ne zaman yapıldığının UTC zaman damgası.
        /// </summary>
        public DateTime BidDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Teklifin yapıldığı ürünün kimliği (Foreign Key).
        /// </summary>
        [Required]
        public int ProductId { get; private set; }

        /// <summary>
        /// Teklifi veren kullanıcının kimliği (Foreign Key).
        /// </summary>
        [Required]
        public string BidderId { get; private set; }

        /// <summary>
        /// Navigasyon: Teklifin yapıldığı ürün.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product.Product Product { get; private set; }

        /// <summary>
        /// Navigasyon: Teklifi veren kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("BidderId")]
        public virtual ApplicationUser Bidder { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core'un nesneyi veritabanından okurken
        /// kullanması için gereken korumalı (veya özel) yapıcı metot.
        /// </summary>
        private Bid() { }

        /// <summary>
        /// Yeni bir 'Bid' (Teklif) nesnesi oluşturur.
        /// Bu, bir teklif yaratmanın tek geçerli yoludur ve kuralları zorunlu kılar.
        /// </summary>
        public Bid(int productId, string bidderId, decimal amount)
        {
            // Alan (Domain) Kuralları
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ProductId.", nameof(productId));
            if (string.IsNullOrWhiteSpace(bidderId))
                throw new ArgumentNullException(nameof(bidderId), "Teklifi verenin kimliği boş olamaz.");
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Teklif tutarı pozitif olmalıdır.");

            ProductId = productId;
            BidderId = bidderId;
            Amount = amount;
            BidDate = DateTime.UtcNow; // Teklif tarihi sistem tarafından o an atanır
        }

        #endregion
    }
}