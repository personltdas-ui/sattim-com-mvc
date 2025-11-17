using AutoMapper;
using Microsoft.Extensions.Logging;
using Sattim.Web.Models.Blog; // Domain Modelleri
using Sattim.Web.Models.User; // UserProfile için eklendi
using Sattim.Web.Services.Repositories; // Özel Repolar
using Sattim.Web.ViewModels.Blog; // DTO'lar
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Blog
{
    public class BlogService : IBlogService
    {
        // Gerekli Özel Repolar
        private readonly IBlogPostRepository _blogPostRepo;
        private readonly ITagRepository _tagRepo;
        // Jenerik Repo (Yorum eklemek için)
        private readonly IGenericRepository<BlogComment> _commentRepo;

        // DÜZELTME: UserProfile'ı (Bio için) almak üzere eklendi
        private readonly IGenericRepository<UserProfile> _profileRepo;

        private readonly IMapper _mapper;
        private readonly ILogger<BlogService> _logger;
        // private readonly INotificationService _notificationService; // (Admin'e bildirim için)

        public BlogService(
            IBlogPostRepository blogPostRepo,
            ITagRepository tagRepo,
            IGenericRepository<BlogComment> commentRepo,
            IGenericRepository<UserProfile> profileRepo, // DÜZELTME: Eklendi
            IMapper mapper,
            ILogger<BlogService> logger
            //, INotificationService notificationService
            )
        {
            _blogPostRepo = blogPostRepo;
            _tagRepo = tagRepo;
            _commentRepo = commentRepo;
            _profileRepo = profileRepo; // DÜZELTME: Eklendi
            _mapper = mapper;
            _logger = logger;
            //_notificationService = notificationService;
        }

        // ====================================================================
        //  QUERIES (Okuma İşlemleri)
        // ====================================================================

        public async Task<(List<BlogSummaryViewModel> Posts, int TotalPages)> GetPublishedPostsAsync(
            int page = 1, int pageSize = 10, string? tagSlug = null)
        {
            // 1. Özel Repo'dan veriyi al
            var (posts, totalCount) = await _blogPostRepo.GetPublishedPostsPaginatedAsync(page, pageSize, tagSlug);

            // 2. AutoMapper ile DTO'ya dönüştür
            var viewModels = _mapper.Map<List<BlogSummaryViewModel>>(posts);

            // 3. Sayfa sayısını hesapla
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return (viewModels, totalPages);
        }

        public async Task<BlogPostDetailViewModel> GetPostBySlugAsync(string slug)
        {
            // 1. Repo'dan veriyi al (Artık Profile'ı İÇERMİYOR)
            // (Bu metot 'AsNoTracking' KULLANMAZ, ViewCount güncellenecek)
            var post = await _blogPostRepo.GetPublishedPostBySlugWithDetailsAsync(slug);

            if (post == null)
            {
                _logger.LogWarning($"Blog yazısı bulunamadı: {slug}");
                throw new KeyNotFoundException("İstenen blog yazısı bulunamadı veya henüz yayınlanmadı.");
            }

            // 2. İş Mantığı: Görüntülenme sayısını artır
            try
            {
                post.IncrementViewCount(); // Domain Modeli metodunu çağır
                _blogPostRepo.Update(post);
                await _blogPostRepo.UnitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Bu işlemin BAŞARISIZ OLMASI, kullanıcının yazıyı görmesini
                // ENGELLEMEMELİDİR. Bu yüzden hatayı sadece log'luyoruz.
                _logger.LogError(ex, $"Blog görüntülenme sayısı artırılırken hata oluştu (Slug: {slug})");
            }

            // 3. DÜZELTME: UserProfile'ı (Bio için) AYRICA AL
            var userProfile = await _profileRepo.GetByIdAsync(post.Author.Id);

            // 4. AutoMapper ile DTO'ya dönüştür
            var viewModel = _mapper.Map<BlogPostDetailViewModel>(post);

            // 5. 'Bio' alanını MANUEL OLARAK ata
            if (userProfile != null)
            {
                viewModel.Author.Bio = userProfile.Bio;
            }

            return viewModel;
        }

        public async Task<List<BlogTagCloudViewModel>> GetTagCloudAsync()
        {
            // 1. Özel Repo'dan veriyi al
            var tags = await _tagRepo.GetTagsWithPublishedPostCountAsync();

            // 2. AutoMapper ile DTO'ya dönüştür
            var viewModel = _mapper.Map<List<BlogTagCloudViewModel>>(tags);

            // 3. (Opsiyonel) Tekrar sırala
            return viewModel.OrderByDescending(t => t.PostCount).ToList();
        }


        // ====================================================================
        //  COMMAND (Yazma İşlemi)
        // ====================================================================

        public async Task<(bool Success, string Slug, string ErrorMessage)> PostCommentAsync(
            PostCommentViewModel model, string userId)
        {
            try
            {
                // 1. İş Kuralı: Yorum yapılan yazı mevcut mu ve yayınlanmış mı?
                var post = await _blogPostRepo.GetByIdAsync(model.BlogPostId);
                if (post == null || post.Status != BlogPostStatus.Published)
                {
                    _logger.LogWarning($"Geçersiz yorum denemesi. Yazı bulunamadı veya yayınlanmamış. (PostID: {model.BlogPostId})");
                    return (false, null, "Bu yazıya yorum yapılamaz.");
                }

                // 2. İş Mantığını Modele Devret
                // (Constructor tüm doğrulamaları yapar: content != null vb.)
                var comment = new BlogComment(
                    blogPostId: model.BlogPostId,
                    userId: userId,
                    content: model.Content
                ); // (Durumu otomatik olarak IsApproved = false ayarlanır)

                // 3. Veritabanına Ekle
                await _commentRepo.AddAsync(comment);
                await _commentRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Yeni yorum moderasyona eklendi (CommentID: {comment.Id}, PostID: {post.Id})");

                // 4. (Opsiyonel) Admin'e bildirim gönder
                // await _notificationService.SendCommentNeedsApprovalNotificationAsync(comment);

                // 5. Başarılı: Controller'ı doğru yazıya yönlendirmek için 'Slug'ı döndür.
                return (true, post.Slug, null);
            }
            catch (ArgumentException ex) // Modelin constructor'ından gelen hata
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