using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Column] ve [Index] için eklendi
using Microsoft.EntityFrameworkCore; // [Index] attribute'u için (EF Core 5+ ise)

namespace Sattim.Web.Models.UI
{
    /// <summary>
    /// Site genelindeki ayarları (örn: "SiteTitle", "CommissionRate")
    /// bir Anahtar-Değer (Key-Value) yapısında temsil eder.
    /// 'Key' alanı benzersiz (unique) olmalıdır.
    /// </summary>
    [Index(nameof(Key), IsUnique = true)] // EF Core 5+ için benzersizliği sağlar
    public class SiteSettings
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        /// <summary>
        /// Ayarın benzersiz (unique) sistem anahtarı (örn: "CommissionRate").
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Key { get; private set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")] // Yorumunuzdaki gibi 'max' olmasını sağlar
        public string Value { get; private set; }

        [StringLength(500)]
        public string? Description { get; private set; }

        [Required]
        public SettingCategory Category { get; private set; }

        public DateTime LastUpdated { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private SiteSettings() { }

        /// <summary>
        /// Yeni bir 'SiteSettings' kaydı oluşturur ve kuralları zorunlu kılar.
        /// 'Key' ve 'Category' oluşturulduktan sonra değiştirilemez.
        /// </summary>
        public SiteSettings(string key, SettingCategory category, string value, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key), "Ayar Anahtarı (Key) boş olamaz.");

            // Alan (domain) kuralları ve atamalar için merkezi metodu kullan (DRY Prensibi)
            UpdateSetting(value, description);

            Key = key;
            Category = category;
            LastUpdated = DateTime.UtcNow; // 'UpdateSetting' yerine burada atanmalı (ilk oluşturma)
        }

        /// <summary>
        /// Ayarın değerini ve açıklamasını günceller ve doğrular.
        /// </summary>
        public void UpdateSetting(string newValue, string? newDescription = null)
        {
            if (string.IsNullOrWhiteSpace(newValue))
                throw new ArgumentNullException(nameof(newValue), "Ayar Değeri (Value) boş olamaz.");

            Value = newValue;
            Description = newDescription;
            LastUpdated = DateTime.UtcNow;
        }

        #endregion
    }

    // Enum (Değişiklik yok, gayet iyi)
    public enum SettingCategory
    {
        General,
        Payment,
        Wallet,
        Email,
        Security,
        SEO,
        Commission
    }
}