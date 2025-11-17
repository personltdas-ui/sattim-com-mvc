using AutoMapper;
using Sattim.Web.Models.Analytical;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Product;
using Sattim.Web.ViewModels.Dispute;
using Sattim.Web.ViewModels.Moderation;

namespace Sattim.Web.Profiles
{
    public class ModerationProfile : Profile
    {
        public ModerationProfile()
        {
            // 1. Report (Şikayet) Eşlemesi
            CreateMap<Report, ReportViewModel>()
                .ForMember(dest => dest.ReportId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ReporterFullName, opt => opt.MapFrom(src => src.Reporter.FullName));

            // 2. Dispute (İhtilaf) Eşlemesi
            CreateMap<Dispute, DisputeViewModel>()
                .ForMember(dest => dest.DisputeId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductTitle, opt => opt.MapFrom(src => src.Product.Title))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Product.Escrow.Amount))
                .ForMember(dest => dest.BuyerFullName, opt => opt.MapFrom(src => src.Product.Escrow.Buyer.FullName))
                .ForMember(dest => dest.SellerFullName, opt => opt.MapFrom(src => src.Product.Escrow.Seller.FullName));

            // DisputeDetailViewModel, IDisputeService'teki DisputeProfile'da zaten tanımlı
            // Admin'e özel DTO'yu temel DTO'dan miras alıyoruz
            CreateMap<ViewModels.Moderation.DisputeDetailViewModel, ViewModels.Moderation.DisputeDetailViewModel>();

            // 3. Content Moderation (Yorum) Eşlemesi
            CreateMap<BlogComment, CommentModerationViewModel>()
                .ForMember(dest => dest.CommentId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AuthorFullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.BlogPostId, opt => opt.MapFrom(src => src.BlogPost.Id))
                .ForMember(dest => dest.BlogPostTitle, opt => opt.MapFrom(src => src.BlogPost.Title))
                .ForMember(dest => dest.BlogPostSlug, opt => opt.MapFrom(src => src.BlogPost.Slug));

            // 4. Content Moderation (Ürün) Eşlemesi
            CreateMap<Product, ProductModerationViewModel>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SellerFullName, opt => opt.MapFrom(src => src.Seller.FullName))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));
        }
    }
}