using AutoMapper;
using Sattim.Web.Models.Product;
using Sattim.Web.Models.User;
using Sattim.Web.ViewModels.Category;
using Sattim.Web.ViewModels.Product;
using System.Linq;

namespace Sattim.Web.Profiles
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            string defaultImageUrl = "/images/placeholder.png";

            CreateMap<Product, ProductSummaryViewModel>()
                .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src =>
                    src.Images
                        .Where(i => i.IsPrimary)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                    ?? defaultImageUrl
                ))
                .ForMember(dest => dest.BidCount, opt => opt.MapFrom(src => src.Bids.Count));

            CreateMap<Product, ProductDetailViewModel>();
            CreateMap<ProductImage, ProductImageViewModel>();
            CreateMap<ApplicationUser, ProductSellerViewModel>();

            CreateMap<Product, ProductFormViewModel>();

            CreateMap<ProductFormViewModel, Product>();

            CreateMap<Product, UserProductViewModel>()
                 .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src =>
                    src.Images
                        .Where(i => i.IsPrimary)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                    ?? defaultImageUrl
                 ))
                 .ForMember(dest => dest.BidCount, opt => opt.MapFrom(src => src.Bids.Count));

            CreateMap<Models.Category.Category, CategoryViewModel>();
        }
    }
}