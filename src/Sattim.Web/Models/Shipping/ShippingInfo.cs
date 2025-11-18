using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Shipping
{
    public class ShippingInfo
    {
        #region Özellikler ve Bire-Bir İlişki

        [Key]
        [ForeignKey("Product")]
        public int ProductId { get; private set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; private set; }

        [Required]
        [StringLength(500)]
        public string Address { get; private set; }

        [Required]
        [StringLength(100)]
        public string City { get; private set; }

        [Required]
        [StringLength(20)]
        public string PostalCode { get; private set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; private set; }

        [StringLength(100)]
        public string? TrackingNumber { get; private set; }

        [StringLength(100)]
        public string? Carrier { get; private set; }

        [Required]
        public ShippingStatus Status { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? ShippedDate { get; private set; }
        public DateTime? DeliveredDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        [Required]
        public string BuyerId { get; private set; }

        public virtual Product.Product Product { get; private set; }

        [ForeignKey("BuyerId")]
        public virtual ApplicationUser Buyer { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private ShippingInfo() { }

        public ShippingInfo(int productId, string buyerId, string fullName, string address, string city, string postalCode, string phone)
        {
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(buyerId))
                throw new ArgumentNullException(nameof(buyerId), "Alıcı kimliği boş olamaz.");

            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Tam ad boş olamaz.", nameof(fullName));
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Adres boş olamaz.", nameof(address));
            if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("Şehir boş olamaz.", nameof(city));
            if (string.IsNullOrWhiteSpace(postalCode)) throw new ArgumentException("Posta kodu boş olamaz.", nameof(postalCode));
            if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Telefon boş olamaz.", nameof(phone));

            ProductId = productId;
            BuyerId = buyerId;
            FullName = fullName;
            Address = address;
            City = city;
            PostalCode = postalCode;
            Phone = phone;

            Status = ShippingStatus.Pending;
            CreatedDate = DateTime.UtcNow;
        }

        public void MarkAsPreparing()
        {
            if (Status != ShippingStatus.Pending)
                throw new InvalidOperationException("Sadece 'Beklemede' olan kargolar 'Hazırlanıyor' olarak işaretlenebilir.");

            Status = ShippingStatus.Preparing;
        }

        public void Ship(string carrier, string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(carrier) || string.IsNullOrWhiteSpace(trackingNumber))
                throw new ArgumentException("Kargoya vermek için Kargo Firması ve Takip Numarası zorunludur.");
            if (Status != ShippingStatus.Pending && Status != ShippingStatus.Preparing)
                throw new InvalidOperationException("Sadece 'Beklemede' veya 'Hazırlanıyor' durumundaki kargolar kargoya verilebilir.");

            Carrier = carrier;
            TrackingNumber = trackingNumber;
            Status = ShippingStatus.Shipped;
            ShippedDate = DateTime.UtcNow;
        }

        public void MarkAsInTransit()
        {
            if (Status != ShippingStatus.Shipped)
                throw new InvalidOperationException("Sadece 'Kargoya Verildi' durumundaki kargolar 'Yolda' olarak işaretlenebilir.");

            Status = ShippingStatus.InTransit;
        }

        public void Deliver()
        {
            if (Status != ShippingStatus.Shipped && Status != ShippingStatus.InTransit)
                throw new InvalidOperationException("Sadece 'Kargoya Verildi' veya 'Yolda' olan bir kargo teslim edilebilir.");

            Status = ShippingStatus.Delivered;
            DeliveredDate = DateTime.UtcNow;
        }

        public void Return()
        {
            if (Status != ShippingStatus.Shipped && Status != ShippingStatus.InTransit && Status != ShippingStatus.Delivered)
                throw new InvalidOperationException("Sadece 'Kargoya Verildi', 'Yolda' veya 'Teslim Edildi' durumundaki bir kargo iade edilebilir.");

            Status = ShippingStatus.Returned;
        }

        #endregion
    }

    public enum ShippingStatus
    {
        Pending,
        Preparing,
        Shipped,
        InTransit,
        Delivered,
        Returned
    }
}