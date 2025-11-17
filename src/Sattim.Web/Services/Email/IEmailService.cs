using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Email
{
    public interface IEmailService
    {
        /// <summary>
        /// Veritabanından bir e-posta şablonu alır, işler ve gönderir.
        /// (Bu, servisin birincil metodudur)
        /// </summary>
        /// <param name="toEmail">Alıcı e-posta adresi</param>
        /// <param name="templateName">EmailTemplates tablosundaki 'Name' (örn: "AuctionWon")</param>
        /// <param name="placeholders">Değiştirilecek anahtar-değerler (örn: {{UserName}}, "Ali")</param>
        Task SendTemplateEmailAsync(string toEmail, string templateName, Dictionary<string, string> placeholders);

        /// <summary>
        /// Önceden işlenmiş ham bir HTML e-postası gönderir.
        /// </summary>
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }

    /// <summary>
    /// SiteSettings'ten okunan ayarları önbellekte tutmak için DTO.
    /// </summary>
    public class EmailSettingsDto
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public bool UseSSL { get; set; }
    }
}