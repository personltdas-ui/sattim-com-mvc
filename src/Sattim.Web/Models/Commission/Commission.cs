using Sattim.Web.Models.Product;
using System; // ArgumentException vb. için eklendi
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Commission
{
    /// <summary>
    /// Bir ürünün başarılı satışından sonra hesaplanan komisyonu temsil eder.
    /// Product ile Bire-Bir (1-to-1) ilişkiye sahiptir.
    /// </summary>
    public class Commission
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

        /// <summary>
        /// Komisyonun hesaplandığı andaki ürün fiyatı (snapshot).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")] // Para birimi için net tip
        public decimal ProductPrice { get; private set; }

        /// <summary>
        /// Uygulanan komisyon oranı (Örn: 15.5 -> %15.5).
        /// </summary>
        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")] // Oran için net tip
        public decimal CommissionRate { get; private set; }

        /// <summary>
        /// Hesaplanan net komisyon tutarı (ProductPrice * (CommissionRate / 100)).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; private set; }

        [Required]
        public CommissionStatus Status { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? PaidDate { get; private set; } // 'Collected' durumuna geçiş tarihi

        #endregion

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: Komisyonun ait olduğu ürün.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        public virtual Product.Product Product { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Commission() { }

        /// <summary>
        /// Yeni bir 'Commission' nesnesi oluşturur ve komisyonu hesaplar.
        /// </summary>
        public Commission(int productId, decimal productPrice, decimal commissionRate)
        {
            // DÜZELTME: Kapsüllemeyi tamamlamak için ID doğrulaması eklendi.
            if (productId <= 0)
                throw new ArgumentException("Geçersiz ürün kimliği.", nameof(productId));
            if (productPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(productPrice), "Ürün fiyatı pozitif olmalıdır.");
            if (commissionRate < 0 || commissionRate > 100)
                throw new ArgumentOutOfRangeException(nameof(commissionRate), "Komisyon oranı 0-100 arası olmalıdır.");

            ProductId = productId;
            ProductPrice = productPrice;
            CommissionRate = commissionRate;

            // Alan (Domain) Mantığı: Komisyon burada hesaplanır
            CommissionAmount = CalculateCommission(productPrice, commissionRate);

            Status = CommissionStatus.Pending; // Her zaman 'Beklemede' başlar
            CreatedDate = DateTime.UtcNow;
            PaidDate = null;
        }

        /// <summary>
        /// Komisyon tutarını 2 ondalık basamağa yuvarlayarak hesaplar.
        /// </summary>
        private decimal CalculateCommission(decimal price, decimal rate)
        {
            var amount = price * (rate / 100m);
            // Finansal hesaplamalarda yuvarlama (Rounding) kritiktir.
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }

        // --- Durum Makinesi Metotları ---

        /// <summary>
        /// Komisyonu 'Tahsil Edildi' olarak işaretler.
        /// </summary>
        public void MarkAsCollected()
        {
            if (Status == CommissionStatus.Collected) return; // Zaten bu durumda

            if (Status != CommissionStatus.Pending)
                throw new InvalidOperationException("'Beklemede' olmayan bir komisyon 'Tahsil Edildi' olarak işaretlenemez.");

            Status = CommissionStatus.Collected;
            PaidDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Komisyondan 'Feragat Edildi' olarak işaretler (örn. iade, promosyon).
        /// </summary>
        public void Waive()
        {
            if (Status == CommissionStatus.Waived) return; // Zaten bu durumda

            if (Status != CommissionStatus.Pending)
                throw new InvalidOperationException("'Beklemede' olmayan bir komisyondan 'Feragat Edilemez'.");

            Status = CommissionStatus.Waived;
            PaidDate = null; // Feragat edildiği için ödeme tarihi olmaz
        }

        #endregion
    }

    /// <summary>
    /// Bir komisyonun durumunu (beklemede, tahsil edildi, feragat edildi) belirtir.
    /// </summary>
    public enum CommissionStatus
    {
        Pending,   // Beklemede (Satış tamamlandı, ödeme bekleniyor)
        Collected, // Tahsil Edildi (Para site hesabına geçti)
        Waived     // Feragat Edildi (İade, iptal veya promosyon nedeniyle alınmadı)
    }
}