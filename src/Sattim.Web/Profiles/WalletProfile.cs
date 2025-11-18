using AutoMapper;
using Sattim.Web.Models.Wallet;
using Sattim.Web.ViewModels.Wallet;

namespace Sattim.Web.Profiles
{
    public class WalletProfile : Profile
    {
        public WalletProfile()
        {
            CreateMap<PayoutRequest, PayoutHistoryViewModel>()
                .ForMember(dest => dest.IBAN, opt => opt.MapFrom(src =>
                    src.IBAN.Length > 8 ? src.IBAN.Substring(0, 4) + "......." + src.IBAN.Substring(src.IBAN.Length - 4) : src.IBAN
                ));

            CreateMap<WalletTransaction, WalletTransactionViewModel>();
        }
    }
}