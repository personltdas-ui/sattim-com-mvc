using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Caching.Memory; // IMemoryCache için eklendi
using Microsoft.Extensions.Logging;
using MimeKit;
using Sattim.Web.Models.UI; // EmailTemplate ve SiteSettings için eklendi
using Sattim.Web.Services.Repositories; // IGenericRepository için eklendi
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Sattim.Web.Services.Email
{
    public class EmailService : IEmailService
    {
        // DÜZELTME: DbContext yerine Repository'ler ve IMemoryCache enjekte edildi
        private readonly IGenericRepository<SiteSettings> _settingsRepo;
        private readonly IGenericRepository<EmailTemplate> _templateRepo;
        private readonly IMemoryCache _cache; // Performans için eklendi
        private readonly ILogger<EmailService> _logger;

        private const string EmailSettingsCacheKey = "EmailSettingsCache";

        public EmailService(
            IGenericRepository<SiteSettings> settingsRepo,
            IGenericRepository<EmailTemplate> templateRepo,
            IMemoryCache cache,
            ILogger<EmailService> logger)
        {
            _settingsRepo = settingsRepo;
            _templateRepo = templateRepo;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Ana e-posta gönderme metodu (Ham HTML)
        /// (Bu metot sizin kodunuzla neredeyse aynı, sadece GetEmailSettingsAsync değişti)
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                // 1. SMTP ayarlarını 'Cache' (Önbellek) üzerinden al
                var settings = await GetEmailSettingsAsync();
                if (settings == null)
                {
                    _logger.LogError("E-posta ayarları (SiteSettings) bulunamadı veya önbelleğe alınamadı. E-posta gönderilemiyor.");
                    return;
                }

                // 2. MailKit MimeMessage nesnesini oluştur
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                // 3. SmtpClient (MailKit'in) ile bağlan ve gönder
                using (var client = new SmtpClient())
                {
                    var options = settings.UseSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
                    await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, options);
                    await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPass);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"E-posta başarıyla gönderildi: {toEmail} (Konu: {subject})");
            }
            catch (Exception ex)
            {
                // Hata durumunda, servisin çökmesini engelle. (Bu çok iyi bir pratik)
                _logger.LogError(ex, $"SendEmailAsync (To: {toEmail}) KRİTİK HATA: {ex.Message}");
            }
        }

        /// <summary>
        /// YENİ METOT: Veritabanındaki şablonu kullanarak e-posta gönderir.
        /// </summary>
        public async Task SendTemplateEmailAsync(string toEmail, string templateName, Dictionary<string, string> placeholders)
        {
            try
            {
                // 1. Şablonu al (Jenerik Repo ile)
                var template = await _templateRepo.FirstOrDefaultAsync(
                    t => t.Name == templateName && t.IsActive
                );

                if (template == null)
                {
                    _logger.LogError($"E-posta şablonu bulunamadı veya aktif değil: '{templateName}'");
                    return;
                }

                // 2. Yer tutucuları (Placeholders) işle
                string subject = template.Subject;
                string htmlBody = template.Body;

                foreach (var placeholder in placeholders)
                {
                    // Örn: "{{UserName}}" -> "Ali Veli"
                    string key = $"{{{{{placeholder.Key}}}}}";
                    subject = subject.Replace(key, placeholder.Value);
                    htmlBody = htmlBody.Replace(key, placeholder.Value);
                }

                // 3. Ana gönderme metodunu çağır
                await SendEmailAsync(toEmail, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SendTemplateEmailAsync (Template: {templateName}) KRİTİK HATA.");
            }
        }


        /// <summary>
        /// DÜZELTİLMİŞ YARDIMCI METOT: Ayarları DbContext'ten değil,
        /// Repository ve IMemoryCache üzerinden okur.
        /// </summary>
        private async Task<EmailSettingsDto> GetEmailSettingsAsync()
        {
            // 1. Önbelleği (Cache) kontrol et
            return await _cache.GetOrCreateAsync(EmailSettingsCacheKey, async (cacheEntry) =>
            {
                _logger.LogInformation("E-posta ayarları önbellekte bulunamadı. Veritabanından okunuyor...");

                // 2. Önbellek ayarlarını belirle (örn: 10 dakika)
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                // 3. Repository kullanarak ayarları al
                var settingsGroup = await _settingsRepo.FindAsync(
                    s => s.Category == SettingCategory.Email
                );

                if (!settingsGroup.Any())
                {
                    _logger.LogError("SiteSettings'te 'Email' kategorisine ait ayar bulunamadı.");
                    return null;
                }

                var settingsDict = settingsGroup.ToDictionary(s => s.Key, s => s.Value);

                // 4. Ayarları DTO'ya haritala (Bu kodunuz zaten mükemmeldi)
                try
                {
                    return new EmailSettingsDto
                    {
                        SmtpHost = settingsDict["SmtpHost"],
                        SmtpPort = int.Parse(settingsDict["SmtpPort"]),
                        SmtpUser = settingsDict["SmtpUser"],
                        SmtpPass = settingsDict["SmtpPass"], // (Not: Bu şifre hala DB'de, ideal değil)
                        FromEmail = settingsDict["FromEmail"],
                        FromName = settingsDict["FromName"],
                        UseSSL = bool.Parse(settingsDict.GetValueOrDefault("UseSSL", "true"))
                    };
                }
                catch (KeyNotFoundException ex)
                {
                    _logger.LogCritical(ex, $"SiteSettings'te 'Email' ayarları EKSİK. '{ex.Message}' anahtarı bulunamadı. Sistem e-posta gönderemez.");
                    return null;
                }
            });
        }
    }
}