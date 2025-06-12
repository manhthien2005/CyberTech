using CyberTech.Data;
using CyberTech.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CyberTech.Services.PaymentProcessors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
});

// Configure Data Protection
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .SetApplicationName("CyberTech")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    // Railway sử dụng PostgreSQL
    if (builder.Environment.IsProduction())
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
    
    options.EnableSensitiveDataLogging(false)
           .EnableServiceProviderCaching(true)
           .EnableDetailedErrors(false)
           .ConfigureWarnings(warnings =>
               warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
});
// Configure Dependency Injection
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRecaptchaService, RecaptchaService>();
builder.Services.AddScoped<IVoucherTokenService, VoucherTokenService>();
builder.Services.AddScoped<VNPayService>();
builder.Services.AddScoped<IRankService, RankService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<IPaymentProcessor, CODPaymentProcessor>();
builder.Services.AddScoped<IPaymentProcessor, VNPayPaymentProcessor>();
builder.Services.AddScoped<IChatLogService, ChatLogService>();

builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// Add email template service
builder.Services.AddScoped<EmailTemplateService>();

// Add background services
builder.Services.AddHostedService<StockNotificationBackgroundService>();

// Configure Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.CorrelationCookie.HttpOnly = true;
    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            var identity = (ClaimsIdentity)context.Principal.Identity;
            var picture = context.User.GetProperty("picture").GetString();
            if (!string.IsNullOrEmpty(picture))
            {
                identity.AddClaim(new Claim("picture", picture));
            }
        },
        OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/Account/Login?error=" + context.Failure.Message);
            return Task.CompletedTask;
        }
    };
})
.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    options.CallbackPath = "/signin-facebook";
    options.Fields.Add("email");
    options.Fields.Add("name");
    options.Fields.Add("picture");
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.CorrelationCookie.HttpOnly = true;
    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            var identity = (ClaimsIdentity)context.Principal.Identity;
            var picture = context.User.GetProperty("picture").GetProperty("data").GetProperty("url").GetString();
            if (!string.IsNullOrEmpty(picture))
            {
                identity.AddClaim(new Claim("picture", picture));
            }
        },
        OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/Account/Login?error=" + context.Failure.Message);
            return Task.CompletedTask;
        }
    };
});

// Add response caching
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024; // 1MB
    options.UseCaseSensitivePaths = false;
});

// Add memory cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure Railway port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Tắt HTTPS redirection khi chạy trên Railway
    // app.UseHsts();
}

// Tắt HTTPS redirection khi chạy trên Railway
// app.UseHttpsRedirection();

// Configure MIME types
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx =>
    {
        // Add security headers
        ctx.Context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    },
    ContentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".js"] = "application/javascript"
        }
    }
});
app.UseResponseCaching();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Login}/{id?}",
    defaults: new { controller = "Admin" }
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();