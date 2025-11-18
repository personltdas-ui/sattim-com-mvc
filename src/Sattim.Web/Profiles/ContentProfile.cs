using AutoMapper;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Category;
using Sattim.Web.Models.UI;
using Sattim.Web.ViewModels.Category;
using Sattim.Web.ViewModels.Content;
using System.Linq;

namespace Sattim.Web.Profiles
{
    public class ContentProfile : Profile
    {
        public ContentProfile()
        {
            CreateMap<SiteSettings, SiteSettingUpdateViewModel>().ReverseMap();

            CreateMap<Category, CategoryViewModel>();
            CreateMap<CategoryFormViewModel, Category>();
            CreateMap<Category, CategoryFormViewModel>();

            CreateMap<BlogPost, BlogPostSummaryViewModel>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.FullName));

            CreateMap<BlogPostFormViewModel, BlogPost>();
            CreateMap<BlogPost, BlogPostFormViewModel>()
                .ForMember(dest => dest.CommaSeparatedTags, opt => opt.MapFrom(src =>
                    string.Join(",", src.BlogPostTags.Select(bt => bt.Tag.Name))));

            CreateMap<Tag, TagViewModel>()
                .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.BlogPostTags.Count));
            CreateMap<TagViewModel, Tag>();

            CreateMap<FaqFormViewModel, FAQ>();
            CreateMap<FAQ, FaqFormViewModel>();

            CreateMap<BannerFormViewModel, Banner>();
            CreateMap<Banner, BannerFormViewModel>();

            CreateMap<EmailTemplateFormViewModel, EmailTemplate>();
            CreateMap<EmailTemplate, EmailTemplateFormViewModel>();
        }
    }
}