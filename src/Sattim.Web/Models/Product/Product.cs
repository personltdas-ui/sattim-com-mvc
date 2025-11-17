using Sattim.Web.Models.User;
using Sattim.Web.Models.Category;
using Sattim.Web.Models.Bid;
using Sattim.Web.Models.Favorite;
using Sattim.Web.Models.Commission;
using Sattim.Web.Models.Coupon;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.Shipping;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Product
{
    /// <summary>
    /// Bir açık artırma ürününü (Aggregate Root - Kök Varlık) temsil eder.
    /// Teklifler, resimler ve diğer ilgili alt varlıkların (child entities)
    /// yaşam döngüsünü yönetir ve iş kurallarını zorunlu kılar.
    /// </summary>
    public class Product
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(200)]
        public string Title { get; private set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; private set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal StartingPrice { get; private set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal? ReservePrice { get; private set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
        public decimal CurrentPrice { get; private set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal BidIncrement { get; private set; }

        [Required]
        public DateTime StartDate { get; private set; }

        [Required]
        public DateTime EndDate { get; private set; }

        [Required]
        public ProductStatus Status { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public bool IsEndingSoonNotified { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        [Required]
        public string SellerId { get; private set; }

        [Required]
        public int CategoryId { get; private set; }

        public string? WinnerId { get; private set; }

        // --- 1'e Çok İlişkiler (Navigasyon) ---
        // DÜZELTME: Tembel Yükleme (Lazy Loading) için 'virtual' eklendi.

        [ForeignKey("SellerId")]
        public virtual ApplicationUser Seller { get; private set; }

        [ForeignKey("WinnerId")]
        public virtual ApplicationUser? Winner { get; private set; }

        [ForeignKey("CategoryId")]
        public virtual Category.Category Category { get; private set; }

        // --- 1'e 1 İlişkiler (Navigasyon) ---
        // DÜZELTME: Tembel Yükleme için 'virtual' eklendi.
        public virtual Commission.Commission? Commission { get; private set; }
        public virtual Escrow.Escrow? Escrow { get; private set; }
        public virtual ShippingInfo? ShippingInfo { get; private set; }
        public virtual ProductAnalytics? Analytics { get; private set; }
        public virtual CouponUsage? CouponUsage { get; private set; }

        // --- Koleksiyonlar (Navigasyon) ---
        // DÜZELTME: 'IEnumerable<T>' -> 'virtual ICollection<T>' olarak değiştirildi.
        // (EF Core'un ilişki düzeltmesi (relationship fix-up) ve
        // Tembel Yükleme (Lazy Loading) yapabilmesi için zorunludur).

        public virtual ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();
        public virtual ICollection<Bid.Bid> Bids { get; private set; } = new List<Bid.Bid>();
        public virtual ICollection<AutoBid> AutoBids { get; private set; } = new List<AutoBid>();
        public virtual ICollection<Favorite.Favorite> Favorites { get; private set; } = new List<Favorite.Favorite>();
        public virtual ICollection<WatchList> WatchLists { get; private set; } = new List<WatchList>();
        public virtual ICollection<ProductReview> Reviews { get; private set; } = new List<ProductReview>();
        public virtual ICollection<Dispute.Dispute> Disputes { get; private set; } = new List<Dispute.Dispute>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Product() { }

        /// <summary>
        /// Yeni bir 'Product' nesnesi oluşturur ve tüm iş kurallarını zorunlu kılar.
        /// </summary>
        public Product(string title, string description, decimal startingPrice, decimal bidIncrement, DateTime startDate, DateTime endDate, int categoryId, string sellerId, decimal? reservePrice = null)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için Değişmez (Immutable) alanlar doğrulandı.
            if (string.IsNullOrWhiteSpace(sellerId))
                throw new ArgumentNullException(nameof(sellerId), "Satıcı kimliği boş olamaz.");

            // Diğer tüm alanlar 'UpdateDetails' tarafından doğrulanır ve ayarlanır (DRY Prensibi)
            UpdateDetails(title, description, startingPrice, bidIncrement, startDate, endDate, categoryId, reservePrice);

            SellerId = sellerId;
            Status = ProductStatus.Pending; // Her zaman 'Onay Bekler' başlar
            CreatedDate = DateTime.UtcNow;
            IsEndingSoonNotified = false;
        }

        /// <summary>
        /// Ürün detaylarını günceller (Sadece 'Pending' durumdayken çağrılmalıdır - Servis Katmanı).
        /// Tüm iş kurallarını zorunlu kılar (Aktif Doğrulama).
        /// </summary>
        public void UpdateDetails(string title, string description, decimal startingPrice, decimal bidIncrement, DateTime startDate, DateTime endDate, int categoryId, decimal? reservePrice)
        {
            // DÜZELTME: Tüm doğrulamalar (IValidatableObject'tan taşındı) burada ZORUNLU kılındı.
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Başlık boş olamaz.", nameof(title));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Açıklama boş olamaz.", nameof(description));
            if (categoryId <= 0)
                throw new ArgumentException("Geçersiz kategori kimliği.", nameof(categoryId));
            if (startingPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(startingPrice), "Başlangıç fiyatı pozitif olmalıdır.");
            if (bidIncrement <= 0)
                throw new ArgumentOutOfRangeException(nameof(bidIncrement), "Minumum artış tutarı pozitif olmalıdır.");
            if (endDate <= startDate)
                throw new ArgumentException("Bitiş tarihi, başlangıç tarihinden sonra olmalıdır.");
            if (reservePrice.HasValue && reservePrice < startingPrice)
                throw new ArgumentException("Rezerv fiyat, başlangıç fiyatından düşük olamaz.");

            Title = title;
            Description = description;
            StartingPrice = startingPrice;
            BidIncrement = bidIncrement;
            StartDate = startDate;
            EndDate = endDate;
            CategoryId = categoryId;
            ReservePrice = reservePrice;

            // Başlangıç fiyatı değişirse, mevcut fiyatı da sıfırla (sadece teklif almadıysa)
            CurrentPrice = startingPrice;
            ModifiedDate = DateTime.UtcNow;
        }

        // --- Durum Makinesi (State Machine) Metotları (Fail-Fast) ---

        /// <summary>
        /// Ürünü (Admin tarafından) onaylar ve 'Aktif' hale getirir.
        /// </summary>
        public void Approve()
        {
            // DÜZELTME: 'Fail-Fast' (Hızlı Hata Ver)
            if (Status != ProductStatus.Pending)
                throw new InvalidOperationException("Sadece 'Beklemede' olan ürünler onaylanabilir.");

            Status = ProductStatus.Active;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Ürünü iptal eder (Teklif alıyor olsa bile).
        /// </summary>
        public void Cancel()
        {
            if (Status == ProductStatus.Cancelled || Status == ProductStatus.Sold)
                throw new InvalidOperationException("Zaten 'İptal Edilmiş' veya 'Satılmış' bir ürün tekrar iptal edilemez.");

            Status = ProductStatus.Cancelled;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Gelen yeni bir teklif sonrası mevcut fiyatı günceller (BidService tarafından çağrılır).
        /// </summary>
        public void UpdateCurrentPrice(decimal newAmount)
        {
            if (Status != ProductStatus.Active)
                throw new InvalidOperationException("Sadece 'Aktif' ürünler teklif alabilir.");
            if (newAmount <= CurrentPrice)
                throw new ArgumentException("Yeni teklif, mevcut fiyattan düşük veya eşit olamaz.");
            // Not: 'newAmount'un 'CurrentPrice + BidIncrement' kuralına uyması
            // Servis katmanında (BidService) kontrol edilmelidir, çünkü bu metot
            // AutoBid'den (toplu) veya manuel tekliften (tekil) tetiklenebilir.

            CurrentPrice = newAmount;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Açık artırmayı sonlandırır (Arka plan servisi tarafından çağrılır).
        /// </summary>
        public void CloseAuction(string? winnerId)
        {
            if (Status != ProductStatus.Active)
                throw new InvalidOperationException("Sadece 'Aktif' bir açık artırma sonlandırılabilir.");

            // DÜZELTME: WinnerId'yi doğrula (boş string vs. null)
            string? actualWinnerId = string.IsNullOrWhiteSpace(winnerId) ? null : winnerId;

            if (actualWinnerId == null)
            {
                Status = ProductStatus.Closed; // Süresi doldu, satılmadı
            }
            else
            {
                Status = ProductStatus.Sold;
                WinnerId = actualWinnerId;
            }
            ModifiedDate = DateTime.UtcNow;
        }

        public void MarkAsEndingSoonNotified()
        {
            if (IsEndingSoonNotified) return; // Zaten bildirildiyse işlem yapma
            IsEndingSoonNotified = true;
            ModifiedDate = DateTime.UtcNow;
        }

        #endregion
    }

    public enum ProductStatus
    {
        Pending,   // Admin onayı bekliyor
        Active,    // Aktif (Teklif alıyor)
        Closed,    // Süresi doldu (Satılmadı / Rezerv fiyata ulaşmadı)
        Cancelled, // İptal edildi (Satıcı/Admin tarafından)
        Sold       // Satıldı
    }
}