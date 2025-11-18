using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Profile
{
    public class ProfileDetailsViewModel
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(2000)]
        public string? Bio { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }
        [StringLength(100)]
        public string? City { get; set; }
        [StringLength(100)]
        public string? Country { get; set; }
        [StringLength(20)]
        public string? PostalCode { get; set; }
    }
}