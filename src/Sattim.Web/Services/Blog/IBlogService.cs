using Sattim.Web.Models.Blog;
using Sattim.Web.ViewModels.Blog; // Gerekli ViewModel'lar
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Blog
{
    public interface IBlogService
    {
        // ====================================================================
        //  QUERIES (Okuma İşlemleri)
        // ====================================================================

        /// <summary>
        /// Blog ana sayfasını doldurur. Sadece 'Published' (Yayınlanmış)
        /// olan yazıları sayfalanmış olarak getirir.
        /// </summary>
        /// <param name="page">Sayfa numarası</param>
        /// <param name="pageSize">Sayfa başına yazı sayısı</param>
        /// <param name="tagSlug">Opsiyonel: Etikete göre filtreleme (örn: /blog/tag/rehberler)</param>
        /// <returns>Yazıların listesi ve toplam sayfa sayısı</returns>
        Task<(List<BlogSummaryViewModel> Posts, int TotalPages)> GetPublishedPostsAsync(int page = 1, int pageSize = 10, string? tagSlug = null);

        /// <summary>
        /// Tek bir blog yazısını 'slug' (URL) üzerinden getirir.
        /// İş Mantığı:
        /// 1. Yazıyı bulur (Status == Published olmalı).
        /// 2. 'post.IncrementViewCount()' metodunu çağırır (Görüntülenme sayısını artırır).
        /// 3. Sadece 'IsApproved = true' olan yorumları getirir.
        /// </summary>
        /// <param name="slug">Yazının URL'i</param>
        /// <returns>Yazı detayları, yazarı ve onaylanmış yorumları içeren DTO</returns>
        Task<BlogPostDetailViewModel> GetPostBySlugAsync(string slug);

        /// <summary>
        /// Blog kenar çubuğu (sidebar) için "Etiket Bulutu" verisini getirir.
        /// (Etiketleri, içerdikleri yazı sayısına göre sıralar).
        /// </summary>
        Task<List<BlogTagCloudViewModel>> GetTagCloudAsync();

        // ====================================================================
        //  COMMAND (Yazma İşlemi)
        // ====================================================================

        /// <summary>
        /// Giriş yapmış kullanıcının bir yazıya yorum yapmasını sağlar.
        /// İş Mantığı:
        /// 1. 'new BlogComment(...)' constructor'ı ile yorumu oluşturur (IsApproved = false).
        /// 2. Değişiklikleri kaydeder.
        /// 3. 'INotificationService.SendCommentNeedsApprovalNotificationAsync' metodunu tetikler.
        /// </summary>
        /// <param name="model">Yorum formu verisi (BlogPostId, Content)</param>
        /// <param name="userId">Yorumu yapan kullanıcı ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, null)</returns>
        /// <summary>
        /// Giriş yapmış kullanıcının bir yazıya yorum yapmasını sağlar.
        /// </summary>
        /// <returns>Başarısızsa (false, null, "Hata Mesajı"),
        /// başarılıysa (true, "post-slug-url", null)</returns>
        Task<(bool Success, string Slug, string ErrorMessage)> PostCommentAsync(PostCommentViewModel model, string userId);


        
    }
}