using Sattim.Web.Models.Escrow;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Order
{
    public class OrderSummaryViewModel
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string ProductImageUrl { get; set; }
        public decimal FinalPrice { get; set; }
        public EscrowStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class SalesSummaryViewModel
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string ProductImageUrl { get; set; }
        public decimal FinalPrice { get; set; }
        public EscrowStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public string BuyerFullName { get; set; }
    }

    public class OrderDetailViewModel
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string ProductImageUrl { get; set; }

        public DateTime OrderDate { get; set; }
        public decimal FinalPrice { get; set; }
        public EscrowStatus Status { get; set; }

        public string BuyerFullName { get; set; }
        public string SellerFullName { get; set; }

        [Required]
        public string BuyerId { get; set; }
        [Required]
        public string SellerId { get; set; }

        public OrderShippingInfoViewModel ShippingDetails { get; set; }
    }

    public class OrderShippingInfoViewModel
    {
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