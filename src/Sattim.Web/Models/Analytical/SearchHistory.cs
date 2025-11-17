using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için eklendi

namespace Sattim.Web.Models.Analytical
{
    /// <summary>
    /// Bir kullanıcının (veya anonim bir ziyaretçinin) yaptığı tek bir
    /// arama işlemini (log kaydı) temsil eder. Değiştirilemez (immutable).
    /// </summary>
    public class SearchHistory
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(255)]
        public string SearchTerm { get; private set; }

        [Range(0, int.MaxValue)]
        public int ResultCount { get; private set; }

        public DateTime SearchDate { get; private set; }

        [Required]
        [StringLength(50)] // IPv6 uyumlu
        public string IpAddress { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar (Relationships & FKs)

        /// <summary>
        /// Aramayı yapan kullanıcı (giriş yaptıysa, Foreign Key).
        /// </summary>
        public string? UserId { get; private set; }

        /// <summary>
        /// Navigasyon: Aramayı yapan kullanıcı.
        /// DÜZELTME: EF Core Tembel Yüklemesi (Lazy Loading) için 'virtual' eklendi.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; private set; }

        #endregion

        #region Yapıcı Metotlar (Constructors)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private SearchHistory() { }

        /// <summary>
        /// Yeni bir 'SearchHistory' log kaydı oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public SearchHistory(string searchTerm, int resultCount, string ipAddress, string? userId = null)
        {
            // Mükemmel Doğrulamalar (Zaten vardı)
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentNullException(nameof(searchTerm), "Arama terimi boş olamaz.");
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP adresi boş olamaz.");
            if (resultCount < 0)
                throw new ArgumentOutOfRangeException(nameof(resultCount), "Sonuç sayısı negatif olamaz.");

            // DÜZELTME: Boş string yerine 'null' kaydet
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId;

            SearchTerm = searchTerm;
            ResultCount = resultCount;
            IpAddress = ipAddress;
            SearchDate = DateTime.UtcNow; // Zaman damgası O AN atanır
        }

        #endregion
    }
}