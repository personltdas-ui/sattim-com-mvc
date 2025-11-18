using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Email
{
    public interface IEmailService
    {
        Task SendTemplateEmailAsync(string toEmail, string templateName, Dictionary<string, string> placeholders);

        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }

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