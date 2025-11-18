using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.Payment;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet;
using Sattim.Web.Repositories.Interface;
using Sattim.Web.Services.Notification;
using Sattim.Web.ViewModels.Payment;
using System;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Payment
{
    public class PaymentService : IPaymentService
    {
        private readonly IGenericRepository<Escrow> _escrowRepo;
        private readonly IGenericRepository<Models.Payment.Payment> _paymentRepo;
        private readonly IGenericRepository<Models.Wallet.Wallet> _walletRepo;
        private readonly IGenericRepository<WalletTransaction> _wtxRepo;
        private readonly IGenericRepository<ApplicationUser> _userRepo;

        private readonly IGatewayService _gatewayService;
        private readonly INotificationService _notificationService;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
          IGenericRepository<Escrow> escrowRepo,
          IGenericRepository<Models.Payment.Payment> paymentRepo,
          IGenericRepository<Models.Wallet.Wallet> walletRepo,
          IGenericRepository<WalletTransaction> wtxRepo,
          IGenericRepository<ApplicationUser> userRepo,
          IGatewayService gatewayService,
          INotificationService notificationService,
          ApplicationDbContext context,
          ILogger<PaymentService> logger)
        {
            _escrowRepo = escrowRepo;
            _paymentRepo = paymentRepo;
            _walletRepo = walletRepo;
            _wtxRepo = wtxRepo;
            _userRepo = userRepo;
            _gatewayService = gatewayService;
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        public async Task<CheckoutViewModel> CreateGatewayCheckoutAsync(int escrowId, string userId)
        {
            try
            {
                var escrow = await _escrowRepo.GetByIdAsync(escrowId);
                var user = await _userRepo.GetByIdAsync(userId);

                if (escrow == null)
                    throw new KeyNotFoundException("Ödeme yapılmak istenen sipariş bulunamadı.");
                if (user == null)
                    throw new KeyNotFoundException("Kullanıcı bulunamadı.");
                if (escrow.BuyerId != userId)
                    throw new UnauthorizedAccessException("Bu siparişin ödemesini sadece alıcı yapabilir.");
                if (escrow.Status != EscrowStatus.Pending)
                    throw new InvalidOperationException("Bu siparişin ödemesi zaten yapılmış veya iptal edilmiş.");

                var payment = new Models.Payment.Payment(
                  escrowProductId: escrowId,
                  amount: escrow.Amount,
                  method: PaymentMethod.CreditCard
                );
                await _paymentRepo.AddAsync(payment);
                await _paymentRepo.UnitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Ödeme (ID: {payment.Id}) 'Pending' olarak oluşturuldu. Ağ geçidi çağrılıyor...");

                var (success, htmlContent, error) = await _gatewayService.CreateCheckoutFormAsync(payment, user);

                if (!success)
                {
                    payment.Fail($"Ağ geçidi hatası: {error}");
                    _paymentRepo.Update(payment);
                    await _paymentRepo.UnitOfWork.SaveChangesAsync();

                    return new CheckoutViewModel { Success = false, ErrorMessage = error };
                }

                return new CheckoutViewModel
                {
                    Success = true,
                    PaymentId = payment.Id,
                    HtmlContent = htmlContent
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ağ geçidi ödemesi (CreateGatewayCheckoutAsync) oluşturulurken hata. EscrowID: {escrowId}");
                return new CheckoutViewModel { Success = false, ErrorMessage = "Ödeme oturumu başlatılırken beklenmedik bir sistem hatası oluştu." };
            }
        }

        public async Task<(bool Success, string ErrorMessage)> PayWithWalletAsync(int escrowId, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"Cüzdanla ödeme (Transaction) başlatıldı: Kullanıcı: {userId}, EscrowID: {escrowId}");

            try
            {
                var escrow = await _escrowRepo.GetByIdAsync(escrowId);
                var wallet = await _walletRepo.GetByIdAsync(userId);

                if (escrow == null || wallet == null)
                    return (false, "Sipariş veya cüzdan bulunamadı.");
                if (escrow.BuyerId != userId)
                    return (false, "Bu sipariş size ait değil.");
                if (escrow.Status != EscrowStatus.Pending)
                    return (false, "Bu siparişin ödemesi zaten yapılmış.");

                wallet.Withdraw(escrow.Amount);
                _walletRepo.Update(wallet);

                escrow.Fund();
                _escrowRepo.Update(escrow);

                var payment = new Models.Payment.Payment(escrowId, escrow.Amount, PaymentMethod.Wallet);
                payment.Complete("WALLET_INTERNAL", "Cüzdan ile anında ödendi");
                await _paymentRepo.AddAsync(payment);

                var wtx = new WalletTransaction(
                  walletUserId: userId,
                  amount: -escrow.Amount,
                  type: WalletTransactionType.Payment,
                  description: $"Sipariş No #{escrow.ProductId} için ödeme",
                  relatedEntityId: escrow.ProductId.ToString(),
                  relatedEntityType: "Product"
                );
                await _wtxRepo.AddAsync(wtx);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Cüzdanla ödeme BAŞARILI (Commit). Kullanıcı: {userId}, EscrowID: {escrowId}");

                return (true, null);
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, $"Cüzdanla ödeme BAŞARISIZ (İş Kuralı - Rollback): {ex.Message}");
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Cüzdanla ödeme sırasında KRİTİK HATA (Rollback).");
                return (false, "Ödeme işlenirken beklenmedik bir sistem hatası oluştu.");
            }
        }

        public async Task<bool> ProcessPaymentConfirmationAsync(PaymentConfirmationViewModel confirmation)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"Ağ geçidi 'callback'i işleniyor (Transaction başladı): PaymentID: {confirmation.PaymentId}");

            try
            {
                var payment = await _paymentRepo.GetByIdAsync(confirmation.PaymentId);

                if (payment == null)
                {
                    _logger.LogError($"Callback hatası: PaymentID: {confirmation.PaymentId} bulunamadı.");
                    await transaction.RollbackAsync();
                    return false;
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    _logger.LogWarning($"Callback tekrarı: PaymentID: {confirmation.PaymentId} zaten işlenmiş (Status: {payment.Status}).");
                    await transaction.RollbackAsync();
                    return true;
                }

                var escrow = await _escrowRepo.GetByIdAsync(payment.EscrowProductId);
                if (escrow == null)
                {
                    _logger.LogError($"Callback hatası: PaymentID: {confirmation.PaymentId} için Escrow bulunamadı.");
                    await transaction.RollbackAsync();
                    return false;
                }

                if (confirmation.Success)
                {
                    payment.Complete(confirmation.TransactionId, confirmation.GatewayResponse);
                    escrow.Fund();

                    _paymentRepo.Update(payment);
                    _escrowRepo.Update(escrow);

                    _logger.LogInformation($"Ağ geçidi ödemesi BAŞARILI. PaymentID: {payment.Id}, EscrowID: {escrow.ProductId}");
                }
                else
                {
                    payment.Fail(confirmation.GatewayResponse ?? confirmation.ErrorMessage);
                    _paymentRepo.Update(payment);

                    _logger.LogWarning($"Ağ geçidi ödemesi BAŞARISIZ. PaymentID: {payment.Id}. Neden: {confirmation.ErrorMessage}");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (confirmation.Success)
                {
                    await _notificationService.SendPaymentSuccessNotificationAsync(escrow.Buyer, escrow.Product);
                    await _notificationService.SendSellerPaymentReceivedNotificationAsync(escrow.Seller, escrow.Product);
                }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogCritical(ex, $"Ağ geçidi 'callback'i işlenirken KRİTİK HATA (Rollback). PaymentID: {confirmation.PaymentId}");
                return false;
            }
        }
    }
}