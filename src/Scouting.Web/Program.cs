using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Scouting.Web.Components;
using Scouting.Web.Infrastructure;
using Scouting.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (DB, services) ────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Auth (cookie-based) ──────────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAntiforgery();

// ── Blazor ───────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ── Minimal API — Login / Logout / Register ──────────────────────────────────

// POST /account/login  (form post from login page)
app.MapPost("/account/login", async (
    HttpContext ctx,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string? returnUrl,
    IAuthService authService) =>
{
    var result = await authService.ValidateCredentialsAsync(email, password);

    if (!result.IsSuccess)
        return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");

    var user = result.Data!;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.Role)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
        new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

    return Results.Redirect(returnUrl ?? "/");
}).DisableAntiforgery(); // Blazor form ile gönderildiğinde DisableAntiforgery kullanılır

// POST /account/register
app.MapPost("/account/register", async (
    HttpContext ctx,
    [FromForm] string email,
    [FromForm] string username,
    [FromForm] string password,
    IAuthService authService) =>
{
    var result = await authService.RegisterAsync(email, username, password);

    if (!result.IsSuccess)
        return Results.Redirect($"/register?error={Uri.EscapeDataString(result.ErrorCode ?? "unknown")}");

    // Auto-login after register
    var userResult = await authService.ValidateCredentialsAsync(email, password);
    if (userResult.IsSuccess)
    {
        var user = userResult.Data!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
    }

    return Results.Redirect("/");
}).DisableAntiforgery();

// GET /account/logout
app.MapGet("/account/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

// ── Blazor ───────────────────────────────────────────────────────────────────
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
