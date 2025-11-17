using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Key], [ForeignKey] için eklendi

namespace Sattim.Web.Models.Shipping
{
    /// <summary>
    /// Bir ürün satışı (Product) için kargo bilgilerini ve durumunu temsil eder.
    /// Product ile Bire-Bir (1-to-1) ilişkiye sahiptir.
    /// Adres bilgileri oluşturulduktan sonra değiştirilemez (immutable).
    /// </summary>
    public class ShippingInfo
    {
        #region Özellikler ve Bire-Bir İlişki

        /// <summary>
        /// Bu tablonun Birincil Anahtarı (PK).
        /// Aynı zamanda Product tablosuna olan Yabancı Anahtardır (FK).
        /// Bu, birebir ilişkiyi garanti eder.
        /// </summary>
        [Key]
        [ForeignKey("Product")]
        public int ProductId { get; private set; }

        // --- Adres Bilgileri (Değişmez) ---
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

        // --- Kargo Durum Bilgileri ---
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

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        [Required]
        public string BuyerId { get; private set; } // Değişmez

        /// <summary>
        /// Navigasyon: Kargonun ait olduğu ürün/satış.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        public virtual Product.Product Product { get; private set; }

        /// <summary>
        /// Navigasyon: Kargonun alıcısı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("BuyerId")]
        public virtual ApplicationUser Buyer { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private ShippingInfo() { }

        /// <summary>
        /// Yeni bir 'ShippingInfo' (kargo etiketi) oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public ShippingInfo(int productId, string buyerId, string fullName, string address, string city, string postalCode, string phone)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için ID doğrulamaları eklendi.
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (string.IsNullOrWhiteSpace(buyerId))
                throw new ArgumentNullException(nameof(buyerId), "Alıcı kimliği boş olamaz.");

            // Adres doğrulamaları (Sizin kodunuzda zaten vardı, çok iyi)
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

            Status = ShippingStatus.Pending; // Her zaman 'Beklemede' başlar
            CreatedDate = DateTime.UtcNow;
        }

        // --- Durum Makinesi (State Machine) Metotları (Fail-Fast) ---

        public void MarkAsPreparing()
        {
            // DÜZELTME: 'Fail-Fast' (Hızlı Hata Ver)
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
        Pending,   // Ödeme tamamlandı, satıcının kargolaması bekleniyor
        Preparing, // Satıcı kargoyu hazırlıyor (Opsiyonel ara durum)
        Shipped,   // Satıcı tarafından kargoya verildi
        InTransit, // Kargo firması tarafından "Yolda" olarak tarandı
        Delivered, // Alıcıya teslim edildi
        Returned   // Alıcı tarafından iade edildi
    }
}