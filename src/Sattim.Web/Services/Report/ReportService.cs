using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Data;
using Sattim.Web.Models.Analytical; // Report modeli için
using Sattim.Web.Models.Product; // Product kontrolü için
using Sattim.Web.Models.User; // User kontrolü için
using Sattim.Web.Services.Notification; // INotificationService
using Sattim.Web.ViewModels.Report;
using System;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Report
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<ReportService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<(bool Success, string ErrorMessage)> CreateReportAsync(ReportFormViewModel model, string reporterId)
        {
            // --- 1. Varlık Doğrulaması (Entity Validation) ---
            // Şikayet edilen şey (ürün, kullanıcı vb.) gerçekten var mı?
            bool entityExists = false;
            switch (model.EntityType)
            {
                case ReportEntityType.Product:
                    if (int.TryParse(model.EntityId, out int productId))
                    {
                        entityExists = await _context.Products.AnyAsync(p => p.Id == productId);
                    }
                    break;
                case ReportEntityType.User:
                    entityExists = await _context.Users.AnyAsync(u => u.Id == model.EntityId);
                    break;
                case ReportEntityType.Bid:
                    if (int.TryParse(model.EntityId, out int bidId))
                    {
                        entityExists = await _context.Bids.AnyAsync(b => b.Id == bidId);
                    }
                    break;
                case ReportEntityType.Message:
                    if (int.TryParse(model.EntityId, out int messageId))
                    {
                        entityExists = await _context.Messages.AnyAsync(m => m.Id == messageId);
                    }
                    break;
            }

            if (!entityExists)
            {
                return (false, "Şikayet etmeye çalıştığınız varlık (ürün, kullanıcı vb.) bulunamadı.");
            }

            // --- 2. Tekrar Eden Şikayet Kontrolü ---
            // (MİMARİ KURALIMIZ) Birleşik anahtar (Composite Key) kontrolü:
            // Bu kullanıcı (ReporterId) bu varlığı (EntityType + EntityId)
            // daha önce şikayet etmiş mi?

            // FindAsync kullanamayız, çünkü anahtarımız 'Id' değil
            var existingReport = await _context.Reports
                .FirstOrDefaultAsync(r => r.ReporterId == reporterId &&
                                          r.EntityType == model.EntityType &&
                                          r.EntityId == model.EntityId);

            if (existingReport != null)
            {
                return (false, "Bu varlığı zaten daha önce şikayet etmişsiniz.");
            }

            // --- 3. Şikayeti Oluşturma ---
            try
            {
                // KURAL 4: YARAT (Constructor'ı kullan)
                var report = new Models.Analytical.Report(
                    reporterId,
                    model.EntityType,
                    model.EntityId,
                    model.Reason,
                    model.Description
                );

                // KURAL 5: KAYDET
                await _context.Reports.AddAsync(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Yeni şikayet (ID: {report.Id}) alındı. (Reporter: {reporterId}, Entity: {model.EntityType}/{model.EntityId})");

                // (Opsiyonel) Adminlere bildirim gönder
                // await _notificationService.SendNewReportToAdminsAsync(report);

                return (true, null);
            }
            catch (DbUpdateException ex)
            {
                // Bu, 'HasIndex(...).IsUnique(true)' kuralının (eğer 'Id' yerine
                // onu kullansaydık) veya başka bir veritabanı kısıtlamasının
                // ihlal edilmesi durumunda çalışır.
                _logger.LogWarning(ex, $"CreateReportAsync DbUpdateException (Muhtemelen mükerrer kayıt): {ex.InnerException?.Message}");
                return (false, "Bu varlığı zaten daha önce şikayet etmişsiniz.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CreateReportAsync KRİTİK HATA: {ex.Message}");
                return (false, "Şikayet oluşturulurken beklenmedik bir hata oluştu.");
            }
        }
    }
}