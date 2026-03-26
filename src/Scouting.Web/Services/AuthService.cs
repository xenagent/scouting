using Microsoft.EntityFrameworkCore;
using Scouting.Web.Domains.UserEntity;
using Scouting.Web.Infrastructure;
using Scouting.Web.Shared;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<AuthUser>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant().Trim(), ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return ServiceResult<AuthUser>.Fail(ErrorCodes.AUTH_INVALID_CREDENTIALS);

        return ServiceResult<AuthUser>.Ok(new AuthUser
        {
            Id = user.Id,
            Username = user.Username!,
            Email = user.Email!,
            Role = user.Role.ToString()
        });
    }

    public async Task<ServiceResult> RegisterAsync(
        string email, string username, string password, CancellationToken ct = default)
    {
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == email.ToLowerInvariant().Trim(), ct);

        if (emailExists)
            return ServiceResult.Fail(ErrorCodes.AUTH_EMAIL_ALREADY_EXISTS);

        var usernameExists = await _db.Users
            .AnyAsync(u => u.Username == username.Trim(), ct);

        if (usernameExists)
            return ServiceResult.Fail(ErrorCodes.AUTH_USERNAME_ALREADY_EXISTS);

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var result = User.Create(email, username, hash);

        if (!result.IsSuccess)
            return ServiceResult.Fail(result.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        await _db.Users.AddAsync(result.Data!, ct);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
