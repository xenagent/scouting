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
    // Points = ApprovedAnalysisCount * 10 + TotalLikesReceived
    // Starter: 0–49   (~0–4 analyses)
    // Mid:    50–199   (~5–19 analyses)
    // Senior: 200–499  (~20–49 analyses)
    // Expert: 500+     (~50+ analyses)
    public static UserLevel FromPoints(int points) => points switch
    {
        >= 500 => UserLevel.Expert,
        >= 200 => UserLevel.Senior,
        >= 50  => UserLevel.Mid,
        _      => UserLevel.Starter
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
