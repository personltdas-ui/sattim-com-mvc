namespace Sattim.Web.ViewModels.Product
{
    public class ProductImageViewModel
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
    }
}
