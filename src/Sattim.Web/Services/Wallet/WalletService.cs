using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Commission;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.UI;
using Sattim.Web.Models.Wallet;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.Services.Notification;
using Sattim.Web.ViewModels.Wallet;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Wallet
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepo;

        private readonly IGenericRepository<WalletTransaction> _wtxRepo;
        private readonly IGenericRepository<PayoutRequest> _payoutRepo;
        private readonly IGenericRepository<Escrow> _escrowRepo;
        private readonly IGenericRepository<Commission> _commissionRepo;
        private readonly IGenericRepository<SiteSettings> _settingsRepo;

        private readonly INotificationService _notificationService;

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<WalletService> _logger;

        private static string _systemWalletUserId;

        public WalletService(
            IWalletRepository walletRepo,
            IGenericRepository<WalletTransaction> wtxRepo,
            IGenericRepository<PayoutRequest> payoutRepo,
            IGenericRepository<Escrow> escrowRepo,
            IGenericRepository<Commission> commissionRepo,
            IGenericRepository<SiteSettings> settingsRepo,
            INotificationService notificationService,
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<WalletService> logger)
        {
            _walletRepo = walletRepo;
            _wtxRepo = wtxRepo;
            _payoutRepo = payoutRepo;
            _escrowRepo = escrowRepo;
            _commissionRepo = commissionRepo;
            _settingsRepo = settingsRepo;
            _notificationService = notificationService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // --------------------------------------------------------------------
        //  1. KULLANICI İŞLEMLERİ (Commands & Queries)
        // --------------------------------------------------------------------

        public async Task<WalletDashboardViewModel> GetWalletDashboardAsync(string userId)
        {
            var wallet = await _walletRepo.GetByIdAsync(userId);
            if (wallet == null)
            {
                _logger.LogWarning($"GetWalletDashboardAsync: Cüzdan bulunamadı (Kullanıcı: {userId}). Yeni cüzdan oluşturuluyor.");
                wallet = new Models.Wallet.Wallet(userId);
                await _walletRepo.AddAsync(wallet);
                await _walletRepo.UnitOfWork.SaveChangesAsync();
            }

            var transactions = await _walletRepo.GetRecentTransactionsAsync(userId);
            var payouts = await _walletRepo.GetPayoutHistoryAsync(userId);

            var model = new WalletDashboardViewModel
            {
                CurrentBalance = wallet.Balance,
                RecentTransactions = _mapper.Map<List<WalletTransactionViewModel>>(transactions),
                PayoutHistory = _mapper.Map<List<PayoutHistoryViewModel>>(payouts)
            };
            return model;
        }

        public async Task<(bool Success, string ErrorMessage)> RequestPayoutAsync(PayoutRequestViewModel model, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"Para çekme talebi (Transaction) başlatıldı: Kullanıcı: {userId}, Tutar: {model.Amount}");

            try
            {
                var wallet = await _walletRepo.GetByIdAsync(userId);
                if (wallet == null)
                    return (false, "Kullanıcı cüzdanı bulunamadı.");

                wallet.Withdraw(model.Amount);
                _walletRepo.Update(wallet);

                var payoutRequest = new PayoutRequest(
                    userId, model.Amount, model.BankName,
                    model.FullName, model.IBAN
                );
                await _payoutRepo.AddAsync(payoutRequest);

                await _context.SaveChangesAsync();

                var wtx = new WalletTransaction(
                    walletUserId: userId,
                    amount: -model.Amount,
                    type: WalletTransactionType.Withdrawal,
                    description: $"Para çekme talebi ({payoutRequest.Id} No'lu talep)",
                    relatedEntityId: payoutRequest.Id.ToString(),
                    relatedEntityType: "PayoutRequest"
                );
                await _wtxRepo.AddAsync(wtx);

                payoutRequest.LinkTransaction(wtx.Id);
                _payoutRepo.Update(payoutRequest);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Para çekme talebi BAŞARILI (Commit). Kullanıcı: {userId}, TalepID: {payoutRequest.Id}");

                await _notificationService.SendPayoutRequestedNotificationAsync(wallet.User, payoutRequest);

                return (true, null);
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, $"Para çekme talebi BAŞARISIZ (İş Kuralı - Rollback): {ex.Message}");
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Para çekme talebi sırasında KRİTİK HATA (Rollback).");
                return (false, "Talep işlenirken beklenmedik bir sistem hatası oluştu.");
            }
        }

        // --------------------------------------------------------------------
        //  2. SİSTEM & ADMİN İŞLEMLERİ (Commands)
        // --------------------------------------------------------------------

        public async Task<(bool Success, string ErrorMessage)> ReleaseFundsToSellerAsync(int escrowId, string adminOrSystemUserId)
        {
            _logger.LogInformation($"Fon serbest bırakma (ReleaseFunds) işlemi çağrıldı. EscrowID: {escrowId}");
            try
            {
                var escrow = await _escrowRepo.GetByIdAsync(escrowId);
                var commission = await _commissionRepo.GetByIdAsync(escrowId);
                if (escrow == null || commission == null)
                    return (false, "Sipariş (Escrow) veya Komisyon kaydı bulunamadı.");

                var sellerWallet = await _walletRepo.GetByIdAsync(escrow.SellerId);
                if (sellerWallet == null)
                    return (false, "Satıcı cüzdanı bulunamadı.");

                var systemWallet = await GetSystemWalletAsync();
                if (systemWallet == null)
                    return (false, "Sistem cüzdanı ayarlanmamış.");

                escrow.Release();
                commission.MarkAsCollected();

                decimal netAmount = escrow.Amount - commission.CommissionAmount;
                if (netAmount < 0) netAmount = 0;

                sellerWallet.Deposit(netAmount);
                systemWallet.Deposit(commission.CommissionAmount);

                await _wtxRepo.AddAsync(new WalletTransaction(
                    sellerWallet.UserId, netAmount, WalletTransactionType.Deposit,
                    $"Sipariş No #{escrow.ProductId} satışı tamamlandı.",
                    escrow.ProductId.ToString(), "Product"
                ));

                await _wtxRepo.AddAsync(new WalletTransaction(
                    systemWallet.UserId, commission.CommissionAmount, WalletTransactionType.Commission,
                    $"Sipariş No #{escrow.ProductId} komisyon geliri.",
                    escrow.ProductId.ToString(), "Product"
                ));

                _escrowRepo.Update(escrow);
                _commissionRepo.Update(commission);
                _walletRepo.Update(sellerWallet);
                _walletRepo.Update(systemWallet);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fon serbest bırakılırken (ReleaseFunds) KRİTİK HATA. EscrowID: {escrowId}");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string ErrorMessage)> RefundEscrowToBuyerAsync(int escrowId, string adminId, string reason)
        {
            _logger.LogInformation($"Fon iadesi (RefundEscrow) işlemi çağrıldı. EscrowID: {escrowId}");
            try
            {
                var escrow = await _escrowRepo.GetByIdAsync(escrowId);
                if (escrow == null)
                    return (false, "Sipariş (Escrow) kaydı bulunamadı.");

                var buyerWallet = await _walletRepo.GetByIdAsync(escrow.BuyerId);
                if (buyerWallet == null)
                    return (false, "Alıcı cüzdanı bulunamadı.");

                escrow.Refund();
                buyerWallet.Deposit(escrow.Amount);

                await _wtxRepo.AddAsync(new WalletTransaction(
                    buyerWallet.UserId, escrow.Amount, WalletTransactionType.Refund,
                    $"Sipariş No #{escrow.ProductId} iadesi ({reason}).",
                    escrow.ProductId.ToString(), "Product"
                ));

                _escrowRepo.Update(escrow);
                _walletRepo.Update(buyerWallet);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fon iadesi (RefundEscrow) sırasında KRİTİK HATA. EscrowID: {escrowId}");
                return (false, ex.Message);
            }
        }

        // --- Admin Payout Yönetimi ---

        public async Task<(bool Success, string ErrorMessage)> ApprovePayoutAsync(int payoutRequestId, string adminId)
        {
            try
            {
                var request = await _payoutRepo.GetByIdAsync(payoutRequestId);
                if (request == null || request.Status != PayoutStatus.Pending)
                    return (false, "Talep bulunamadı veya 'Beklemede' durumunda değil.");

                request.Approve($"Onaylayan Admin: {adminId}");
                _payoutRepo.Update(request);
                await _payoutRepo.UnitOfWork.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Para çekme talebi onaylanırken hata (ID: {payoutRequestId})");
                return (false, "İşlem sırasında bir hata oluştu.");
            }
        }

        public async Task<(bool Success, string ErrorMessage)> CompletePayoutAsync(int payoutRequestId, string adminId)
        {
            try
            {
                var request = await _payoutRepo.GetByIdAsync(payoutRequestId);
                if (request == null || request.Status != PayoutStatus.Approved)
                    return (false, "Talep bulunamadı veya 'Onaylandı' durumunda değil.");

                request.Complete($"İşlemi tamamlayan Admin: {adminId}");
                _payoutRepo.Update(request);
                await _payoutRepo.UnitOfWork.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Para çekme talebi tamamlanırken hata (ID: {payoutRequestId})");
                return (false, "İşlem sırasında bir hata oluştu.");
            }
        }

        public async Task<(bool Success, string ErrorMessage)> RejectPayoutAsync(int payoutRequestId, string adminId, string reason)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"Para çekme talebi reddediliyor (Transaction): {payoutRequestId}, Admin: {adminId}");

            try
            {
                var request = await _payoutRepo.GetByIdAsync(payoutRequestId);
                if (request == null || (request.Status != PayoutStatus.Pending && request.Status != PayoutStatus.Approved))
                    return (false, "Talep bulunamadı veya 'Beklemede'/'Onaylandı' durumunda değil.");

                var wallet = await _walletRepo.GetByIdAsync(request.UserId);
                if (wallet == null)
                    return (false, "İlgili kullanıcının cüzdanı bulunamadı.");

                string note = $"Reddeden Admin: {adminId}. Neden: {reason}";
                request.Reject(note);
                _payoutRepo.Update(request);

                wallet.Deposit(request.Amount);
                _walletRepo.Update(wallet);

                await _wtxRepo.AddAsync(new WalletTransaction(
                    wallet.UserId, request.Amount, WalletTransactionType.Refund,
                    $"Reddedilen Talep No #{request.Id} iadesi. Neden: {reason}",
                    request.Id.ToString(), "PayoutRequest"
                ));

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Para çekme talebi reddedildi (Commit). (ID: {payoutRequestId})");

                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Para çekme talebi reddedilirken hata (ID: {payoutRequestId}, Rollback).");
                return (false, "Talep reddedilirken bir sistem hatası oluştu.");
            }
        }

        // --- Yardımcı Metot ---
        private async Task<Models.Wallet.Wallet> GetSystemWalletAsync()
        {
            if (string.IsNullOrEmpty(_systemWalletUserId))
            {
                var setting = await _settingsRepo.FirstOrDefaultAsync(s => s.Key == "SystemWalletUserId");
                if (setting == null)
                    throw new InvalidOperationException("KRİTİK SİSTEM HATASI: 'SystemWalletUserId' ayarı bulunamadı.");

                _systemWalletUserId = setting.Value;
            }

            var systemWallet = await _walletRepo.GetByIdAsync(_systemWalletUserId);
            if (systemWallet == null)
                throw new InvalidOperationException($"KRİTİK SİSTEM HATASI: 'SystemWalletUserId' ({_systemWalletUserId}) için bir cüzdan bulunamadı.");

            return systemWallet;
        }
    }
}