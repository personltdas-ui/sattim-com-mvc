using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sattim.Web.Models.User;
using Sattim.Web.Services.Account;

using Sattim.Web.Services.Bid;
using Sattim.Web.Services.Blog;
using Sattim.Web.Services.Content;
using Sattim.Web.Services.Dispute;
using Sattim.Web.Services.Email;

using Sattim.Web.Services.Management;
using Sattim.Web.Services.Moderation;
using Sattim.Web.Services.Notification;
using Sattim.Web.Services.Order;
using Sattim.Web.Services.Payment;
using Sattim.Web.Services.Product;
using Sattim.Web.Services.Profile;
using Sattim.Web.Services.Report;
using Sattim.Web.Services.Repositories;
using Sattim.Web.Services.Shipping;
using Sattim.Web.Services.Storage;
using Sattim.Web.Services.Wallet;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    });


// AutoMapper — tüm assembly'leri otomatik tara
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();



builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});





builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IModerationService, ModerationService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IBlogService, BlogService>();

builder.Services.AddScoped<IFileStorageService, LocalStorageService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IGatewayService, IyzicoGatewayService>();


// =================================================================
// REPOSITORY KATMANI KAYITLARI (DI)
// =================================================================

// 1. Jenerik (Generic) Repository'yi kaydet
// (Birisi IGenericRepository<Wallet> isterse, ona GenericRepository<Wallet> ver)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// 2. Özel (Specific) Repository'leri kaydet
// (Her arayüzün kendi somut sınıfına eşlenmesi)

// Product
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// User
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Bid
builder.Services.AddScoped<IBidRepository, BidRepository>();

// Blog
builder.Services.AddScoped<IBlogPostRepository, BlogPostRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IBlogCommentRepository, BlogCommentRepository>();

// Dispute & Report
builder.Services.AddScoped<IDisputeRepository, DisputeRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// Management & Dashboard
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

// Order & Shipping
builder.Services.AddScoped<IEscrowRepository, EscrowRepository>();
builder.Services.AddScoped<IShippingRepository, ShippingRepository>();

// Wallet
builder.Services.AddScoped<IWalletRepository, WalletRepository>();

// Notification
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();











builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<UrlEncoder>(UrlEncoder.Default);


builder.Services.AddMemoryCache();



builder.Services.AddHttpClient();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});



var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Veritabanı tohumlama (seeding) sırasında ölümcül bir hata oluştu.");
        throw; 
    }
}



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
});


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}/{id?}");





app.Run();