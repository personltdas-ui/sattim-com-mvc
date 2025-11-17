using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Column] ve [Index] için eklendi
using Microsoft.EntityFrameworkCore; // [Index] attribute'u için (EF Core 5+ ise)

namespace Sattim.Web.Models.UI
{
    /// <summary>
    /// Sistem tarafından gönderilen e-postaların (örn: "Welcome", "AuctionWon")
    /// konu ve içerik şablonlarını temsil eder.
    /// 'Name' alanı benzersiz (unique) olmalıdır.
    /// </summary>
    [Index(nameof(Name), IsUnique = true)] // EF Core 5+ için benzersizliği sağlar
    public class EmailTemplate
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        /// <summary>
        /// Şablonun benzersiz (unique) sistem adı/anahtarı (örn: "Welcome").
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; private set; }

        [Required]
        [StringLength(255)]
        public string Subject { get; private set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")] // Yorumunuzdaki gibi 'max' olmasını sağlar
        public string Body { get; private set; }

        [Required]
        public EmailTemplateType Type { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? ModifiedDate { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private EmailTemplate() { }

        /// <summary>
        /// Yeni bir 'EmailTemplate' nesnesi oluşturur ve kuralları zorunlu kılar.
        /// </summary>
        public EmailTemplate(string name, EmailTemplateType type, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "Şablon adı boş olamaz.");

            // Alan (domain) kuralları ve atamalar için merkezi metodu kullan (DRY Prensibi)
            UpdateTemplate(subject, body); // 'UpdateTemplate' doğrulamaları zaten yapıyor

            Name = name;
            Type = type;
            IsActive = true;
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = null; // 'UpdateTemplate' bunu ayarlar, 'null'a geri çekiyoruz.
        }

        /// <summary>
        /// Şablonun konusunu ve içeriğini günceller ve doğrular.
        /// </summary>
        public void UpdateTemplate(string newSubject, string newBody)
        {
            // Doğrulamalar (Mükemmel)
            if (string.IsNullOrWhiteSpace(newSubject))
                throw new ArgumentException("Konu boş olamaz.", nameof(newSubject));
            if (string.IsNullOrWhiteSpace(newBody))
                throw new ArgumentException("İçerik boş olamaz.", nameof(newBody));

            Subject = newSubject;
            Body = newBody;
            ModifiedDate = DateTime.UtcNow;
        }

        // --- Durum Metotları (Sağlamlaştırılmış) ---

        public void Activate()
        {
            // DÜZELTME: Durum zaten 'Active' ise gereksiz işlem yapma.
            if (IsActive) return;

            IsActive = true;
            ModifiedDate = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            // DÜZELTME: Durum zaten 'Deactive' ise gereksiz işlem yapma.
            if (!IsActive) return;

            IsActive = false;
            ModifiedDate = DateTime.UtcNow;
        }

        #endregion
    }

    // Enum (Değişiklik yok, gayet iyi)
    public enum EmailTemplateType
    {
        Welcome,
        BidNotification,
        BidOutbid,
        AuctionWon,
        AuctionLost,
        AuctionEnding,
        PaymentConfirmation,
        ShippingNotification,
        PasswordReset,
        EmailVerification
    }
}