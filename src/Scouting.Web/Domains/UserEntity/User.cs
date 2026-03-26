using Scouting.Web;
using Scouting.Web.Shared;

namespace Scouting.Web.Domains.UserEntity;

public class User : BaseModel
{
    private User() { }

    public string? Email { get; private set; }
    public string? Username { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;
    public UserLevel Level { get; private set; } = UserLevel.Starter;
    public int ApprovedAnalysisCount { get; private set; }
    public int TotalLikesReceived { get; private set; }
    public int FollowerCount { get; private set; }

    public static ResultDomain<User> Create(string email, string username, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ResultDomain<User>.Error(new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY, Property = nameof(email), Table = nameof(User) });

        if (string.IsNullOrWhiteSpace(username))
            return ResultDomain<User>.Error(new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY, Property = nameof(username), Table = nameof(User) });

        if (username.Length > 32)
            return ResultDomain<User>.Error(new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR, Property = nameof(username), Table = nameof(User) });

        return ResultDomain<User>.Ok(new User
        {
            Email = email.ToLowerInvariant().Trim(),
            Username = username.Trim(),
            PasswordHash = passwordHash,
            Role = UserRole.User
        });
    }

    public void SetAvatar(string url) => AvatarUrl = url;
    public void MakeAdmin() => Role = UserRole.Admin;

    public void IncrementApprovedAnalysisCount()
    {
        ApprovedAnalysisCount++;
        RecalculateLevel();
    }

    public void IncrementLikesReceived()
    {
        TotalLikesReceived++;
        RecalculateLevel();
    }

    public void DecrementLikesReceived()
    {
        if (TotalLikesReceived > 0) TotalLikesReceived--;
        RecalculateLevel();
    }

    public void IncrementFollowerCount() => FollowerCount++;
    public void DecrementFollowerCount() { if (FollowerCount > 0) FollowerCount--; }

    private void RecalculateLevel()
    {
        var points = ApprovedAnalysisCount * 10 + TotalLikesReceived;
        Level = UserLevelExtensions.FromPoints(points);
    }
}

