namespace Sattim.Web.ViewModels.Product
{
    public class ProductFilterViewModel
    {
        public string? Query { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public ProductSortOrder SortBy { get; set; } = ProductSortOrder.Newest;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public enum ProductSortOrder
    {
        Newest,
        EndingSoon,
        PriceAsc,
        PriceDesc
    }
}