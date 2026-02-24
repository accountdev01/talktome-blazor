using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using TalkToMe.Client.Services;
using TalkToMe.Components;
using TalkToMe.Endpoints;
using TalkToMe.Shared;
using TalkToMe.Shared.Data;
using TalkToMe.Shared.IService;
using TalkToMe.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

ICryptographyService cryptographyService = new CryptographyService(builder.Configuration);

string rawDefaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(rawDefaultConn))
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing or empty.");

string decryptedConn = cryptographyService.Unprotect(rawDefaultConn);

builder.Services.AddDbContext<TalkToMeContext>(options => options.UseSqlServer(decryptedConn));

builder.Services.AddDataServices();
builder.Services.AddHostedService<AutomatedSystem>();

builder.Services.AddControllers();
builder.Services.AddScoped<ToastService>();

builder.Services.AddLocalization();
var supportedCultures = new[] { "th", "en" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
{
    o.Cookie.Name = ".TalkToMe.Auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.LoginPath = "/login";
    o.LogoutPath = "/logout";
    o.AccessDeniedPath = "/login";
    o.SlidingExpiration = false;
    o.ExpireTimeSpan = TimeSpan.FromHours(10);

    o.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/auth"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };

});

builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddRateLimiter(_ => _
    .AddPolicy("login", httpContext =>
    {
        static string? GetClientIp(HttpContext ctx)
        {
            var xff = ctx.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(xff))
            {
                var first = xff.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(first)) return first;
            }
            return ctx.Connection.RemoteIpAddress?.ToString();
        }

        var ip = GetClientIp(httpContext) ?? "unknown-ip";
        return RateLimitPartition.GetSlidingWindowLimiter(
            ip,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }));


var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

var logFolder = Path.Combine(AppContext.BaseDirectory, "logs");
if (!Directory.Exists(logFolder))
{
    Directory.CreateDirectory(logFolder);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRequestLocalization();

app.UseHttpsRedirection();

app.UseRouting();
app.UseAntiforgery();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TalkToMe.Client._Imports).Assembly);

app.MapAuthEndpoints();

app.Run();
