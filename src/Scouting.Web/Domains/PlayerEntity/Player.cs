using scommon;
using Scouting.Web.Shared;

namespace Scouting.Web.Domains.PlayerEntity;

public class Player : BaseUserTrackModel
{
    private Player() { }

    public string? Name { get; private set; }
    public string? Slug { get; private set; }
    public int Age { get; private set; }
    public PlayerPosition Position { get; private set; }
    public string? Team { get; private set; }
    public string? League { get; private set; }
    public string? Country { get; private set; }
    public string? ImageUrl { get; private set; }
    public PlayerStatus Status { get; private set; } = PlayerStatus.Pending;
    public int Score { get; private set; }
    public string? RejectionReason { get; private set; }

    public static ResultDomain<Player> Create(
        string name, int age, PlayerPosition position,
        string team, string league, string country, Guid suggestedByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ResultDomain<Player>.Error(new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY, Property = nameof(name), Table = nameof(Player) });

        if (name.Length > 128)
            return ResultDomain<Player>.Error(new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR, Property = nameof(name), Table = nameof(Player) });

        if (age < 13 || age > 50)
            return ResultDomain<Player>.Error(new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_INVALID_VALUE, Property = nameof(age), Table = nameof(Player) });

        var id = Guid.NewGuid();
        return ResultDomain<Player>.Ok(new Player
        {
            Id = id,
            Name = name.Trim(),
            Slug = GenerateSlug(name, id),
            Age = age,
            Position = position,
            Team = team.Trim(),
            League = league.Trim(),
            Country = country.Trim(),
            Status = PlayerStatus.Pending,
            CreatedUserId = suggestedByUserId
        });
    }

    public ResultDomain Approve()
    {
        if (Status == PlayerStatus.Approved)
            return ResultDomain.Error(new MessageItem { Code = ErrorCodes.PLAYER_ALREADY_APPROVED, Property = nameof(Status), Table = nameof(Player) });
        Status = PlayerStatus.Approved;
        RejectionReason = null;
        return ResultDomain.Ok();
    }

    public ResultDomain Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ResultDomain.Error(new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY, Property = nameof(reason), Table = nameof(Player) });
        Status = PlayerStatus.Rejected;
        RejectionReason = reason;
        return ResultDomain.Ok();
    }

    public void UpdateScore(int delta) => Score += delta;
    public void SetImageUrl(string? imageUrl) => ImageUrl = imageUrl;

    private static string GenerateSlug(string name, Guid id)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
            .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return $"{slug}-{id.ToString("N")[..8]}";
    }
}
