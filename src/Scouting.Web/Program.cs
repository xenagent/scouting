using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Scouting.Web.Components;
using Scouting.Web.Infrastructure;
using Scouting.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (DB, services) ────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// ── Auth (cookie for Blazor pages + JWT for API endpoints) ───────────────────
var jwtService = new JwtService(builder.Configuration);

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
    })
    .AddJwtBearer(options =>
    {
        var p = jwtService.GetValidationParameters();
        options.TokenValidationParameters = p;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAntiforgery();

// ── Blazor ───────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Seed data (only in development, InMemory DB)
if (app.Environment.IsDevelopment())
    await SeedData.SeedAsync(app.Services);

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

// ── Minimal API — Login / Logout / Register ───────────────────────────────────

// POST /account/login  (form post from Blazor login page)
app.MapPost("/account/login", async (
    HttpContext ctx,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string? returnUrl,
    IAuthService authService) =>
{
    var result = await authService.ValidateCredentialsAsync(email, password);
    if (!result.IsSuccess)
        return Results.Redirect($"/login?error=invalid");

    var user = result.Data!;
    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        BuildPrincipal(user),
        new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

    return Results.Redirect(returnUrl ?? "/");
}).DisableAntiforgery();

// POST /account/register  (form post from Blazor register page)
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

    // Auto-login after successful register
    var userResult = await authService.ValidateCredentialsAsync(email, password);
    if (userResult.IsSuccess)
    {
        await ctx.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            BuildPrincipal(userResult.Data!),
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

// ── Minimal API — JWT Token ───────────────────────────────────────────────────

// POST /api/auth/token  → returns JWT bearer token (for programmatic / mobile clients)
app.MapPost("/api/auth/token", async (
    [FromBody] TokenRequest req,
    IAuthService authService,
    IJwtService jwtSvc) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Email ve şifre gereklidir." });

    var result = await authService.ValidateCredentialsAsync(req.Email, req.Password);
    if (!result.IsSuccess)
        return Results.Unauthorized();

    var user = result.Data!;
    var token = jwtSvc.GenerateToken(user);

    return Results.Ok(new
    {
        token,
        expires = DateTime.UtcNow.AddDays(7),
        user = new { user.Id, user.Username, user.Email, user.Role }
    });
}).DisableAntiforgery();

// ── Blazor ───────────────────────────────────────────────────────────────────
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────
static ClaimsPrincipal BuildPrincipal(AuthUser user)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.Role)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    return new ClaimsPrincipal(identity);
}

// ── Request models ────────────────────────────────────────────────────────────
record TokenRequest(string Email, string Password);
