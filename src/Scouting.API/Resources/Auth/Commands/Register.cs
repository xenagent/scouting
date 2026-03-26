using FluentValidation;
using scommon;
using Scouting.API.Domains.UserEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Shared;
using Scouting.API.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Scouting.API.Resources.Auth.Commands;

public static class Register
{
    public class Request : Command<FeatureObjectResultModel<Response>>
    {
        public string Email { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class Response
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .EmailAddress().WithErrorCode(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE)
                .MaximumLength(256).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);

            RuleFor(x => x.Username)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MinimumLength(3).WithErrorCode(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE)
                .MaximumLength(32).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR)
                .Matches(@"^[a-zA-Z0-9_]+$").WithErrorCode(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE);

            RuleFor(x => x.Password)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MinimumLength(6).WithErrorCode(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE)
                .MaximumLength(128).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);
        }
    }

    public class Handler : ICommandHandler<Request, FeatureObjectResultModel<Response>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FeatureObjectResultModel<Response>> HandleAsync(Request request, CancellationToken ct)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email.ToLowerInvariant().Trim(), ct);

            if (emailExists)
                return FeatureObjectResultModel<Response>.Error(new MessageItem
                {
                    Code = ErrorCodes.AUTH_EMAIL_ALREADY_EXISTS,
                    Property = nameof(request.Email),
                    Table = nameof(User)
                });

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username.Trim(), ct);

            if (usernameExists)
                return FeatureObjectResultModel<Response>.Error(new MessageItem
                {
                    Code = ErrorCodes.AUTH_USERNAME_ALREADY_EXISTS,
                    Property = nameof(request.Username),
                    Table = nameof(User)
                });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var result = User.Create(request.Email, request.Username, passwordHash);

            if (!result.IsSuccess)
                return FeatureObjectResultModel<Response>.Error(result.Messages!);

            await _context.Users.AddAsync(result.Data!, ct);
            await _context.SaveChangesAsync(ct);

            return FeatureObjectResultModel<Response>.Ok(new Response
            {
                Id = result.Data!.Id,
                Username = result.Data.Username!,
                Email = result.Data.Email!
            });
        }
    }
}
