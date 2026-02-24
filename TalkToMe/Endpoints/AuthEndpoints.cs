using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using TalkToMe.Shared.IService;

namespace TalkToMe.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var auth = app.MapGroup("/auth").WithTags("Auth");

            auth.MapGet("/sign-in", (string? returnUrl) =>
                Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}"));

            auth.MapPost("/sign-in", async (HttpContext ctx, IAuthADService authAD) =>
            {
                string? username = null, password = null, returnUrl = null;

                if (ctx.Request.HasJsonContentType())
                {
                    try
                    {
                        using var doc = await System.Text.Json.JsonDocument.ParseAsync(ctx.Request.Body);

                        var root = doc.RootElement;

                        username = root.TryGetProperty("Username", out var u) ? u.GetString() : null;
                        password = root.TryGetProperty("Password", out var p) ? p.GetString() : null;
                        returnUrl = root.TryGetProperty("ReturnUrl", out var r) ? r.GetString() : null;
                    }
                    catch
                    {
                        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}&error=badpayload");
                    }
                }
                else if (ctx.Request.HasFormContentType)
                {
                    var form = await ctx.Request.ReadFormAsync();

                    username = form["Username"];
                    password = form["Password"];
                    returnUrl = form["ReturnUrl"];

                }
                else
                {
                    var fallback = "/";
                    return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl ?? fallback)}&error=unsupported");
                }

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}&error=badcredentials");

                try
                {
                    var auth = await authAD.LoginADAsync(username, password);
                    if (auth == null)
                        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}&error=badcredentials");

                    var midnight = DateTime.UtcNow.Date.AddDays(1);
                    var expires = new DateTimeOffset(midnight);

                    var sessionId = Guid.NewGuid().ToString("N");

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, auth.Name),
                        new Claim(ClaimTypes.NameIdentifier, username),
                        new Claim("empId", auth.EmpId),
                        new Claim("department", auth.Department)
                    };

                    var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                         new ClaimsPrincipal(id),
                         new AuthenticationProperties
                         {
                             IsPersistent = true,
                             ExpiresUtc = expires,
                             AllowRefresh = false
                         });

                    static string Safe(string? url)
                    {
                        if (string.IsNullOrWhiteSpace(url)) return "/";
                        if (Uri.TryCreate(url, UriKind.Relative, out _))
                        {
                            var p = url.StartsWith("/") ? url : "/" + url;
                            return p.StartsWith("/login", StringComparison.OrdinalIgnoreCase) ? "/" : p;
                        }
                        return "/";
                    }

                    var target = Safe(returnUrl);

                    if (!ctx.Request.HasJsonContentType())
                        return Results.Redirect(target);

                    return Results.Ok(new { redirect = target, displayName = auth.Name, empId = auth.EmpId, department = auth.Department });
                }
                catch
                {
                    return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}&error=internal");
                }
            })
            .WithDisplayName("AuthSignInPost")
            .DisableAntiforgery()
            .RequireRateLimiting("login");

            auth.MapPost("/sign-out", async (HttpContext ctx) =>
            {
                await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login?info=signedout");
            })
            .RequireAuthorization()
            .WithDisplayName("AuthSignOutPost");

            return app;

        }
    }
}
