using AutoMapper;
using Sattim.Web.Models.Bid;
using Sattim.Web.Models.Product;
using Sattim.Web.ViewModels.Bid;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sattim.Web.Profiles
{
    public class BidProfile : Profile
    {
        public BidProfile()
        {
            // Query: GetProductBidHistoryAsync (Product -> ViewModel)
            CreateMap<Product, ProductBidHistoryViewModel>()
                .ForMember(dest => dest.BidCount, opt => opt.MapFrom(src => src.Bids.Count))
                .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src =>
                    src.Images.FirstOrDefault(i => i.IsPrimary).ImageUrl ?? "/images/default.png"));

            // Query: GetProductBidHistoryAsync (Bid -> ViewModel)
            CreateMap<Bid, BidHistoryItemViewModel>()
                .ForMember(dest => dest.BidderFullName, opt => opt.MapFrom(src =>
                    src.Bidder.FullName)); // Veya gizlilik için "A*** B***"

            // Query: GetUserAutoBidSettingAsync (AutoBid -> ViewModel)
            CreateMap<AutoBid, AutoBidSettingViewModel>();

            // Query: GetUserBidsAsync (Product -> ViewModel)
            // (Bu karmaşık olduğu için serviste manuel olarak eşlenecek,
            // ama temel eşlemeyi (mapping) buradan yapabiliriz)
            CreateMap<Product, UserBidItemViewModel>()
                .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src =>
                    src.Images.FirstOrDefault(i => i.IsPrimary).ImageUrl ?? "/images/default.png"));
        }
    }
}