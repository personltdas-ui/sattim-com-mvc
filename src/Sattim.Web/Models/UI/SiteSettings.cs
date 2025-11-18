using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Sattim.Web.Models.UI
{
    [Index(nameof(Key), IsUnique = true)]
    public class SiteSettings
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(100)]
        public string Key { get; private set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Value { get; private set; }

        [StringLength(500)]
        public string? Description { get; private set; }

        [Required]
        public SettingCategory Category { get; private set; }

        public DateTime LastUpdated { get; private set; }

        private SiteSettings() { }

        public SiteSettings(string key, SettingCategory category, string value, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key), "Ayar Anahtarı (Key) boş olamaz.");

            UpdateSetting(value, description);

            Key = key;
            Category = category;
            LastUpdated = DateTime.UtcNow;
        }

        public void UpdateSetting(string newValue, string? newDescription = null)
        {
            if (string.IsNullOrWhiteSpace(newValue))
                throw new ArgumentNullException(nameof(newValue), "Ayar Değeri (Value) boş olamaz.");

            Value = newValue;
            Description = newDescription;
            LastUpdated = DateTime.UtcNow;
        }
    }

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