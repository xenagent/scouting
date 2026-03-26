using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using scommon;
using Scouting.API.Domains.UserEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Shared;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Auth.Commands;

public static class Login
{
    public class Request : Command<FeatureObjectResultModel<Response>>
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class Response
    {
        public string Token { get; set; } = default!;
        public Guid UserId { get; set; }
        public string Username { get; set; } = default!;
        public string Role { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .EmailAddress().WithErrorCode(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE);

            RuleFor(x => x.Password)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY);
        }
    }

    public class Handler : ICommandHandler<Request, FeatureObjectResultModel<Response>>
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public Handler(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<FeatureObjectResultModel<Response>> HandleAsync(Request request, CancellationToken ct)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant().Trim(), ct);

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return FeatureObjectResultModel<Response>.Error(new MessageItem
                {
                    Code = ErrorCodes.AUTH_INVALID_CREDENTIALS,
                    Property = nameof(request.Email),
                    Table = nameof(User)
                });

            var token = GenerateToken(user);

            return FeatureObjectResultModel<Response>.Ok(new Response
            {
                Token = token.Token,
                UserId = user.Id,
                Username = user.Username!,
                Role = user.Role.ToString(),
                ExpiresAt = token.ExpiresAt
            });
        }

        private (string Token, DateTime ExpiresAt) GenerateToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
            var expiresAt = DateTime.UtcNow.AddDays(7);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.Username!),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
