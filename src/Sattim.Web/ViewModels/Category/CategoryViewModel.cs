namespace Sattim.Web.ViewModels.Category
{
    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string ImageUrl { get; set; }
        public int? ParentCategoryId { get; set; }
        public List<CategoryViewModel> SubCategories { get; set; }
    }
}
