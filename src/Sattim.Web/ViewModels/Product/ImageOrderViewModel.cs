namespace Sattim.Web.ViewModels.Product
{
    /// <summary>
    /// Resim sırasını güncellemek için DTO.
    /// </summary>
    public class ImageOrderViewModel
    {
        public int ImageId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }
    }
}
