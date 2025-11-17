using Sattim.Web.ViewModels.Dispute;
using Sattim.Web.ViewModels.Moderation; // Gerekli ViewModel'lar
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Moderation
{
    // BU SERVİSİN TAMAMI [Authorize(Roles = "Admin,Moderator")] İLE KORUNMALIDIR
    public interface IModerationService
    {
        // ====================================================================
        //  1. KULLANICI ŞİKAYETLERİ (Report)
        // ====================================================================

        /// <summary>
        /// Admin panelindeki "Bekleyen Şikayetler" listesini getirir.
        /// </summary>
        Task<List<ViewModels.Moderation.ReportViewModel>> GetPendingReportsAsync();

        /// <summary>
        /// Bir şikayeti 'İncelemede' olarak işaretler.
        /// </summary>
        Task<bool> MarkReportAsUnderReviewAsync(int reportId, string adminId);

        /// <summary>
        /// Bir şikayeti 'Çözüldü' olarak işaretler (örn: şikayet edilen
        /// ürün/kullanıcıya karşı aksiyon alındı).
        /// </summary>
        Task<bool> ResolveReportAsync(int reportId, string adminId, string resolutionNote);

        /// <summary>
        /// Bir şikayeti 'Reddedildi' (asılsız) olarak işaretler.
        /// </summary>
        Task<bool> RejectReportAsync(int reportId, string adminId, string rejectionNote);

        // ====================================================================
        //  2. SİPARİŞ İHTİLAFLARI (Dispute) - PARA AKIŞI
        // ====================================================================

        /// <summary>
        /// Admin panelindeki "Bekleyen İhtilaflar" (anlaşmazlıklar) listesini getirir.
        /// </summary>
        Task<List<ViewModels.Moderation.DisputeViewModel>> GetPendingDisputesAsync();

        /// <summary>
        /// Bir ihtilafın detay sayfasını (mesajları vb.) getirir.
        /// </summary>
        Task<ViewModels.Moderation.DisputeDetailViewModel> GetDisputeDetailsAsync(int disputeId);

        /// <summary>
        /// Admin/Moderatörün ihtilafa mesaj eklemesini sağlar.
        /// </summary>
        Task<bool> AddDisputeMessageAsync(int disputeId, string adminId, string message);

        /// <summary>
        /// (KRİTİK) İhtilafı SATICI lehine çözer.
        /// İş Mantığı:
        /// 1. 'dispute.Resolve(...)' metodunu çağırır.
        /// 2. 'IWalletService.ReleaseFundsToSellerAsync(...)' metodunu tetikler.
        /// </summary>
        Task<bool> ResolveDisputeForSellerAsync(int disputeId, string adminId, string resolutionNote);

        /// <summary>
        /// (KRİTİK) İhtilafı ALICI lehine çözer.
        /// İş Mantığı:
        /// 1. 'dispute.Resolve(...)' metodunu çağırır.
        /// 2. 'IWalletService.RefundEscrowToBuyerAsync(...)' metodunu tetikler.
        /// </summary>
        Task<bool> ResolveDisputeForBuyerAsync(int disputeId, string adminId, string resolutionNote);

        // ====================================================================
        //  3. İÇERİK MODERASYONU (BlogComment)
        // ====================================================================

        /// <summary>
        /// Admin panelindeki "Onay Bekleyen Yorumlar" listesini getirir.
        /// </summary>
        Task<List<CommentModerationViewModel>> GetPendingCommentsAsync();

        /// <summary>
        /// Bir blog yorumunu onaylar.
        /// İş Mantığı: 'blogComment.Approve()' metodunu çağırır.
        /// </summary>
        Task<bool> ApproveCommentAsync(int commentId, string adminId);

        /// <summary>
        /// Bir blog yorumunu reddeder/yayından kaldırır.
        /// İş Mantığı: 'blogComment.Reject()' metodunu çağırır.
        /// </summary>
        Task<bool> RejectCommentAsync(int commentId, string adminId);

        /// <summary>
        /// Admin panelindeki "Onay Bekleyen Ürünler" listesini getirir.
        /// </summary>
        Task<List<ProductModerationViewModel>> GetPendingProductsAsync();
    }
}