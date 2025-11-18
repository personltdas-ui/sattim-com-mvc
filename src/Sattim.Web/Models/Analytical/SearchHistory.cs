using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Analytical
{
    public class SearchHistory
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(255)]
        public string SearchTerm { get; private set; }

        [Range(0, int.MaxValue)]
        public int ResultCount { get; private set; }

        public DateTime SearchDate { get; private set; }

        [Required]
        [StringLength(50)]
        public string IpAddress { get; private set; }

        #endregion

        #region İlişkiler ve Yabancı Anahtarlar

        public string? UserId { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; private set; }

        #endregion

        #region Yapıcı Metotlar

        private SearchHistory() { }

        public SearchHistory(string searchTerm, int resultCount, string ipAddress, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentNullException(nameof(searchTerm), "Arama terimi boş olamaz.");
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP adresi boş olamaz.");
            if (resultCount < 0)
                throw new ArgumentOutOfRangeException(nameof(resultCount), "Sonuç sayısı negatif olamaz.");

            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId;

            SearchTerm = searchTerm;
            ResultCount = resultCount;
            IpAddress = ipAddress;
            SearchDate = DateTime.UtcNow;
        }

        #endregion
    }
}