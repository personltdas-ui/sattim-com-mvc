using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Column] ve [Index] için eklendi
using Microsoft.EntityFrameworkCore; // [Index] attribute'u için (EF Core 5+ ise)

namespace Sattim.Web.Models.UI
{
    /// <summary>
    /// E-posta bültenine abone olan bir e-posta adresini temsil eder.
    /// 'Email' alanı benzersiz (unique) olmalıdır.
    /// </summary>
    [Index(nameof(Email), IsUnique = true)] // EF Core 5+ için benzersizliği sağlar
    public class Newsletter
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        /// <summary>
        /// Abonenin benzersiz (unique) e-posta adresi.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime SubscribedDate { get; private set; }
        public DateTime? UnsubscribedDate { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Newsletter() { }

        /// <summary>
        /// Yeni bir 'Newsletter' aboneliği oluşturur ve e-postayı doğrular.
        /// </summary>
        public Newsletter(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email), "E-posta adresi boş olamaz.");
            // Not: Daha güçlü bir doğrulama için burada Regex de kullanılabilir,
            // ancak [EmailAddress] attribute'u genellikle yeterlidir.

            Email = email;
            IsActive = true;
            SubscribedDate = DateTime.UtcNow;
            UnsubscribedDate = null;
        }

        /// <summary>
        /// Abonelikten çıkarır (Mükemmel "Guard Clause" mantığı).
        /// </summary>
        public void Unsubscribe()
        {
            if (!IsActive) return; // Zaten abone değilse işlem yapma

            IsActive = false;
            UnsubscribedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Yeniden abone olur (Mükemmel "Guard Clause" mantığı).
        /// </summary>
        public void Resubscribe()
        {
            if (IsActive) return; // Zaten aboneyse işlem yapma

            IsActive = true;
            UnsubscribedDate = null;
        }

        #endregion
    }
}