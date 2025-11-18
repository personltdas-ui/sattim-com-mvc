using Sattim.Web.Models.Escrow;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace Sattim.Web.Models.Payment
{
    public class Payment
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        public int EscrowProductId { get; private set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; private set; }

        public PaymentMethod Method { get; private set; }

        public PaymentStatus Status { get; private set; }

        [StringLength(255)]
        public string? TransactionId { get; private set; }

        [StringLength(4000)]
        public string? PaymentGatewayResponse { get; private set; }

        public DateTime CreatedDate { get; private set; }
        public DateTime? PaidDate { get; private set; }
        public DateTime? RefundedDate { get; private set; }

        public virtual Escrow.Escrow Escrow { get; private set; }


        private Payment() { }

        public Payment(int escrowProductId, decimal amount, PaymentMethod method)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Tutar pozitif olmalıdır.");

            EscrowProductId = escrowProductId;
            Amount = amount;
            Method = method;
            Status = PaymentStatus.Pending;
            CreatedDate = DateTime.UtcNow;
        }

        public void StartProcessing()
        {
            if (Status == PaymentStatus.Pending)
                Status = PaymentStatus.Processing;
        }

        public void Complete(string transactionId, string? gatewayResponse = null)
        {
            if (Status == PaymentStatus.Processing)
            {
                Status = PaymentStatus.Completed;
                TransactionId = transactionId;
                PaymentGatewayResponse = gatewayResponse;
                PaidDate = DateTime.UtcNow;
            }
        }

        public void Fail(string? gatewayResponse = null)
        {
            if (Status == PaymentStatus.Pending || Status == PaymentStatus.Processing)
            {
                Status = PaymentStatus.Failed;
                PaymentGatewayResponse = gatewayResponse;
            }
        }

        public void MarkAsRefunded(string refundTransactionId, string? gatewayResponse = null)
        {
            if (Status == PaymentStatus.Completed)
            {
                Status = PaymentStatus.Refunded;
                TransactionId = refundTransactionId;
                PaymentGatewayResponse = gatewayResponse;
                RefundedDate = DateTime.UtcNow;
            }
        }
    }

    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        BankTransfer,
        PayPal,
        Stripe,
        Iyzico,
        Wallet
    }
    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Refunded,
        Cancelled
    }
}