namespace Scouting.Web.Domains.UserEntity;

public enum UserLevel
{
    Starter = 0,
    Mid     = 1,
    Senior  = 2,
    Expert  = 3
}

public static class UserLevelExtensions
{
    // Points = ApprovedAnalysisCount * 5 + TotalLikesReceived + BonusPoints
    // Starter:   0– 24  (~0–4 analyses)
    // Mid:      25– 99  (~5–19 analyses)
    // Senior:  100–249  (~20–49 analyses)
    // Expert:  250+     (~50+ analyses)
    public static UserLevel FromPoints(int points) => points switch
    {
        >= 250 => UserLevel.Expert,
        >= 100 => UserLevel.Senior,
        >= 25  => UserLevel.Mid,
        _      => UserLevel.Starter
    };

    /// <summary>Bir sonraki seviye için gereken minimum puan (Expert için MaxValue).</summary>
    public static int NextThreshold(this UserLevel level) => level switch
    {
        UserLevel.Starter => 25,
        UserLevel.Mid     => 100,
        UserLevel.Senior  => 250,
        _                 => int.MaxValue
    };

    /// <summary>Bu seviyenin alt sınırı.</summary>
    public static int PrevThreshold(this UserLevel level) => level switch
    {
        UserLevel.Mid    => 25,
        UserLevel.Senior => 100,
        UserLevel.Expert => 250,
        _                => 0
    };

    public static string NextLevelName(this UserLevel level) => level switch
    {
        UserLevel.Starter => "Mid",
        UserLevel.Mid     => "Senior",
        UserLevel.Senior  => "Expert",
        _                 => "—"
    };

    public static string DisplayName(this UserLevel level) => level switch
    {
        UserLevel.Mid    => "Mid",
        UserLevel.Senior => "Senior",
        UserLevel.Expert => "Expert",
        _                => "Starter"
    };

    public static string BadgeClass(this UserLevel level) => level switch
    {
        UserLevel.Expert => "badge bg-yellow-900/60 text-yellow-400 border border-yellow-700/40",
        UserLevel.Senior => "badge bg-violet-900/60 text-violet-300 border border-violet-700/40",
        UserLevel.Mid    => "badge bg-pitch-900/60 text-pitch-300 border border-pitch-700/40",
        _                => "badge bg-scout-800 text-scout-400"
    };
}
