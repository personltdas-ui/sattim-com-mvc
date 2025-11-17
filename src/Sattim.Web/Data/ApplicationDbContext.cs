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

namespace Sattim.Web.Data // (Veya sizin DbContext namespace'iniz ne ise)
{
    /// <summary>
    /// Uygulamanın ana veritabanı bağlamı. 
    /// Identity ve tüm alan (domain) varlıklarını yönetir.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        #region DbSet Alanları (Tüm Varlıklar)

        // Analytical
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PopularSearch> PopularSearches { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }

        // Bid
        public DbSet<Bid> Bids { get; set; }
        public DbSet<AutoBid> AutoBids { get; set; }

        // Blog
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<BlogComment> BlogComments { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<BlogPostTag> BlogPostTags { get; set; }

        // Category
        public DbSet<Category> Categories { get; set; }

        // Commission
        public DbSet<Commission> Commissions { get; set; }

        // Coupon
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }

        // Dispute
        public DbSet<Dispute> Disputes { get; set; }
        public DbSet<DisputeMessage> DisputeMessages { get; set; }

        // Escrow
        public DbSet<Escrow> Escrows { get; set; }

        // Favorite
        public DbSet<Favorite> Favorites { get; set; }

        // Message
        public DbSet<Message> Messages { get; set; }

        // Payment
        public DbSet<Payment> Payments { get; set; }

        // Product (Aggregate Root ve Alt Varlıkları)
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductAnalytics> ProductAnalytics { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ProductView> ProductViews { get; set; }
        public DbSet<WatchList> WatchLists { get; set; }

        // Security
        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<TwoFactorAuth> TwoFactorAuths { get; set; }

        // Shipping
        public DbSet<ShippingInfo> ShippingInfos { get; set; }

        // UI
        public DbSet<Banner> Banners { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<Newsletter> Newsletters { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SiteSettings> SiteSettings { get; set; }

        // User
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }

        // Wallet
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<PayoutRequest> PayoutRequests { get; set; }

        #endregion

        /// <summary>
        /// Tüm varlık ilişkileri, anahtarlar, benzersiz kısıtlamalar
        /// ve silme davranışları burada (Fluent API ile) tanımlanır.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Identity tabloları için (örn: AspNetUsers)
            base.OnModelCreating(modelBuilder);

            // Not: Decimal (para) alanlarının [Column(TypeName="decimal(18,2)")]
            // attribute'u ile modeller üzerinde zaten tanımlandığını varsayıyoruz.

            #region 1. Birleşik Anahtarlar (Composite Keys)
            // (Bir kullanıcının bir ürünü sadece bir kez favorilemesi/oylaması/izlemesi vb.)

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

            #region 2. Benzersiz Alanlar (Unique Indices)
            // (Aynı e-posta/kod/isim ile birden fazla kayıt olmasını engeller)

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

            #region 3. İlişkiler (Relationships)

            // --- User Domain (ApplicationUser'a bağlı 1-1 ve 1-Çok) ---
            // (ApplicationUser'da koleksiyon OLMADIĞI için .WithMany() boştur)

            // User -> UserProfile (1'e 1)
            modelBuilder.Entity<UserProfile>()
                .HasOne(p => p.User)
                .WithOne() // ApplicationUser'da navigasyon yok
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Wallet (1'e 1)
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithOne() // ApplicationUser'da navigasyon yok
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> TwoFactorAuth (1'e 1)
            modelBuilder.Entity<TwoFactorAuth>()
                .HasOne(tfa => tfa.User)
                .WithOne() // ApplicationUser'da navigasyon yok
                .HasForeignKey<TwoFactorAuth>(tfa => tfa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> UserAddress (1'e Çok)
            modelBuilder.Entity<UserAddress>()
                .HasOne(a => a.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Notification (1'e Çok)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> PayoutRequest (1'e Çok)
            modelBuilder.Entity<PayoutRequest>()
                .HasOne(p => p.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // --- Product Domain (Product Aggregate Root ve ilişkileri) ---

            // Product -> Seller (Satıcı) (1'e Çok)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Seller)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product -> Winner (Kazanan) (1'e Çok)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Winner)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(p => p.WinnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product -> Category (1'e Çok)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products) // Category'deki ICollection<Product>
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Kategori silinirse ürünler silinmesin

            // Category (Self-referencing 1'e Çok)
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product -> ProductImage (1'e Çok)
            modelBuilder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images) // Product'taki ICollection<ProductImage>
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Product (1'e 1 İlişkiler) ---
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


            // --- Bid Domain (Product ve User'a bağlanır) ---
            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Bidder)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(b => b.BidderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Product)
                .WithMany(p => p.Bids) // Product'taki ICollection<Bid>
                .HasForeignKey(b => b.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AutoBid>() // Composite Key (bkz: Bölüm 1)
                .HasOne(ab => ab.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(ab => ab.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AutoBid>()
                .HasOne(ab => ab.Product)
                .WithMany(p => p.AutoBids) // Product'taki ICollection<AutoBid>
                .HasForeignKey(ab => ab.ProductId)
                .OnDelete(DeleteBehavior.Cascade);


            // --- Finans & Ödeme Domain (Escrow, Payment, Wallet) ---
            modelBuilder.Entity<Escrow>()
                .HasOne(e => e.Buyer)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(e => e.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Escrow>()
                .HasOne(e => e.Seller)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(e => e.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Escrow -> Payment (1'e Çok) (ÖZEL KURAL: Escrow.PK = ProductId)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Escrow)
                .WithMany(e => e.Payments) // Escrow'daki ICollection<Payment>
                .HasForeignKey(p => p.EscrowProductId) // Payment'taki FK
                .HasPrincipalKey(e => e.ProductId); // Escrow'daki PK

            // Wallet -> WalletTransaction (1'e Çok) (ÖZEL KURAL: Wallet.PK = UserId)
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(t => t.Wallet)
                .WithMany() // Wallet'ta navigasyon yok
                .HasForeignKey(t => t.WalletUserId) // Transaction'daki FK
                .HasPrincipalKey(w => w.UserId); // Wallet'taki PK

            // PayoutRequest -> WalletTransaction (İsteğe bağlı 1'e 1)
            modelBuilder.Entity<PayoutRequest>()
                .HasOne(p => p.WalletTransaction)
                .WithOne() // WalletTransaction'da navigasyon yok
                .HasForeignKey<PayoutRequest>(p => p.WalletTransactionId)
                .OnDelete(DeleteBehavior.SetNull);


            // --- İçerik & Moderasyon Domain (Blog, Message, Favorite, Dispute, Report) ---

            // Message (Gönderen ve Alıcı)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Favorite (Composite Key)
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Product)
                .WithMany(p => p.Favorites) // Product'taki ICollection<Favorite>
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // WatchList (Composite Key)
            modelBuilder.Entity<WatchList>()
                .HasOne(w => w.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WatchList>()
                .HasOne(w => w.Product)
                .WithMany(p => p.WatchLists) // Product'taki ICollection<WatchList>
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductReview (Composite Key)
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.Reviewer)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(pr => pr.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Reviews) // Product'taki ICollection<ProductReview>
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserRating (Composite Key)
            modelBuilder.Entity<UserRating>()
                .HasOne(ur => ur.RatedUser)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(ur => ur.RatedUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserRating>()
                .HasOne(ur => ur.RaterUser)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(ur => ur.RaterUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserRating>()
                .HasOne(ur => ur.Product)
                .WithMany() // Product'ta navigasyon yok
                .HasForeignKey(ur => ur.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Dispute & DisputeMessage
            modelBuilder.Entity<Dispute>()
                .HasOne(d => d.Initiator)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(d => d.InitiatorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Dispute>()
                .HasOne(d => d.Product)
                .WithMany(p => p.Disputes) // Product'taki ICollection<Dispute>
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DisputeMessage>()
                .HasOne(dm => dm.Sender)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(dm => dm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DisputeMessage>()
                .HasOne(dm => dm.Dispute)
                .WithMany(d => d.Messages) // Dispute'taki ICollection<DisputeMessage>
                .HasForeignKey(dm => dm.DisputeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Report
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Blog (BlogPost, BlogComment, BlogPostTag)
            modelBuilder.Entity<BlogPost>()
                .HasOne(p => p.Author)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BlogComment>()
                .HasOne(c => c.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BlogComment>()
                .HasOne(c => c.BlogPost)
                .WithMany(p => p.Comments) // BlogPost'taki ICollection<BlogComment>
                .HasForeignKey(c => c.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<BlogPostTag>() // Composite Key
                .HasOne(bt => bt.BlogPost)
                .WithMany(p => p.BlogPostTags) // BlogPost'taki ICollection<BlogPostTag>
                .HasForeignKey(bt => bt.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<BlogPostTag>()
                .HasOne(bt => bt.Tag)
                .WithMany(t => t.BlogPostTags) // Tag'deki ICollection<BlogPostTag>
                .HasForeignKey(bt => bt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Loglama & Analitik (Silme davranışları 'SetNull') ---
            modelBuilder.Entity<AuditLog>()
                .HasOne(log => log.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Kullanıcı silinirse kayıtlar kalsın

            modelBuilder.Entity<SecurityLog>()
                .HasOne(log => log.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Kullanıcı silinirse kayıtlar kalsın

            modelBuilder.Entity<SearchHistory>()
                .HasOne(log => log.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Kullanıcı silinirse kayıtlar kalsın

            modelBuilder.Entity<ProductView>()
                .HasOne(v => v.Product)
                .WithMany() // Product'ta navigasyon yok
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductView>()
                .HasOne(v => v.User)
                .WithMany() // ApplicationUser'da navigasyon yok
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Kullanıcı silinirse kayıtlar kalsın

            #endregion
        }
    }
}