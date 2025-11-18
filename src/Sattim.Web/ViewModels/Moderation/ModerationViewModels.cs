using Sattim.Web.Models.Analytical;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Product;
using Sattim.Web.ViewModels.Dispute;
using System;
using System.Collections.Generic;

namespace Sattim.Web.ViewModels.Moderation
{
    public class ReportViewModel
    {
        public int ReportId { get; set; }
        public string ReporterFullName { get; set; }
        public ReportEntityType EntityType { get; set; }
        public string EntityId { get; set; }
        public ReportReason Reason { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public ReportStatus Status { get; set; }
    }

    public class DisputeViewModel
    {
        public int DisputeId { get; set; }
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string BuyerFullName { get; set; }
        public string SellerFullName { get; set; }
        public decimal Amount { get; set; }
        public DisputeStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class DisputeDetailViewModel : Sattim.Web.ViewModels.Dispute.DisputeDetailViewModel
    {
        public string BuyerIpAddress { get; set; }
        public string SellerIpAddress { get; set; }
    }

    public class CommentModerationViewModel
    {
        public int CommentId { get; set; }
        public string AuthorFullName { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }

        public int BlogPostId { get; set; }
        public string BlogPostTitle { get; set; }
        public string BlogPostSlug { get; set; }
    }

    public class ProductModerationViewModel
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string SellerFullName { get; set; }
        public string CategoryName { get; set; }
        public decimal StartingPrice { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}