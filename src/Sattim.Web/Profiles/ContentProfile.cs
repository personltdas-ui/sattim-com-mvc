using AutoMapper;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Category;
using Sattim.Web.Models.UI;
using Sattim.Web.ViewModels.Category; // Gerekli
using Sattim.Web.ViewModels.Content; // Gerekli
using System.Linq;

namespace Sattim.Web.Profiles
{
    public class ContentProfile : Profile
    {
        public ContentProfile()
        {
            // 1. Site Ayarları
            CreateMap<SiteSettings, SiteSettingUpdateViewModel>().ReverseMap();

            // 2. Kategori
            CreateMap<Category, CategoryViewModel>(); // Okuma
            CreateMap<CategoryFormViewModel, Category>(); // Yazma (Create)
            CreateMap<Category, CategoryFormViewModel>(); // Okuma (Edit Formu Doldurma)

            // 3. Blog
            CreateMap<BlogPost, BlogPostSummaryViewModel>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.FullName));

            CreateMap<BlogPostFormViewModel, BlogPost>(); // Yazma (Create)
            CreateMap<BlogPost, BlogPostFormViewModel>() // Okuma (Edit Formu Doldurma)
                .ForMember(dest => dest.CommaSeparatedTags, opt => opt.MapFrom(src =>
                    string.Join(",", src.BlogPostTags.Select(bt => bt.Tag.Name))));

            CreateMap<Tag, TagViewModel>()
                .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.BlogPostTags.Count));
            CreateMap<TagViewModel, Tag>(); // Yazma (Create)

            // 4. UI (FAQ, Banner)
            CreateMap<FaqFormViewModel, FAQ>();
            CreateMap<FAQ, FaqFormViewModel>();

            CreateMap<BannerFormViewModel, Banner>();
            CreateMap<Banner, BannerFormViewModel>();

            // 5. Email Template
            CreateMap<EmailTemplateFormViewModel, EmailTemplate>();
            CreateMap<EmailTemplate, EmailTemplateFormViewModel>();
        }
    }
}