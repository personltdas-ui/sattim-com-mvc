using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // DbContext (Transaction)
using Sattim.Web.Models.Commission;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.UI; // SiteSettings
using Sattim.Web.Models.Wallet;
using Sattim.Web.Services.Notification; // INotificationService
using Sattim.Web.Services.Repositories; // Gerekli tüm Repolar
using Sattim.Web.ViewModels.Wallet; // Arayüzün istediği DTO'lar
using System;
using System.Linq;
using System.Threading.Tasks;

// Arayüzünüzle aynı namespace'i kullanır
namespace Sattim.Web.Services.Wallet
{
    public class WalletService : IWalletService
    {
        // Gerekli Özel Repo
        private readonly IWalletRepository _walletRepo;

        // Gerekli Jenerik Repolar
        private readonly IGenericRepository<WalletTransaction> _wtxRepo;
        private readonly IGenericRepository<PayoutRequest> _payoutRepo;
        private readonly IGenericRepository<Escrow> _escrowRepo;
        private readonly IGenericRepository<Commission> _commissionRepo;
        private readonly IGenericRepository<SiteSettings> _settingsRepo;

        // Gerekli Diğer Servisler
        private readonly INotificationService _notificationService;

        // Yardımcılar
        private readonly ApplicationDbContext _context; // Transaction yönetimi için
        private readonly IMapper _mapper;
        private readonly ILogger<WalletService> _logger;

        // "SystemWalletUserId" ayarını önbelleğe almak için (performans)
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

        // ====================================================================
        //  1. KULLANICI İŞLEMLERİ (Commands & Queries)
        // ====================================================================

        public async Task<WalletDashboardViewModel> GetWalletDashboardAsync(string userId)
        {
            // 1. Cüzdanı Al (veya oluştur)
            var wallet = await _walletRepo.GetByIdAsync(userId);
            if (wallet == null)
            {
                _logger.LogWarning($"GetWalletDashboardAsync: Cüzdan bulunamadı (Kullanıcı: {userId}). Yeni cüzdan oluşturuluyor.");
                wallet = new Models.Wallet.Wallet(userId);
                await _walletRepo.AddAsync(wallet);
                await _walletRepo.UnitOfWork.SaveChangesAsync();
            }

            // 2. Özel Repo: Son İşlemleri ve Talepleri al
            var transactions = await _walletRepo.GetRecentTransactionsAsync(userId);
            var payouts = await _walletRepo.GetPayoutHistoryAsync(userId);

            // 3. DTO'ya dönüştür
            var model = new WalletDashboardViewModel
            {
                CurrentBalance = wallet.Balance,
                RecentTransactions = _mapper.Map<List<WalletTransactionViewModel>>(transactions),
                PayoutHistory = _mapper.Map<List<PayoutHistoryViewModel>>(payouts)
            };
            return model;
        }

        /// <summary>
        /// (Kullanıcı) Para çekme talebi oluşturur.
        /// Transactional: Wallet, PayoutRequest, WalletTransaction.
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> RequestPayoutAsync(PayoutRequestViewModel model, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"Para çekme talebi (Transaction) başlatıldı: Kullanıcı: {userId}, Tutar: {model.Amount}");

            try
            {
                // 1. Varlığı Al (Takip et)
                var wallet = await _walletRepo.GetByIdAsync(userId);
                if (wallet == null)
                    return (false, "Kullanıcı cüzdanı bulunamadı.");

                // 2. İş Mantığını Modele Devret (Bakiye kontrolü)
                // (Eğer bakiye yetersizse bu metot 'InvalidOperationException' fırlatır)
                wallet.Withdraw(model.Amount);
                _walletRepo.Update(wallet);

                // 3. PayoutRequest (Talep) Oluştur
                var payoutRequest = new PayoutRequest(
                    userId, model.Amount, model.BankName,
                    model.FullName, model.IBAN
                );
                await _payoutRepo.AddAsync(payoutRequest);

                // 4. ID'leri almak için ÖNCE kaydet
                await _context.SaveChangesAsync();

                // 5. WalletTransaction (Dekont) Oluştur
                var wtx = new WalletTransaction(
                    walletUserId: userId,
                    amount: -model.Amount, // Para ÇIKIŞI (Negatif)
                    type: WalletTransactionType.Withdrawal,
                    description: $"Para çekme talebi ({payoutRequest.Id} No'lu talep)",
                    relatedEntityId: payoutRequest.Id.ToString(),
                    relatedEntityType: "PayoutRequest"
                );
                await _wtxRepo.AddAsync(wtx);

                // 6. Transaction'ı Dekonta Bağla (Model Metodu)
                payoutRequest.LinkTransaction(wtx.Id);
                _payoutRepo.Update(payoutRequest);

                // 7. Transaction'ı Kaydet ve Onayla
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Para çekme talebi BAŞARILI (Commit). Kullanıcı: {userId}, TalepID: {payoutRequest.Id}");

                // 8. (Transaction DIŞINDA) Bildirim Gönder
                await _notificationService.SendPayoutRequestedNotificationAsync(wallet.User, payoutRequest);

                return (true, null); // Başarılı
            }
            catch (InvalidOperationException ex) // (wallet.Withdraw'dan gelen "Yetersiz bakiye")
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, $"Para çekme talebi BAŞARISIZ (İş Kuralı - Rollback): {ex.Message}");
                return (false, ex.Message); // (Örn: "Yetersiz bakiye.")
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Para çekme talebi sırasında KRİTİK HATA (Rollback).");
                return (false, "Talep işlenirken beklenmedik bir sistem hatası oluştu.");
            }
        }

        // ====================================================================
        //  2. SİSTEM & ADMİN İŞLEMLERİ (Commands)
        // ====================================================================

        /// <summary>
        /// (KRİTİK - SİSTEM) Parayı 'Escrow'dan satıcının 'Wallet'ına aktarır.
        /// ÖNEMLİ: Bu metot, zaten var olan bir 'Transaction' içinden
        /// (örn: IShippingService.MarkAsDelivered) çağrılmalıdır.
        /// Kendi 'Transaction'ını VEYA 'SaveChangesAsync()'i çağırmaz!
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> ReleaseFundsToSellerAsync(int escrowId, string adminOrSystemUserId)
        {
            _logger.LogInformation($"Fon serbest bırakma (ReleaseFunds) işlemi çağrıldı. EscrowID: {escrowId}");
            try
            {
                // 1. Varlıkları Al (Takip et)
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

                // 2. İş Kuralları
                // (Çağıran servis (Shipping/Moderation) zaten 'Escrow'
                // durumunu 'Delivered' veya 'Resolved'a hazırlamış olmalı)

                // 3. İş Mantığını Modele Devret
                escrow.Release();
                commission.MarkAsCollected();

                decimal netAmount = escrow.Amount - commission.CommissionAmount;
                if (netAmount < 0) netAmount = 0; // Komisyon, satıştan fazla olamaz

                sellerWallet.Deposit(netAmount);
                systemWallet.Deposit(commission.CommissionAmount);

                // 4. Dekontları Oluştur
                // Satıcı Dekontu
                await _wtxRepo.AddAsync(new WalletTransaction(
                    sellerWallet.UserId, netAmount, WalletTransactionType.Deposit,
                    $"Sipariş No #{escrow.ProductId} satışı tamamlandı.",
                    escrow.ProductId.ToString(), "Product"
                ));

                // Sistem Komisyon Dekontu
                await _wtxRepo.AddAsync(new WalletTransaction(
                    systemWallet.UserId, commission.CommissionAmount, WalletTransactionType.Commission,
                    $"Sipariş No #{escrow.ProductId} komisyon geliri.",
                    escrow.ProductId.ToString(), "Product"
                ));

                // 5. Değişiklikleri Bildir (Kaydetme!)
                _escrowRepo.Update(escrow);
                _commissionRepo.Update(commission);
                _walletRepo.Update(sellerWallet);
                _walletRepo.Update(systemWallet);

                // 6. (Transaction DIŞINDA) Bildirim Gönder
                // (SaveChangesAsync'ten sonra çağrılmalı)
                // await _notificationService.SendFundsReleasedNotificationAsync(sellerWallet.User, netAmount);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fon serbest bırakılırken (ReleaseFunds) KRİTİK HATA. EscrowID: {escrowId}");
                return (false, ex.Message); // Hatanın, ana Transaction'ı durdurması için fırlat
            }
        }

        /// <summary>
        /// (KRİTİK - SİSTEM) 'Escrow'daki parayı alıcının 'Wallet'ına iade eder.
        /// ÖNEMLİ: Bu metot, 'IModerationService.ResolveDisputeForBuyer'
        /// gibi var olan bir 'Transaction' içinden çağrılmalıdır.
        /// Kendi 'Transaction'ını VEYA 'SaveChangesAsync()'i çağırmaz!
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> RefundEscrowToBuyerAsync(int escrowId, string adminId, string reason)
        {
            _logger.LogInformation($"Fon iadesi (RefundEscrow) işlemi çağrıldı. EscrowID: {escrowId}");
            try
            {
                // 1. Varlıkları Al (Takip et)
                var escrow = await _escrowRepo.GetByIdAsync(escrowId);
                if (escrow == null)
                    return (false, "Sipariş (Escrow) kaydı bulunamadı.");

                var buyerWallet = await _walletRepo.GetByIdAsync(escrow.BuyerId);
                if (buyerWallet == null)
                    return (false, "Alıcı cüzdanı bulunamadı.");

                // 2. İş Mantığını Modele Devret
                escrow.Refund();
                buyerWallet.Deposit(escrow.Amount);

                // 3. Dekontu Oluştur
                await _wtxRepo.AddAsync(new WalletTransaction(
                    buyerWallet.UserId, escrow.Amount, WalletTransactionType.Refund,
                    $"Sipariş No #{escrow.ProductId} iadesi ({reason}).",
                    escrow.ProductId.ToString(), "Product"
                ));

                // 4. Değişiklikleri Bildir (Kaydetme!)
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

                // (Bildirim gönder)
                // await _notificationService.SendPayoutApprovedNotificationAsync(...);
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

                // (Bildirim gönder)
                // await _notificationService.SendPayoutCompletedNotificationAsync(...);
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
            // Transactional: PayoutRequest, Wallet ve WalletTransaction güncellenmeli.
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

                // 1. Talebi Reddet
                string note = $"Reddeden Admin: {adminId}. Neden: {reason}";
                request.Reject(note);
                _payoutRepo.Update(request);

                // 2. Parayı Cüzdana İade Et
                wallet.Deposit(request.Amount);
                _walletRepo.Update(wallet);

                // 3. İade Dekontu Oluştur
                await _wtxRepo.AddAsync(new WalletTransaction(
                    wallet.UserId, request.Amount, WalletTransactionType.Refund,
                    $"Reddedilen Talep No #{request.Id} iadesi. Neden: {reason}",
                    request.Id.ToString(), "PayoutRequest"
                ));

                // 4. Kaydet ve Onayla
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Para çekme talebi reddedildi (Commit). (ID: {payoutRequestId})");

                // 5. (Transaction DIŞINDA) Bildirim Gönder
                // await _notificationService.SendPayoutRejectedNotificationAsync(...);
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
            // (Performans için statik bir değişkende önbelleğe alıyoruz)
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