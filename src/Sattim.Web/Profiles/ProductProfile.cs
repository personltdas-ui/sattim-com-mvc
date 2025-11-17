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

            // 1. Product -> ProductSummaryViewModel (Katalog/Arama)
            CreateMap<Product, ProductSummaryViewModel>()
                .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src =>
                    // DÜZELTME: CS8072 Hatası Çözümü
                    // 'FirstOrDefault(..)?.' yerine 'Where(..).Select(..).FirstOrDefault()' kullanıldı.
                    src.Images
                       .Where(i => i.IsPrimary)      // Önce birincil olanları filtrele
                       .Select(i => i.ImageUrl)    // Sadece URL'lerini seç
                       .FirstOrDefault()           // İlk URL'yi al (yoksa null döner)
                    ?? defaultImageUrl             // Eğer sonuç null ise varsayılanı kullan
                ))
                .ForMember(dest => dest.BidCount, opt => opt.MapFrom(src => src.Bids.Count));

            // 2. Product -> ProductDetailViewModel
            CreateMap<Product, ProductDetailViewModel>();
            CreateMap<ProductImage, ProductImageViewModel>();
            CreateMap<ApplicationUser, ProductSellerViewModel>();

            // 3. Product -> ProductFormViewModel (Form Doldurma)
            CreateMap<Product, ProductFormViewModel>();

            // 4. ProductFormViewModel -> Product (Form Gönderme)
            CreateMap<ProductFormViewModel, Product>();

            // 5. Product -> UserProductViewModel (Satıcı Paneli)
            CreateMap<Product, UserProductViewModel>()
                 .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src =>
                    // DÜZELTME: (Aynı hata burada da geçerli)
                    src.Images
                       .Where(i => i.IsPrimary)
                       .Select(i => i.ImageUrl)
                       .FirstOrDefault()
                    ?? defaultImageUrl
                 ))
                .ForMember(dest => dest.BidCount, opt => opt.MapFrom(src => src.Bids.Count));

            // 6. Category (Entity) -> CategoryViewModel (DTO)
            CreateMap<Models.Category.Category, CategoryViewModel>();
        }
    }
}