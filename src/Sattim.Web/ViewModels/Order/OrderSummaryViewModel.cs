using Sattim.Web.Models.Escrow; // EscrowStatus enum'u için
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Order
{
    

    /// <summary>
    /// Alıcının "Siparişlerim" sayfasındaki tek bir sipariş özetini temsil eder.
    /// (GetMyOrdersAsync tarafından döndürülür)
    /// </summary>
    public class OrderSummaryViewModel
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string ProductImageUrl { get; set; }
        public decimal FinalPrice { get; set; }
        public EscrowStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
    }

    /// <summary>
    /// Satıcının "Satışlarım" sayfasındaki tek bir satış özetini temsil eder.
    /// (GetMySalesAsync tarafından döndürülür)
    /// </summary>
    public class SalesSummaryViewModel
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string ProductImageUrl { get; set; }
        public decimal FinalPrice { get; set; }
        public EscrowStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public string BuyerFullName { get; set; } // Satıcı, alıcının adını görür
    }

    /// <summary>
    /// Alıcı ve Satıcının gördüğü ortak sipariş detay sayfası.
    /// (GetOrderDetailAsync tarafından döndürülür)
    /// </summary>
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

        
        // Controller'ın güvenlik ve UI kontrolleri için bu ID'lere ihtiyacı var.
        [Required]
        public string BuyerId { get; set; }
        [Required]
        public string SellerId { get; set; }
        

        // Kargo detayları bu alt modele doldurulur
        public OrderShippingInfoViewModel ShippingDetails { get; set; }
    }

    /// <summary>
    /// OrderDetailViewModel içinde kullanılan kargo DTO'su.
    /// </summary>
    public class OrderShippingInfoViewModel
    {
        // Adres (Tek bir string olarak)
        public string FullName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }

        // Kargo Durumu
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
    }
}