using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.UI
{
    /// <summary>
    /// Bir kullanıcıya (User) gösterilecek tek bir bildirimi temsil eder.
    /// Değiştirilemez (immutable) olarak tasarlanmıştır.
    /// </summary>
    public class Notification
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(150)]
        public string Title { get; private set; }

        [Required]
        [StringLength(500)]
        public string Message { get; private set; }

        [Required]
        public NotificationType Type { get; private set; }

        // --- İsteğe bağlı ilişki ---
        [StringLength(50)]
        public string? RelatedEntityId { get; private set; }

        [StringLength(50)]
        public string? RelatedEntityType { get; private set; }

        public bool IsRead { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? ReadDate { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Bildirimin sahibi olan kullanıcının kimliği (Foreign Key).
        /// </summary>
        [Required]
        public string UserId { get; private set; }

        /// <summary>
        /// Navigasyon: Bildirimin sahibi olan kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Notification() { }

        /// <summary>
        /// Yeni bir 'Notification' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public Notification(string userId, string title, string message, NotificationType type, string? relatedEntityId = null, string? relatedEntityType = null)
        {
            // Mükemmel Doğrulamalar (Zaten vardı)
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

            // DÜZELTME: İlişkisel doğrulama eklendi.
            // Bu iki alan "ya hep ya hiç" kuralına uymalıdır.
            if (string.IsNullOrWhiteSpace(relatedEntityId) != string.IsNullOrWhiteSpace(relatedEntityType))
            {
                throw new ArgumentException("RelatedEntityId ve RelatedEntityType ya birlikte doldurulmalı ya da birlikte boş bırakılmalıdır.");
            }

            UserId = userId;
            Title = title;
            Message = message;
            Type = type;
            RelatedEntityId = string.IsNullOrWhiteSpace(relatedEntityId) ? null : relatedEntityId;
            RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType;

            IsRead = false; // Varsayılan durum
            CreatedDate = DateTime.UtcNow; // Zaman damgası o an atanır
            ReadDate = null;
        }

        /// <summary>
        /// Bildirimi 'okundu' olarak işaretler (Mükemmel "Guard Clause" mantığı).
        /// </summary>
        public void MarkAsRead()
        {
            if (IsRead) return; // Zaten okunduysa gereksiz işlem yapma

            IsRead = true;
            ReadDate = DateTime.UtcNow;
        }

        #endregion
    }

    // Enum (Değişiklik yok, gayet iyi)
    public enum NotificationType
    {
        BidPlaced,        // Ürününüze teklif verildi
        BidOutbid,        // Teklifiniz geçildi
        AuctionWon,       // İhaleyi kazandınız
        AuctionLost,      // İhaleyi kaybettiniz
        AuctionEnding,    // İhale bitiyor (24 saat kala)
        ProductSold,      // Ürününüz satıldı
        ProductApproved,  // Ürününüz onaylandı
        MessageReceived,  // Mesaj aldınız
        PaymentReceived,  // Ödeme alındı
        System            // Sistem bildirimi
    }
}