using Sattim.Web.Models.Product;

using System;
using System.Collections.Generic;

namespace Sattim.Web.ViewModels.Product
{
    public class ProductDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal StartingPrice { get; set; }
        public decimal? ReservePrice { get; set; }
        public decimal BidIncrement { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ProductStatus Status { get; set; }

        public ProductSellerViewModel Seller { get; set; }
        public List<ProductImageViewModel> Images { get; set; } = new List<ProductImageViewModel>();
    }
}