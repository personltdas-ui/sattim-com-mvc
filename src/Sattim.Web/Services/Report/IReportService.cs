using Sattim.Web.ViewModels.Report; // Gerekli ViewModel
using System.Threading.Tasks;

namespace Sattim.Web.Services.Report
{
    // BU SERVİS [Authorize] İLE KORUNMALIDIR
    public interface IReportService
    {
        /// <summary>
        /// Kullanıcının bir varlığı (Ürün, Kullanıcı, Yorum vb.)
        /// şikayet etmesini sağlar.
        /// İş Mantığı:
        /// 1. Varlığın (örn: ProductId=123) gerçekten var olduğunu doğrular.
        /// 2. Kullanıcının bu varlığı DAHA ÖNCE şikayet edip etmediğini
        ///    birleşik anahtardan (Composite Key) kontrol eder.
        /// 3. 'new Report(...)' constructor'ı ile şikayeti oluşturur.
        /// 4. Değişiklikleri kaydeder.
        /// 5. (Opsiyonel) Adminlere bildirim gönderir.
        /// </summary>
        /// <param name="model">Şikayet formu verilerini (EntityId, Type, Reason) içerir</param>
        /// <param name="reporterId">Şikayeti yapan kullanıcının ID'si</param>
        /// <returns>Başarısızsa (false, "Hata Mesajı"), başarılıysa (true, null)</returns>
        Task<(bool Success, string ErrorMessage)> CreateReportAsync(ReportFormViewModel model, string reporterId);
    }
}