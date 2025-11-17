using Microsoft.EntityFrameworkCore; // Transaction için
using Microsoft.Extensions.Logging;
using Sattim.Web.Data; // DbContext (Transaction)
using Sattim.Web.Models.Escrow;
using Sattim.Web.Models.Payment;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet;
using Sattim.Web.Services.Notification; // INotificationService
using Sattim.Web.Services.Repositories; // Gerekli tüm Repolar
using Sattim.Web.ViewModels.Payment; // Arayüzün istediği DTO'lar
using System;
using System.Threading.Tasks;

// Arayüzünüzle aynı namespace'i kullanır
namespace Sattim.Web.Services.Payment
{
    public class PaymentService : IPaymentService
    {
        // Gerekli Jenerik Repolar
        private readonly IGenericRepository<Escrow> _escrowRepo;
        private readonly IGenericRepository<Models.Payment.Payment> _paymentRepo;
        private readonly IGenericRepository<Models.Wallet.Wallet> _walletRepo;
        private readonly IGenericRepository<WalletTransaction> _wtxRepo;
        private readonly IGenericRepository<ApplicationUser> _userRepo;

        // Gerekli Diğer Servisler
        private readonly IGatewayService _gatewayService; // Harici ödeme (Iyzico/Stripe)
        private readonly INotificationService _notificationService;

        // Yardımcılar
        private readonly ApplicationDbContext _context; // Transaction yönetimi için
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

        // ====================================================================
        //  COMMANDS (Yazma İşlemleri)
        // ====================================================================

        public async Task<CheckoutViewModel> CreateGatewayCheckoutAsync(int escrowId, string userId)
        {
            try
            {
                // 1. Varlıkları Al (Takip et)
                var escrow = await _escrowRepo.GetByIdAsync(escrowId);
                var user = await _userRepo.GetByIdAsync(userId);

                // 2. İş Kuralları (Validasyon)
                if (escrow == null)
                    throw new KeyNotFoundException("Ödeme yapılmak istenen sipariş bulunamadı.");
                if (user == null)
                    throw new KeyNotFoundException("Kullanıcı bulunamadı.");
                if (escrow.BuyerId != userId)
                    throw new UnauthorizedAccessException("Bu siparişin ödemesini sadece alıcı yapabilir.");
                if (escrow.Status != EscrowStatus.Pending)
                    throw new InvalidOperationException("Bu siparişin ödemesi zaten yapılmış veya iptal edilmiş.");

                // 3. Veritabanına 'Pending' (Beklemede) Payment kaydı oluştur
                var payment = new Models.Payment.Payment(
                    escrowProductId: escrowId,
                    amount: escrow.Amount,
                    method: PaymentMethod.CreditCard // (veya seçilen metoda göre)
                );
                await _paymentRepo.AddAsync(payment);
                await _paymentRepo.UnitOfWork.SaveChangesAsync(); // ID'sini almak için kaydet

                _logger.LogInformation($"Ödeme (ID: {payment.Id}) 'Pending' olarak oluşturuldu. Ağ geçidi çağrılıyor...");

                // 4. Harici Ağ Geçidi (Gateway) Servisini Çağır
                var (success, htmlContent, error) = await _gatewayService.CreateCheckoutFormAsync(payment, user);

                if (!success)
                {
                    // Ağ geçidi başarısız olduysa, 'Pending' kaydımızı 'Failed' yap
                    payment.Fail($"Ağ geçidi hatası: {error}");
                    _paymentRepo.Update(payment);
                    await _paymentRepo.UnitOfWork.SaveChangesAsync();

                    return new CheckoutViewModel { Success = false, ErrorMessage = error };
                }

                // 5. Başarılı: View'e döndürülecek DTO'yu oluştur
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
            // 1. Transaction'ı Başlat (Cüzdan ve Sipariş aynı anda güncellenmeli)
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"Cüzdanla ödeme (Transaction) başlatıldı: Kullanıcı: {userId}, EscrowID: {escrowId}");

            try
            {
                // 2. Varlıkları Al (Takip et - 'AsNoTracking' KULLANMA)
                var escrow = await _escrowRepo.GetByIdAsync(escrowId);
                var wallet = await _walletRepo.GetByIdAsync(userId); // PK'si UserId'dir

                // 3. İş Kuralları (Validasyon)
                if (escrow == null || wallet == null)
                    return (false, "Sipariş veya cüzdan bulunamadı.");
                if (escrow.BuyerId != userId)
                    return (false, "Bu sipariş size ait değil.");
                if (escrow.Status != EscrowStatus.Pending)
                    return (false, "Bu siparişin ödemesi zaten yapılmış.");

                // 4. İş Mantığını Modele Devret (Wallet)
                // (Eğer bakiye yetersizse bu metot 'InvalidOperationException' fırlatır)
                wallet.Withdraw(escrow.Amount);
                _walletRepo.Update(wallet);

                // 5. İş Mantığını Modele Devret (Escrow)
                escrow.Fund(); // Statüyü 'Funded' yapar
                _escrowRepo.Update(escrow);

                // 6. Ödeme (Payment) Kaydını Oluştur (Dekont)
                var payment = new Models.Payment.Payment(escrowId, escrow.Amount, PaymentMethod.Wallet);
                payment.Complete("WALLET_INTERNAL", "Cüzdan ile anında ödendi");
                await _paymentRepo.AddAsync(payment);

                // 7. Cüzdan Hareketi (WalletTransaction) Kaydını Oluştur
                var wtx = new WalletTransaction(
                    walletUserId: userId,
                    amount: -escrow.Amount, // Para ÇIKIŞI (Negatif)
                    type: WalletTransactionType.Payment,
                    description: $"Sipariş No #{escrow.ProductId} için ödeme",
                    relatedEntityId: escrow.ProductId.ToString(),
                    relatedEntityType: "Product"
                );
                await _wtxRepo.AddAsync(wtx);

                // 8. Transaction'ı Kaydet ve Onayla
                // (Wallet, Escrow, Payment, WalletTransaction - 4 varlık aynı anda)
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Cüzdanla ödeme BAŞARILI (Commit). Kullanıcı: {userId}, EscrowID: {escrowId}");

                // 9. (Transaction DIŞINDA) Bildirimleri Gönder
                // (Alıcı ve Satıcıyı bilgilendir)
                // (Bu 'Escrow'u 'Product' ile Include etmediğimiz için,
                // bildirim servisine göndermeden önce Product'ı yüklememiz gerekir)

                // await _notificationService.SendPaymentSuccessNotificationAsync(...);
                // await _notificationService.SendSellerPaymentReceivedNotificationAsync(...);

                return (true, null); // Başarılı
            }
            catch (InvalidOperationException ex) // (wallet.Withdraw'dan gelen "Yetersiz bakiye" hatası)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, $"Cüzdanla ödeme BAŞARISIZ (İş Kuralı - Rollback): {ex.Message}");
                return (false, ex.Message); // (Örn: "Yetersiz bakiye.")
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
            // 1. Transaction'ı Başlat
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation($"Ağ geçidi 'callback'i işleniyor (Transaction başladı): PaymentID: {confirmation.PaymentId}");

            try
            {
                // 2. Varlıkları Al (Takip et)
                var payment = await _paymentRepo.GetByIdAsync(confirmation.PaymentId);

                // 3. Validasyon
                if (payment == null)
                {
                    _logger.LogError($"Callback hatası: PaymentID: {confirmation.PaymentId} bulunamadı.");
                    await transaction.RollbackAsync();
                    return false; // Ağ geçidine "Başarısız" bildir
                }

                // Bu 'callback' zaten işlendi mi?
                if (payment.Status != PaymentStatus.Pending)
                {
                    _logger.LogWarning($"Callback tekrarı: PaymentID: {confirmation.PaymentId} zaten işlenmiş (Status: {payment.Status}).");
                    await transaction.RollbackAsync();
                    return true; // Ağ geçidine "Başarılı" (tekrar gönderme) bildir
                }

                var escrow = await _escrowRepo.GetByIdAsync(payment.EscrowProductId);
                if (escrow == null)
                {
                    _logger.LogError($"Callback hatası: PaymentID: {confirmation.PaymentId} için Escrow bulunamadı.");
                    await transaction.RollbackAsync();
                    return false;
                }

                // 4. İşlemi Onayla veya Reddet
                if (confirmation.Success)
                {
                    // 4a. ÖDEME BAŞARILI
                    payment.Complete(confirmation.TransactionId, confirmation.GatewayResponse);
                    escrow.Fund(); // Escrow'u 'Funded' (Ödendi) yap

                    _paymentRepo.Update(payment);
                    _escrowRepo.Update(escrow);

                    _logger.LogInformation($"Ağ geçidi ödemesi BAŞARILI. PaymentID: {payment.Id}, EscrowID: {escrow.ProductId}");
                }
                else
                {
                    // 4b. ÖDEME BAŞARISIZ
                    payment.Fail(confirmation.GatewayResponse ?? confirmation.ErrorMessage);
                    _paymentRepo.Update(payment);

                    _logger.LogWarning($"Ağ geçidi ödemesi BAŞARISIZ. PaymentID: {payment.Id}. Neden: {confirmation.ErrorMessage}");
                }

                // 5. Transaction'ı Kaydet ve Onayla
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 6. (Transaction DIŞINDA) Bildirimleri Gönder
                if (confirmation.Success)
                {
                    // (Kullanıcıları ve Ürünü tekrar çekmemiz gerekebilir
                    // veya Escrow'dan alıp bildirim servisine göndermemiz)
                    await _notificationService.SendPaymentSuccessNotificationAsync(escrow.Buyer, escrow.Product);
                    await _notificationService.SendSellerPaymentReceivedNotificationAsync(escrow.Seller, escrow.Product);
                }

                return true; // Ağ geçidine "Başarılı" (işledik) bildir
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogCritical(ex, $"Ağ geçidi 'callback'i işlenirken KRİTİK HATA (Rollback). PaymentID: {confirmation.PaymentId}");
                return false; // Ağ geçidine "Başarısız" (tekrar dene) bildir
            }
        }
    }
}