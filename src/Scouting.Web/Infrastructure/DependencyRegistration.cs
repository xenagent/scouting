using Microsoft.EntityFrameworkCore;
using scommon;
using scommon.Auths;
using Scouting.Web.Domains.AnalysisEntity;
using Scouting.Web.Domains.PlayerEntity;
using Scouting.Web.Domains.ScouterFollowEntity;
using Scouting.Web.Domains.UserEntity;
using Scouting.Web.Domains.VoteEntity;
using Scouting.Web.Services;

namespace Scouting.Web.Infrastructure;

public static class DependencyRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<AppDbContext>((provider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Entity configurations (used by BaseDbContext to auto-map)
        services.AddSingleton<IEntityConfiguration, UserConfiguration>();
        services.AddSingleton<IEntityConfiguration, PlayerConfiguration>();
        services.AddSingleton<IEntityConfiguration, AnalysisConfiguration>();
        services.AddSingleton<IEntityConfiguration, VoteConfiguration>();
        services.AddSingleton<IEntityConfiguration, ScouterFollowConfiguration>();

        // Current user (reads from cookie auth claims via IHttpContextAccessor)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, BlazorCurrentUser>();

        // Business services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IAnalysisService, AnalysisService>();
        services.AddScoped<IVoteService, VoteService>();
        services.AddScoped<IScouterService, ScouterService>();

        return services;
    }
}
