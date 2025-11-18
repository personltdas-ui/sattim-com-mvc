using Sattim.Web.Models.Dispute;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sattim.Web.ViewModels.Dispute
{
    public class OpenDisputeViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public DisputeReason Reason { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 20)]
        public string Description { get; set; }
    }

    public class AddDisputeMessageViewModel
    {
        [Required]
        public int DisputeId { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 2)]
        public string Message { get; set; }
    }

    public class DisputeSummaryViewModel
    {
        public int DisputeId { get; set; }
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string ProductImageUrl { get; set; }
        public DisputeStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string RoleInDispute { get; set; }
    }

    public class DisputeDetailViewModel
    {
        public int DisputeId { get; set; }
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public DisputeStatus Status { get; set; }
        public DisputeReason Reason { get; set; }
        public DateTime CreatedDate { get; set; }

        public string BuyerId { get; set; }
        public string SellerId { get; set; }

        public List<DisputeMessageViewModel> Messages { get; set; } = new List<DisputeMessageViewModel>();
    }

    public class DisputeMessageViewModel
    {
        public string SenderId { get; set; }
        public string SenderFullName { get; set; }
        public string SenderProfileImageUrl { get; set; }
        public string Message { get; set; }
        public DateTime SentDate { get; set; }
    }
}