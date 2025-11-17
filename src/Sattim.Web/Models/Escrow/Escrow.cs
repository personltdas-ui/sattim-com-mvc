using Sattim.Web.Models.User;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.Payment;
using System; // ArgumentException vb. için eklendi
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Key], [ForeignKey], [Column] için eklendi

namespace Sattim.Web.Models.Escrow
{
    /// <summary>
    /// Bir satış işlemi (Product) için alıcı (Buyer) ve satıcı (Seller)
    /// arasındaki para akışını yöneten güvenli hesabı temsil eder.
    /// Product ile Bire-Bir (1-to-1) ilişkiye sahiptir.
    /// </summary>
    public class Escrow
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

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")] // Para birimi için net tip
        public decimal Amount { get; private set; } // Ödenmesi GEREKEN toplam tutar

        [Required]
        public EscrowStatus Status { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? ReleasedDate { get; private set; }
        public DateTime? RefundedDate { get; private set; }

        [StringLength(1000)]
        public string? DisputeReason { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        [Required]
        public string BuyerId { get; private set; }

        [Required]
        public string SellerId { get; private set; }

        /// <summary>
        /// Navigasyon: Alıcı kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("BuyerId")]
        public virtual ApplicationUser Buyer { get; private set; }

        /// <summary>
        /// Navigasyon: Satıcı kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("SellerId")]
        public virtual ApplicationUser Seller { get; private set; }

        /// <summary>
        /// Navigasyon: Bu güvenli hesabın ait olduğu ürün/satış.
        /// DÜZELTME: EF Core Tembel Yüklemesi için 'virtual' eklendi.
        /// </summary>
        public virtual Product.Product Product { get; private set; }

        /// <summary>
        /// Navigasyon: Bu güvenli hesaba yapılan ödeme girişimleri (1'e Çok).
        /// DÜZELTME: 'IEnumerable<T>' -> 'ICollection<T>' ve 'virtual' eklendi.
        /// </summary>
        public virtual ICollection<Payment.Payment> Payments { get; private set; } = new List<Payment.Payment>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Escrow() { }

        /// <summary>
        /// Yeni bir 'Escrow' (Güvenli Hesap) oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public Escrow(int productId, string buyerId, string sellerId, decimal amount)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için tüm ID'ler ve değerler doğrulandı.
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
            Status = EscrowStatus.Pending; // Para bekleniyor
            CreatedDate = DateTime.UtcNow;
        }

        // --- Durum Makinesi (State Machine) Metotları ---

        /// <summary>
        /// Hesabı 'Fonlandı' (parası ödendi) durumuna geçirir.
        /// </summary>
        public void Fund()
        {
            if (Status != EscrowStatus.Pending)
                throw new InvalidOperationException("'Beklemede' olmayan bir hesap fonlanamaz.");

            Status = EscrowStatus.Funded;
        }

        /// <summary>
        /// Parayı satıcıya 'Serbest Bırakır'.
        /// </summary>
        public void Release()
        {
            if (Status != EscrowStatus.Funded)
                throw new InvalidOperationException("Sadece 'Fonlanmış' bir hesaptan para serbest bırakılabilir.");

            Status = EscrowStatus.Released;
            ReleasedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Parayı alıcıya 'İade Eder'.
        /// </summary>
        public void Refund()
        {
            if (Status != EscrowStatus.Funded)
                throw new InvalidOperationException("Sadece 'Fonlanmış' bir hesaptan para iade edilebilir.");

            Status = EscrowStatus.Refunded;
            RefundedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Hesap için 'İhtilaf' (Anlaşmazlık) açar.
        /// </summary>
        public void OpenDispute(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("İhtilaf nedeni boş olamaz.", nameof(reason));
            if (Status != EscrowStatus.Funded)
                throw new InvalidOperationException("Sadece 'Fonlanmış' bir hesap için ihtilaf açılabilir.");

            Status = EscrowStatus.Disputed;
            DisputeReason = reason;
        }

        /// <summary>
        /// İhtilafı satıcı lehine 'Serbest Bırakarak' çözer.
        /// </summary>
        public void ResolveByReleasing()
        {
            if (Status != EscrowStatus.Disputed)
                throw new InvalidOperationException("Sadece 'İhtilaflı' bir durum bu metotla çözülebilir.");

            Status = EscrowStatus.Released;
            ReleasedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// İhtilafı alıcı lehine 'İade Ederek' çözer.
        /// </summary>
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
        Pending,   // Para bekleniyor
        Funded,    // Para yatırıldı (Güvenli hesapta)
        Released,  // Satıcıya ödendi
        Refunded,  // Alıcıya iade edildi
        Disputed,   // İhtilaf var
        Shipped
    }
}