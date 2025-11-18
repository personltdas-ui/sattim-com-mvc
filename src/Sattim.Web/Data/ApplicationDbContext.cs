using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sattim.Web.Models.Audit;
using Sattim.Web.Models.Analytical;
using Sattim.Web.Models.Bid;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Category;
using Sattim.Web.Models.Commission;
using Sattim.Web.Models.Coupon;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.Favorite;
using Sattim.Web.Models.Message;
using Sattim.Web.Models.Payment;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.Security;
using Sattim.Web.Models.Shipping;
using Sattim.Web.Models.UI;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet;

namespace Sattim.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        #region DbSet Alanları

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PopularSearch> PopularSearches { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }

        public DbSet<Bid> Bids { get; set; }
        public DbSet<AutoBid> AutoBids { get; set; }

        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<BlogComment> BlogComments { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<BlogPostTag> BlogPostTags { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Commission> Commissions { get; set; }

        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }

        public DbSet<Dispute> Disputes { get; set; }
        public DbSet<DisputeMessage> DisputeMessages { get; set; }

        public DbSet<Escrow> Escrows { get; set; }

        public DbSet<Favorite> Favorites { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductAnalytics> ProductAnalytics { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ProductView> ProductViews { get; set; }
        public DbSet<WatchList> WatchLists { get; set; }

        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<TwoFactorAuth> TwoFactorAuths { get; set; }

        public DbSet<ShippingInfo> ShippingInfos { get; set; }

        public DbSet<Banner> Banners { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<Newsletter> Newsletters { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SiteSettings> SiteSettings { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }

        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<PayoutRequest> PayoutRequests { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region 1. Birleşik Anahtarlar

            modelBuilder.Entity<UserRating>()
                .HasKey(ur => new { ur.ProductId, ur.RaterUserId, ur.RatedUserId });

            modelBuilder.Entity<AutoBid>()
                .HasKey(ab => new { ab.UserId, ab.ProductId });

            modelBuilder.Entity<BlogPostTag>()
                .HasKey(bt => new { bt.BlogPostId, bt.TagId });

            modelBuilder.Entity<Favorite>()
                .HasKey(f => new { f.UserId, f.ProductId });

            modelBuilder.Entity<ProductReview>()
                .HasKey(pr => new { pr.ProductId, pr.ReviewerId });

            modelBuilder.Entity<WatchList>()
                .HasKey(w => new { w.UserId, w.ProductId });

            #endregion

            #region 2. Benzersiz Alanlar

            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.Slug)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            modelBuilder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique();

            modelBuilder.Entity<EmailTemplate>()
                .HasIndex(et => et.Name)
                .IsUnique();

            modelBuilder.Entity<Newsletter>()
                .HasIndex(n => n.Email)
                .IsUnique();

            modelBuilder.Entity<SiteSettings>()
                .HasIndex(s => s.Key)
                .IsUnique();

            modelBuilder.Entity<PopularSearch>()
                .HasIndex(p => p.SearchTerm)
                .IsUnique();

            #endregion

            #region 3. İlişkiler

            // --- User Domain ---
            modelBuilder.Entity<UserProfile>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithOne()
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TwoFactorAuth>()
                .HasOne(tfa => tfa.User)
                .WithOne()
                .HasForeignKey<TwoFactorAuth>(tfa => tfa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAddress>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PayoutRequest>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // --- Product Domain ---
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Seller)
                .WithMany()
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Winner)
                .WithMany()
                .HasForeignKey(p => p.WinnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product (1'e 1 İlişkiler)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Commission)
                .WithOne(c => c.Product)
                .HasForeignKey<Commission>(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Escrow)
                .WithOne(e => e.Product)
                .HasForeignKey<Escrow>(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.ShippingInfo)
                .WithOne(s => s.Product)
                .HasForeignKey<ShippingInfo>(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Analytics)
                .WithOne(a => a.Product)
                .HasForeignKey<ProductAnalytics>(a => a.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.CouponUsage)
                .WithOne(cu => cu.Product)
                .HasForeignKey<CouponUsage>(cu => cu.ProductId)
                .OnDelete(DeleteBehavior.Cascade);


            // --- Bid Domain ---
            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Bidder)
                .WithMany()
                .HasForeignKey(b => b.BidderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Product)
                .WithMany(p => p.Bids)
                .HasForeignKey(b => b.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AutoBid>()
                .HasOne(ab => ab.User)
                .WithMany()
                .HasForeignKey(ab => ab.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AutoBid>()
                .HasOne(ab => ab.Product)
                .WithMany(p => p.AutoBids)
                .HasForeignKey(ab => ab.ProductId)
                .OnDelete(DeleteBehavior.Cascade);


            // --- Finans & Ödeme Domain ---
            modelBuilder.Entity<Escrow>()
                .HasOne(e => e.Buyer)
                .WithMany()
                .HasForeignKey(e => e.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Escrow>()
                .HasOne(e => e.Seller)
                .WithMany()
                .HasForeignKey(e => e.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Escrow)
                .WithMany(e => e.Payments)
                .HasForeignKey(p => p.EscrowProductId)
                .HasPrincipalKey(e => e.ProductId);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(t => t.Wallet)
                .WithMany()
                .HasForeignKey(t => t.WalletUserId)
                .HasPrincipalKey(w => w.UserId);

            modelBuilder.Entity<PayoutRequest>()
                .HasOne(p => p.WalletTransaction)
                .WithOne()
                .HasForeignKey<PayoutRequest>(p => p.WalletTransactionId)
                .OnDelete(DeleteBehavior.SetNull);


            // --- İçerik & Moderasyon Domain ---
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Product)
                .WithMany(p => p.Favorites)
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WatchList>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WatchList>()
                .HasOne(w => w.Product)
                .WithMany(p => p.WatchLists)
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.Reviewer)
                .WithMany()
                .HasForeignKey(pr => pr.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRating>()
                .HasOne(ur => ur.RatedUser)
                .WithMany()
                .HasForeignKey(ur => ur.RatedUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserRating>()
                .HasOne(ur => ur.RaterUser)
                .WithMany()
                .HasForeignKey(ur => ur.RaterUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserRating>()
                .HasOne(ur => ur.Product)
                .WithMany()
                .HasForeignKey(ur => ur.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Dispute>()
                .HasOne(d => d.Initiator)
                .WithMany()
                .HasForeignKey(d => d.InitiatorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Dispute>()
                .HasOne(d => d.Product)
                .WithMany(p => p.Disputes)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DisputeMessage>()
                .HasOne(dm => dm.Sender)
                .WithMany()
                .HasForeignKey(dm => dm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DisputeMessage>()
                .HasOne(dm => dm.Dispute)
                .WithMany(d => d.Messages)
                .HasForeignKey(dm => dm.DisputeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Blog
            modelBuilder.Entity<BlogPost>()
                .HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BlogComment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BlogComment>()
                .HasOne(c => c.BlogPost)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<BlogPostTag>()
                .HasOne(bt => bt.BlogPost)
                .WithMany(p => p.BlogPostTags)
                .HasForeignKey(bt => bt.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<BlogPostTag>()
                .HasOne(bt => bt.Tag)
                .WithMany(t => t.BlogPostTags)
                .HasForeignKey(bt => bt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // Loglama & Analitik
            modelBuilder.Entity<AuditLog>()
                .HasOne(log => log.User)
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SecurityLog>()
                .HasOne(log => log.User)
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SearchHistory>()
                .HasOne(log => log.User)
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductView>()
                .HasOne(v => v.Product)
                .WithMany()
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductView>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            #endregion
        }
    }
}