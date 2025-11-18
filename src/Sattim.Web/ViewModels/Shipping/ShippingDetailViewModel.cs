using Sattim.Web.Models.Shipping;
using System;

namespace Sattim.Web.ViewModels.Shipping
{
    public class ShippingDetailViewModel
    {
        public int ProductId { get; set; }
        public ShippingStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }

        public string FullName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }

        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
    }
}