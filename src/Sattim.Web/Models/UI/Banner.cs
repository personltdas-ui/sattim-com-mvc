using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.UI
{
    public class Banner
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(150)]
        public string Title { get; private set; }

        [Required]
        [StringLength(1000)]
        public string ImageUrl { get; private set; }

        [StringLength(1000)]
        public string? LinkUrl { get; private set; }

        [Required]
        public BannerPosition Position { get; private set; }

        [Range(0, 100)]
        public int DisplayOrder { get; private set; }

        [Required]
        public DateTime StartDate { get; private set; }

        [Required]
        public DateTime EndDate { get; private set; }

        public bool IsActive { get; private set; }

        [Range(0, int.MaxValue)]
        public int ClickCount { get; private set; }

        [Range(0, int.MaxValue)]
        public int ViewCount { get; private set; }

        private Banner() { }

        public Banner(string title, string imageUrl, BannerPosition position, DateTime startDate, DateTime endDate, string? linkUrl = null, int displayOrder = 0)
        {
            SetValues(title, imageUrl, position, startDate, endDate, linkUrl, displayOrder);

            IsActive = true;
            ClickCount = 0;
            ViewCount = 0;
        }

        public void UpdateDetails(string newTitle, string newImageUrl, BannerPosition newPosition, DateTime newStartDate, DateTime newEndDate, string? newLinkUrl, int newDisplayOrder)
        {
            SetValues(newTitle, newImageUrl, newPosition, newStartDate, newEndDate, newLinkUrl, newDisplayOrder);
        }

        private void SetValues(string title, string imageUrl, BannerPosition position, DateTime startDate, DateTime endDate, string? linkUrl, int displayOrder)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Başlık boş olamaz.", nameof(title));
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Resim URL'i boş olamaz.", nameof(imageUrl));

            if (endDate <= startDate)
                throw new ArgumentException("Bitiş tarihi, başlangıç tarihinden sonra olmalıdır.");
            if (displayOrder < 0)
                throw new ArgumentOutOfRangeException(nameof(displayOrder), "Görüntüleme sırası 0'dan küçük olamaz.");

            Title = title;
            ImageUrl = imageUrl;
            Position = position;
            StartDate = startDate;
            EndDate = endDate;
            LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl;
            DisplayOrder = displayOrder;
        }

        public void IncrementView()
        {
            ViewCount++;
        }

        public void IncrementClick()
        {
            ClickCount++;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
        }
    }

    public enum BannerPosition
    {
        HomePage_Top,
        HomePage_Sidebar,
        HomePage_Bottom,
        ProductList_Top,
        ProductDetail_Sidebar
    }
}