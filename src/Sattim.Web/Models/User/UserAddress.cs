using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.User
{
    public class UserAddress
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(100)]
        public string Title { get; private set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; private set; }

        [Required]
        [StringLength(500)]
        public string Address { get; private set; }

        [Required]
        [StringLength(100)]
        public string City { get; private set; }

        [Required]
        [StringLength(20)]
        public string PostalCode { get; private set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; private set; }

        public bool IsDefault { get; private set; }

        [Required]
        public string UserId { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        private UserAddress() { }

        public UserAddress(string userId, string title, string fullName, string address, string city, string postalCode, string phone)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            Update(title, fullName, address, city, postalCode, phone);

            UserId = userId;
            IsDefault = false;
        }

        public void Update(string title, string fullName, string address, string city, string postalCode, string phone)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Başlık boş olamaz.", nameof(title));
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Tam ad boş olamaz.", nameof(fullName));
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Adres boş olamaz.", nameof(address));
            if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("Şehir boş olamaz.", nameof(city));
            if (string.IsNullOrWhiteSpace(postalCode)) throw new ArgumentException("Posta kodu boş olamaz.", nameof(postalCode));
            if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Telefon boş olamaz.", nameof(phone));

            Title = title;
            FullName = fullName;
            Address = address;
            City = city;
            PostalCode = postalCode;
            Phone = phone;
        }

        public void SetAsDefault()
        {
            IsDefault = true;
        }

        public void RemoveAsDefault()
        {
            IsDefault = false;
        }
    }
}