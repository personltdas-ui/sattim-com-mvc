using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Column] için eklendi

namespace Sattim.Web.Models.UI
{
    /// <summary>
    /// Sıkça Sorulan Soruları (SSS) ve cevaplarını temsil eder.
    /// </summary>
    public class FAQ
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(255)]
        public string Question { get; private set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")] // Yorumunuzdaki gibi 'max' olmasını sağlar
        public string Answer { get; private set; }

        [Required]
        public FAQCategory Category { get; private set; }

        [Range(0, 100)]
        public int DisplayOrder { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? ModifiedDate { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private FAQ() { }

        /// <summary>
        /// Yeni bir 'FAQ' nesnesi oluşturur ve tüm iş kurallarını zorunlu kılar.
        /// </summary>
        public FAQ(string question, string answer, FAQCategory category, int displayOrder = 0)
        {
            // Alan (domain) kuralları ve atamalar için merkezi metodu kullan (DRY Prensibi)
            Update(question, answer, category, displayOrder);

            IsActive = true;
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = null; // 'Update' bunu ayarlar, 'null'a geri çekiyoruz.
        }

        /// <summary>
        /// SSS kaydını günceller ve tüm iş kurallarını zorunlu kılar.
        /// </summary>
        public void Update(string newQuestion, string newAnswer, FAQCategory newCategory, int newDisplayOrder)
        {
            // DÜZELTME: Kapsüllemeyi sağlamak için tüm doğrulamalar eklendi.
            if (string.IsNullOrWhiteSpace(newQuestion))
                throw new ArgumentException("Soru boş olamaz.", nameof(newQuestion));
            if (string.IsNullOrWhiteSpace(newAnswer))
                throw new ArgumentException("Cevap boş olamaz.", nameof(newAnswer));
            if (newDisplayOrder < 0)
                throw new ArgumentOutOfRangeException(nameof(newDisplayOrder), "Görüntüleme sırası 0'dan küçük olamaz.");

            Question = newQuestion;
            Answer = newAnswer;
            Category = newCategory;
            DisplayOrder = newDisplayOrder;
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
    public enum FAQCategory
    {
        General,
        Buying,
        Selling,
        Payment,
        Shipping,
        Account
    }
}