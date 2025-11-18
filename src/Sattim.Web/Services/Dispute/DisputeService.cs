using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Analytical;
using Sattim.Web.Models.Dispute;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.User;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.Services.Notification;
using Sattim.Web.ViewModels.Dispute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Dispute
{
    public class DisputeService : IDisputeService
    {
        private readonly IDisputeRepository _disputeRepo;
        private readonly IGenericRepository<DisputeMessage> _messageRepo;
        private readonly IGenericRepository<Escrow> _escrowRepo;

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<DisputeService> _logger;
        private readonly INotificationService _notificationService;

        public DisputeService(
          IDisputeRepository disputeRepo,
          IGenericRepository<DisputeMessage> messageRepo,
          IGenericRepository<Escrow> escrowRepo,
          ApplicationDbContext context,
          IMapper mapper,
          ILogger<DisputeService> logger,
          INotificationService notificationService
          )
        {
            _disputeRepo = disputeRepo;
            _messageRepo = messageRepo;
            _escrowRepo = escrowRepo;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<List<DisputeSummaryViewModel>> GetMyDisputesAsync(string userId)
        {
            var disputes = await _disputeRepo.GetDisputesForUserAsync(userId);

            var viewModels = _mapper.Map<List<DisputeSummaryViewModel>>(disputes);

            foreach (var vm in viewModels)
            {
                var originalDispute = disputes.First(d => d.Id == vm.DisputeId);
                vm.RoleInDispute = (originalDispute.Product.Escrow.BuyerId == userId) ? "Alıcı" : "Satıcı";
            }

            return viewModels;
        }

        public async Task<DisputeDetailViewModel> GetMyDisputeDetailsAsync(int disputeId, string userId)
        {
            var dispute = await _disputeRepo.GetDisputeWithDetailsAsync(disputeId);

            if (dispute == null)
                throw new KeyNotFoundException("İhtilaf bulunamadı.");

            if (dispute.Product.Escrow.BuyerId != userId && dispute.Product.Escrow.SellerId != userId)
            {
                _logger.LogWarning($"Yetkisiz ihtilaf detayı erişim denemesi. Kullanıcı: {userId}, İhtilaf: {disputeId}");
                throw new UnauthorizedAccessException("Bu ihtilafı görüntüleme yetkiniz yok.");
            }

            var viewModel = _mapper.Map<DisputeDetailViewModel>(dispute);

            var senderIds = viewModel.Messages.Select(m => m.SenderId).Distinct().ToList();

            if (senderIds.Any())
            {
                var userImageDictionary = (await _context.Users
                            .Where(u => senderIds.Contains(u.Id))
                            .Select(u => new { u.Id, u.ProfileImageUrl })
                            .ToListAsync())
                            .ToDictionary(u => u.Id, u => u.ProfileImageUrl);

                foreach (var messageVM in viewModel.Messages)
                {
                    if (userImageDictionary.TryGetValue(messageVM.SenderId, out var imageUrl))
                    {
                        messageVM.SenderProfileImageUrl = imageUrl;
                    }
                }
            }

            return viewModel;
        }

        public async Task<(bool Success, int? DisputeId, string ErrorMessage)> OpenDisputeAsync(OpenDisputeViewModel model, string buyerId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"İhtilaf açma işlemi (Transaction) başlatıldı: Alıcı: {buyerId}, Ürün: {model.ProductId}");

            try
            {
                var escrow = await _escrowRepo.GetByIdAsync(model.ProductId);

                if (escrow == null)
                    return (false, null, "Bu ihtilafın ilgili olduğu sipariş (Escrow) kaydı bulunamadı.");

                if (escrow.BuyerId != buyerId)
                    return (false, null, "Sadece kendi siparişiniz için ihtilaf açabilirsiniz.");

                if (escrow.Status != EscrowStatus.Funded && escrow.Status != EscrowStatus.Shipped)
                    return (false, null, "Sadece 'Ödendi' veya 'Kargolandı' durumundaki siparişler için ihtilaf açılabilir.");

                if (await _disputeRepo.AnyAsync(d => d.ProductId == model.ProductId &&
                  (d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview)))
                {
                    return (false, null, "Bu ürün için zaten açık bir ihtilaf başvurunuz bulunuyor.");
                }

                var dispute = new Models.Dispute.Dispute(
                  productId: model.ProductId,
                  initiatorId: buyerId,
                  reason: model.Reason,
                  description: model.Description
                );

                await _disputeRepo.AddAsync(dispute);

                await _context.SaveChangesAsync();

                var message = new DisputeMessage(
                  disputeId: dispute.Id,
                  senderId: buyerId,
                  message: model.Description
                );
                await _messageRepo.AddAsync(message);

                escrow.OpenDispute(model.Description);
                _escrowRepo.Update(escrow);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation($"İhtilaf BAŞARILI (Commit). Alıcı: {buyerId}, İhtilaf ID: {dispute.Id}");

                return (true, dispute.Id, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"İhtilaf açma sırasında KRİTİK HATA (Rollback). Alıcı: {buyerId}, Ürün: {model.ProductId}");
                return (false, null, "İhtilaf açılırken beklenmedik bir sistem hatası oluştu.");
            }
        }


        public async Task<(bool Success, string ErrorMessage)> AddDisputeMessageAsync(AddDisputeMessageViewModel model, string userId)
        {
            try
            {
                var dispute = await _disputeRepo.GetDisputeWithDetailsAsync(model.DisputeId);

                if (dispute == null)
                    return (false, "Mesaj göndermek istediğiniz ihtilaf bulunamadı.");

                if (dispute.Product.Escrow.BuyerId != userId && dispute.Product.Escrow.SellerId != userId)
                    return (false, "Bu ihtilafa mesaj gönderme yetkiniz yok.");

                if (dispute.Status == DisputeStatus.Closed || dispute.Status == DisputeStatus.Resolved)
                    return (false, "Bu ihtilaf zaten kapatılmış/çözülmüş.");

                var message = new DisputeMessage(
                  disputeId: model.DisputeId,
                  senderId: userId,
                  message: model.Message
                );

                await _messageRepo.AddAsync(message);
                await _messageRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"İhtilafa yeni mesaj eklendi (MessageID: {message.Id}, DisputeID: {dispute.Id})");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"İhtilafa mesaj eklenirken kritik hata (DisputeID: {model.DisputeId}, UserID: {userId})");
                return (false, "Mesajınız gönderilirken beklenmedik bir sistem hatası oluştu.");
            }
        }
    }
}