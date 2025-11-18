using AutoMapper;
using Sattim.Web.Models.Bid;
using Sattim.Web.Models.Product;
using Sattim.Web.ViewModels.Bid;
using System.Linq;

namespace Sattim.Web.Profiles
{
    public class BidProfile : Profile
    {
        public BidProfile()
        {
            CreateMap<Product, ProductBidHistoryViewModel>()
                .ForMember(dest => dest.BidCount, opt => opt.MapFrom(src => src.Bids.Count))
                .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src =>
                    src.Images.FirstOrDefault(i => i.IsPrimary).ImageUrl ?? "/images/default.png"));

            CreateMap<Bid, BidHistoryItemViewModel>()
                .ForMember(dest => dest.BidderFullName, opt => opt.MapFrom(src =>
                    src.Bidder.FullName));

            CreateMap<AutoBid, AutoBidSettingViewModel>();

            CreateMap<Product, UserBidItemViewModel>()
                .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src =>
                    src.Images.FirstOrDefault(i => i.IsPrimary).ImageUrl ?? "/images/default.png"));
        }
    }
}