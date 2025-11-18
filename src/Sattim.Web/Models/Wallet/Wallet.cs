using Sattim.Web.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Wallet
{
    public class Wallet
    {
        [Key]
        public string UserId { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; private set; }

        public DateTime LastUpdated { get; private set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; private set; }

        private Wallet() { }

        public Wallet(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "Kullanıcı kimliği boş olamaz.");

            UserId = userId;
            Balance = 0m;
            LastUpdated = DateTime.UtcNow;
        }

        public void Deposit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Yatırılan tutar pozitif olmalıdır.");

            Balance += amount;
            LastUpdated = DateTime.UtcNow;
        }

        public void Withdraw(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Çekilen tutar pozitif olmalıdır.");
            if (Balance < amount)
                throw new InvalidOperationException("Yetersiz bakiye.");

            Balance -= amount;
            LastUpdated = DateTime.UtcNow;
        }
    }
}