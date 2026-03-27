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
    public int BonusPoints { get; private set; }        // market değeri, transfer, istatistik bonusları
    public int FollowerCount { get; private set; }

    /// <summary>
    /// Toplam puan: her analiz 5p + her beğeni 1p + bonus puanlar.
    /// Seviye bu değere göre hesaplanır.
    /// </summary>
    public int TotalPoints => ApprovedAnalysisCount * 5 + TotalLikesReceived + BonusPoints;

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

    /// <summary>
    /// Keşif/sonuç bonusu ekler: market değeri artışı, transfer, istatistik iyileşmesi.
    /// Negatif değer verilebilir (market değeri düşüşü gibi cezalar).
    /// </summary>
    public void AddBonusPoints(int points)
    {
        BonusPoints += points;
        if (BonusPoints < 0) BonusPoints = 0;
        RecalculateLevel();
    }

    public void IncrementFollowerCount() => FollowerCount++;
    public void DecrementFollowerCount() { if (FollowerCount > 0) FollowerCount--; }

    private void RecalculateLevel()
    {
        Level = UserLevelExtensions.FromPoints(TotalPoints);
    }
}
