using AutoMapper;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.User;
using Sattim.Web.ViewModels.Blog;
using System.Linq;

namespace Sattim.Web.Profiles
{
    public class BlogProfile : Profile
    {
        public BlogProfile()
        {
            CreateMap<BlogPost, BlogSummaryViewModel>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.FullName))
                .ForMember(dest => dest.PublishedDate, opt => opt.MapFrom(src => src.PublishedDate ?? src.CreatedDate));

            CreateMap<BlogPost, BlogPostDetailViewModel>();

            CreateMap<ApplicationUser, BlogAuthorViewModel>();

            CreateMap<BlogComment, BlogCommentViewModel>()
                .ForMember(dest => dest.AuthorFullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.AuthorProfileImageUrl, opt => opt.MapFrom(src => src.User.ProfileImageUrl));

            CreateMap<Tag, BlogTagCloudViewModel>()
                .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src =>
                    src.BlogPostTags.Count(bt => bt.BlogPost.Status == BlogPostStatus.Published)));
        }
    }
}