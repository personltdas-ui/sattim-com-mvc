using Sattim.Web.Models.Shipping;

namespace Sattim.Web.ViewModels.Shipping
{
    /// <summary>
    /// Bir siparişin kargo durumunu ve adresini gösteren DTO.
    /// (GetShippingDetailsAsync tarafından döndürülür)
    /// </summary>
    public class ShippingDetailViewModel
    {
        public int ProductId { get; set; }
        public ShippingStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }

        // Adres Bilgileri
        public string FullName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }

        // Kargo Bilgileri
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
    }
}
