using AutoMapper;
using Microsoft.Extensions.Logging;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.ViewModels.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Blog
{
    public class BlogService : IBlogService
    {
        private readonly IBlogPostRepository _blogPostRepo;
        private readonly ITagRepository _tagRepo;
        private readonly IGenericRepository<BlogComment> _commentRepo;

        private readonly IGenericRepository<UserProfile> _profileRepo;

        private readonly IMapper _mapper;
        private readonly ILogger<BlogService> _logger;


        public BlogService(
          IBlogPostRepository blogPostRepo,
          ITagRepository tagRepo,
          IGenericRepository<BlogComment> commentRepo,
          IGenericRepository<UserProfile> profileRepo,
          IMapper mapper,
          ILogger<BlogService> logger

          )
        {
            _blogPostRepo = blogPostRepo;
            _tagRepo = tagRepo;
            _commentRepo = commentRepo;
            _profileRepo = profileRepo;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(List<BlogSummaryViewModel> Posts, int TotalPages)> GetPublishedPostsAsync(
          int page = 1, int pageSize = 10, string? tagSlug = null)
        {
            var (posts, totalCount) = await _blogPostRepo.GetPublishedPostsPaginatedAsync(page, pageSize, tagSlug);

            var viewModels = _mapper.Map<List<BlogSummaryViewModel>>(posts);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return (viewModels, totalPages);
        }

        public async Task<BlogPostDetailViewModel> GetPostBySlugAsync(string slug)
        {
            var post = await _blogPostRepo.GetPublishedPostBySlugWithDetailsAsync(slug);

            if (post == null)
            {
                _logger.LogWarning($"Blog yazısı bulunamadı: {slug}");
                throw new KeyNotFoundException("İstenen blog yazısı bulunamadı veya henüz yayınlanmadı.");
            }

            try
            {
                post.IncrementViewCount();
                _blogPostRepo.Update(post);
                await _blogPostRepo.UnitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Blog görüntülenme sayısı artırılırken hata oluştu (Slug: {slug})");
            }

            var userProfile = await _profileRepo.GetByIdAsync(post.Author.Id);

            var viewModel = _mapper.Map<BlogPostDetailViewModel>(post);

            if (userProfile != null)
            {
                viewModel.Author.Bio = userProfile.Bio;
            }

            return viewModel;
        }

        public async Task<List<BlogTagCloudViewModel>> GetTagCloudAsync()
        {
            var tags = await _tagRepo.GetTagsWithPublishedPostCountAsync();

            var viewModel = _mapper.Map<List<BlogTagCloudViewModel>>(tags);

            return viewModel.OrderByDescending(t => t.PostCount).ToList();
        }

        public async Task<(bool Success, string Slug, string ErrorMessage)> PostCommentAsync(
          PostCommentViewModel model, string userId)
        {
            try
            {
                var post = await _blogPostRepo.GetByIdAsync(model.BlogPostId);
                if (post == null || post.Status != BlogPostStatus.Published)
                {
                    _logger.LogWarning($"Geçersiz yorum denemesi. Yazı bulunamadı veya yayınlanmamış. (PostID: {model.BlogPostId})");
                    return (false, null, "Bu yazıya yorum yapılamaz.");
                }

                var comment = new BlogComment(
                  blogPostId: model.BlogPostId,
                  userId: userId,
                  content: model.Content
                );

                await _commentRepo.AddAsync(comment);
                await _commentRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Yeni yorum moderasyona eklendi (CommentID: {comment.Id}, PostID: {post.Id})");

                return (true, post.Slug, null);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Yorum gönderme BAŞARISIZ (Geçersiz argüman): {ex.Message}");
                return (false, null, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yorum gönderme sırasında kritik veritabanı hatası.");
                return (false, null, "Yorumunuz gönderilirken beklenmedik bir hata oluştu.");
            }
        }

    }
}