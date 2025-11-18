using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Sattim.Web.ViewModels.Category;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Product
{
    public class ProductFormViewModel
    {
        public int Id { get; set; }
        [Required] public string Title { get; set; }
        [Required] public string Description { get; set; }
        [Required] public decimal StartingPrice { get; set; }
        [Required] public decimal BidIncrement { get; set; }
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
        [Required] public int CategoryId { get; set; }
        public decimal? ReservePrice { get; set; }

        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
        public List<ProductImageViewModel> Images { get; set; } = new List<ProductImageViewModel>();
    }
}