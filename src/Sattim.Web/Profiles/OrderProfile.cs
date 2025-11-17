using AutoMapper;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.Shipping;
using Sattim.Web.ViewModels.Order;
using System.Linq;

namespace Sattim.Web.Profiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            // Escrow -> OrderSummaryViewModel (Alıcı)
            CreateMap<Escrow, OrderSummaryViewModel>()
                .ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.ProductTitle, opt => opt.MapFrom(src => src.Product.Title))
                .ForMember(dest => dest.ProductImageUrl, opt => opt.MapFrom(src =>
                    src.Product.Images.FirstOrDefault(i => i.IsPrimary).ImageUrl ?? "/images/default.png"));

            // Escrow -> SalesSummaryViewModel (Satıcı)
            CreateMap<Escrow, SalesSummaryViewModel>()
                .ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.ProductTitle, opt => opt.MapFrom(src => src.Product.Title))
                .ForMember(dest => dest.ProductImageUrl, opt => opt.MapFrom(src =>
                    src.Product.Images.FirstOrDefault(i => i.IsPrimary).ImageUrl ?? "/images/default.png"))
                .ForMember(dest => dest.BuyerFullName, opt => opt.MapFrom(src => src.Buyer.FullName));

            // ShippingInfo (Entity) -> OrderShippingInfoViewModel (DTO)
            CreateMap<ShippingInfo, OrderShippingInfoViewModel>();

            // Escrow -> OrderDetailViewModel (Ana eşleme)
            CreateMap<Escrow, OrderDetailViewModel>()
                .ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.ProductTitle, opt => opt.MapFrom(src => src.Product.Title))
                .ForMember(dest => dest.ProductImageUrl, opt => opt.MapFrom(src =>
                    src.Product.Images.FirstOrDefault(i => i.IsPrimary).ImageUrl ?? "/images/default.png"))
                .ForMember(dest => dest.BuyerFullName, opt => opt.MapFrom(src => src.Buyer.FullName))
                .ForMember(dest => dest.SellerFullName, opt => opt.MapFrom(src => src.Seller.FullName))
                // Kargo detaylarını manuel olarak eşleştir
                .ForMember(dest => dest.ShippingDetails, opt => opt.MapFrom(src => src.Product.ShippingInfo));
        }
    }
}