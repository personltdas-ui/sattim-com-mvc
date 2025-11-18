using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sattim.Web.Models.Blog;
using Sattim.Web.Models.Category;
using Sattim.Web.Models.Security;
using Sattim.Web.Models.UI;
using Sattim.Web.Models.User;
using Sattim.Web.Models.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sattim.Web.Data
{
    public static class SeedData
    {
        private const string AdminPassword = "Admin!123";
        private const string AdminEmail = "admin@sattim.com";
        private const string AdminFullName = "Sistem Yöneticisi";
        private const string AdminUserName = "admin";

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("SeedData");

            logger.LogInformation("Veritabanı tohumlama (seeding) işlemi başlıyor...");

            await context.Database.MigrateAsync();

            await SeedRolesAsync(roleManager, logger);

            var adminUser = await SeedAdminUserAsync(userManager, logger);

            if (adminUser != null)
            {
                await SeedUserRelationsAsync(context, adminUser.Id, logger);
            }
            else
            {
                logger.LogCritical("Admin kullanıcısı bulunamadı veya oluşturulamadı. Seed işlemi devam edemez.");
                return;
            }

            await SeedSiteSettingsAsync(context, adminUser.Id, logger);

            await SeedEmailTemplatesAsync(context, logger);

            await SeedCategoriesAsync(context, logger);

            logger.LogInformation("Veritabanı tohumlama işlemi başarıyla tamamlandı.");
        }


        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            string[] roleNames = { "Admin", "Moderator", "User" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    logger.LogInformation($"'{roleName}' rolü oluşturuluyor...");
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task<ApplicationUser> SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            var adminUser = await userManager.FindByEmailAsync(AdminEmail);
            if (adminUser != null)
            {
                logger.LogWarning("Admin kullanıcısı (ApplicationUser) zaten mevcut.");
                return adminUser;
            }

            logger.LogInformation("Admin kullanıcısı oluşturuluyor...");

            adminUser = new ApplicationUser(
                userName: AdminUserName,
                email: AdminEmail,
                fullName: AdminFullName
            );

            adminUser.EmailConfirmed = true;

            var result = await userManager.CreateAsync(adminUser, AdminPassword);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Bilinmeyen hata";
                logger.LogCritical($"Admin kullanıcısı oluşturulamadı: {error}");
                throw new Exception($"Admin kullanıcısı oluşturulamadı: {error}");
            }

            logger.LogInformation("Admin kullanıcısı 'Admin' rolüne atanıyor...");
            await userManager.AddToRoleAsync(adminUser, "Admin");

            logger.LogInformation("Admin kullanıcısı başarıyla oluşturuldu.");
            return adminUser;
        }

        private static async Task SeedUserRelationsAsync(ApplicationDbContext context, string adminUserId, ILogger logger)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                bool saveChangesNeeded = false;

                if (!await context.Wallets.AnyAsync(w => w.UserId == adminUserId))
                {
                    logger.LogInformation($"Admin (UserId: {adminUserId}) için Wallet oluşturuluyor...");
                    await context.Wallets.AddAsync(new Wallet(adminUserId));
                    saveChangesNeeded = true;
                }

                if (!await context.UserProfiles.AnyAsync(p => p.UserId == adminUserId))
                {
                    logger.LogInformation($"Admin (UserId: {adminUserId}) için UserProfile oluşturuluyor...");
                    await context.UserProfiles.AddAsync(new UserProfile(adminUserId));
                    saveChangesNeeded = true;
                }

                if (!await context.TwoFactorAuths.AnyAsync(t => t.UserId == adminUserId))
                {
                    logger.LogInformation($"Admin (UserId: {adminUserId}) için TwoFactorAuth oluşturuluyor...");
                    await context.TwoFactorAuths.AddAsync(new TwoFactorAuth(adminUserId));
                    saveChangesNeeded = true;
                }

                if (saveChangesNeeded)
                {
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.LogInformation("Admin 1-to-1 ilişkili modelleri (Wallet, Profile, 2FA) başarıyla oluşturuldu/doğrulandı.");
                }
                else
                {
                    logger.LogInformation("Admin 1-to-1 ilişkili modelleri (Wallet, Profile, 2FA) zaten mevcut.");
                    await transaction.RollbackAsync();
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogCritical(ex, "Admin 1-to-1 ilişkili modelleri (Wallet, Profile) oluşturulurken KRİTİK HATA.");
                throw;
            }
        }

        private static async Task SeedSiteSettingsAsync(ApplicationDbContext context, string adminUserId, ILogger logger)
        {
            var existingKeysList = await context.SiteSettings
                .Select(s => s.Key)
                .ToListAsync();

            var existingKeys = new HashSet<string>(existingKeysList);

            var allSettings = new List<SiteSettings>
            {
                new SiteSettings("CommissionRate", SettingCategory.Commission, "5.0", "Satışlardan alınacak komisyon yüzdesi (örn: 5.0)"),
                new SiteSettings("SystemWalletUserId", SettingCategory.Wallet, adminUserId, "Komisyon gelirlerinin toplanacağı Sistem Cüzdanı (Admin'in cüzdanı)"),
                new SiteSettings("SmtpHost", SettingCategory.Email, "smtp.mailtrap.io", "SMTP Sunucusu (Mailtrap.io ile değiştirin)"),
                new SiteSettings("SmtpPort", SettingCategory.Email, "2525", "SMTP Portu"),
                new SiteSettings("SmtpUser", SettingCategory.Email, "your-mailtrap-user", "SMTP Kullanıcı Adı"),
                new SiteSettings("SmtpPass", SettingCategory.Email, "your-mailtrap-pass", "SMTP Şifresi"),
                new SiteSettings("FromEmail", SettingCategory.Email, "no-reply@sattim.com", "Gönderen E-posta Adresi"),
                new SiteSettings("FromName", SettingCategory.Email, "Sattim Destek", "Gönderen Adı"),
                new SiteSettings("UseSSL", SettingCategory.Email, "true", "SMTP SSL Kullanılsın mı?"),
                new SiteSettings("SiteName", SettingCategory.General, "Sattim.com - Açık Artırma Sitesi", "Sitenin genel adı"),
            };

            var settingsToAdd = allSettings.Where(s => !existingKeys.Contains(s.Key)).ToList();

            if (settingsToAdd.Any())
            {
                logger.LogInformation($"{settingsToAdd.Count} adet yeni Site Ayarı (SiteSettings) oluşturuluyor...");
                await context.SiteSettings.AddRangeAsync(settingsToAdd);
                await context.SaveChangesAsync();
            }
            else
            {
                logger.LogInformation("Tüm Site Ayarları (SiteSettings) zaten güncel.");
            }
        }

        private static async Task SeedEmailTemplatesAsync(ApplicationDbContext context, ILogger logger)
        {
            var existingNamesList = await context.EmailTemplates
                .Select(t => t.Name)
                .ToListAsync();

            var existingNames = new HashSet<string>(existingNamesList);

            var allTemplates = new List<EmailTemplate>
            {
                new EmailTemplate("Welcome", EmailTemplateType.Welcome, "Sattim'a Hoş Geldiniz!",
                    "Sayın {{UserName}},<br>Açık artırma sitemize hoş geldiniz!"),
                new EmailTemplate("PasswordReset", EmailTemplateType.PasswordReset, "Şifre Sıfırlama Talebi",
                    "Şifrenizi sıfırlamak için şu linke tıklayın: <a href='{{ResetLink}}'>Sıfırla</a>"),
                new EmailTemplate("EmailVerification", EmailTemplateType.EmailVerification, "E-postanızı Doğrulayın",
                    "E-postanızı doğrulamak için şu linke tıklayın: <a href='{{ConfirmationLink}}'>Doğrula</a>"),
                new EmailTemplate("AuctionWon", EmailTemplateType.AuctionWon, "İhaleyi Kazandınız!",
                    "Tebrikler {{UserName}}!<br>'{{ProductName}}' adlı ürünü {{FinalPrice}} bedelle kazandınız. Ödeme yapmak için tıklayın: <a href='{{PaymentLink}}'>Ödeme Yap</a>"),
                new EmailTemplate("BidOutbid", EmailTemplateType.BidOutbid, "Teklifiniz Geçildi!",
                    "Sayın {{UserName}},<br>'{{ProductName}}' adlı üründeki teklifiniz geçildi. Yeni fiyat: {{NewPrice}}. Teklifi artırmak için: <a href='{{ProductLink}}'>Tıkla</a>"),
                new EmailTemplate("AuctionEnding", EmailTemplateType.AuctionEnding, "İhale Bitiyor!",
                    "İzlediğiniz '{{ProductName}}' adlı ürünün ihalesi yakında bitiyor. Kaçırmayın! <a href='{{ProductLink}}'>Tıkla</a>"),
                new EmailTemplate("PaymentConfirmation", EmailTemplateType.PaymentConfirmation, "Ödemeniz Alındı",
                    "Sayın {{UserName}},<br>'{{ProductName}}' adlı ürün için {{Amount}} tutarındaki ödemeniz başarıyla alındı."),
                new EmailTemplate("ShippingNotification", EmailTemplateType.ShippingNotification, "Ürününüz Kargolandı!",
                    "Sayın {{UserName}},<br>'{{ProductName}}' adlı ürününüz {{Carrier}} firması ile kargolandı.<br>Takip No: {{TrackingNumber}}")
            };

            var templatesToAdd = allTemplates.Where(t => !existingNames.Contains(t.Name)).ToList();

            if (templatesToAdd.Any())
            {
                logger.LogInformation($"{templatesToAdd.Count} adet yeni E-posta Şablonu (EmailTemplates) oluşturuluyor...");
                await context.EmailTemplates.AddRangeAsync(templatesToAdd);
                await context.SaveChangesAsync();
            }
            else
            {
                logger.LogInformation("Tüm E-posta Şablonları (EmailTemplates) zaten güncel.");
            }
        }

        private static async Task SeedCategoriesAsync(ApplicationDbContext context, ILogger logger)
        {
            bool needsSave = false;

            if (!await context.Categories.AnyAsync(c => c.Slug == "elektronik"))
            {
                logger.LogInformation("'Elektronik' ana kategorisi oluşturuluyor...");
                await context.Categories.AddAsync(new Category("Elektronik", "elektronik", null, "Elektronik cihazlar"));
                needsSave = true;
            }

            if (!await context.Categories.AnyAsync(c => c.Slug == "moda"))
            {
                logger.LogInformation("'Moda' ana kategorisi oluşturuluyor...");
                await context.Categories.AddAsync(new Category("Moda", "moda", null, "Giyim ve Aksesuar"));
                needsSave = true;
            }

            if (needsSave)
            {
                await context.SaveChangesAsync();
            }

            needsSave = false;

            var elektronikCat = await context.Categories.SingleOrDefaultAsync(c => c.Slug == "elektronik");
            var modaCat = await context.Categories.SingleOrDefaultAsync(c => c.Slug == "moda");

            if (elektronikCat != null && !await context.Categories.AnyAsync(c => c.Slug == "cep-telefonlari"))
            {
                logger.LogInformation("'Cep Telefonları' alt kategorisi oluşturuluyor...");
                await context.Categories.AddAsync(new Category("Cep Telefonları", "cep-telefonlari", elektronikCat.Id, "Akıllı telefonlar"));
                needsSave = true;
            }

            if (modaCat != null && !await context.Categories.AnyAsync(c => c.Slug == "giyim"))
            {
                logger.LogInformation("'Giyim' alt kategorisi oluşturuluyor...");
                await context.Categories.AddAsync(new Category("Giyim", "giyim", modaCat.Id, "Kadın/Erkek giyim"));
                needsSave = true;
            }

            if (needsSave)
            {
                await context.SaveChangesAsync();
            }

            if (!needsSave && elektronikCat != null && modaCat != null)
            {
                logger.LogInformation("Tüm temel kategoriler (Categories) zaten güncel.");
            }
        }
    }
}