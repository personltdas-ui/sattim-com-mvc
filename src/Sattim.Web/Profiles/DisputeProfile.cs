using AutoMapper;
using Sattim.Web.Models.Dispute;
using Sattim.Web.ViewModels.Dispute;
using System.Linq;

namespace Sattim.Web.Profiles
{
    public class DisputeProfile : Profile
    {
        public DisputeProfile()
        {
            // GetMyDisputesAsync için
            CreateMap<Dispute, DisputeSummaryViewModel>()
                .ForMember(dest => dest.DisputeId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductTitle, opt => opt.MapFrom(src => src.Product.Title))
                .ForMember(dest => dest.ProductImageUrl, opt => opt.MapFrom(src =>
                    src.Product.Images.FirstOrDefault(i => i.IsPrimary).ImageUrl ?? "/images/default.png"));

            // GetMyDisputeDetailsAsync için
            CreateMap<Dispute, DisputeDetailViewModel>()
                .ForMember(dest => dest.DisputeId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductTitle, opt => opt.MapFrom(src => src.Product.Title))
                .ForMember(dest => dest.BuyerId, opt => opt.MapFrom(src => src.Product.Escrow.BuyerId))
                .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.Product.Escrow.SellerId))
                .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.Messages.OrderBy(m => m.SentDate)));

            // DisputeMessage -> DisputeMessageViewModel
            CreateMap<DisputeMessage, DisputeMessageViewModel>()
                .ForMember(dest => dest.SenderFullName, opt => opt.MapFrom(src => src.Sender.FullName));
            
        }
    }
}