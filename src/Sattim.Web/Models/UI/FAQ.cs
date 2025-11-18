using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.UI
{
    public class FAQ
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(255)]
        public string Question { get; private set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Answer { get; private set; }

        [Required]
        public FAQCategory Category { get; private set; }

        [Range(0, 100)]
        public int DisplayOrder { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime? ModifiedDate { get; private set; }

        private FAQ() { }

        public FAQ(string question, string answer, FAQCategory category, int displayOrder = 0)
        {
            Update(question, answer, category, displayOrder);

            IsActive = true;
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = null;
        }

        public void Update(string newQuestion, string newAnswer, FAQCategory newCategory, int newDisplayOrder)
        {
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

        public void Activate()
        {
            if (IsActive) return;

            IsActive = true;
            ModifiedDate = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive) return;

            IsActive = false;
            ModifiedDate = DateTime.UtcNow;
        }
    }

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