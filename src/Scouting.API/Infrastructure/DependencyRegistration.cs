using Microsoft.EntityFrameworkCore;
using scommon;
using scommon.Auths;
using Scouting.API.Domains.AnalysisEntity;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Domains.ScouterFollowEntity;
using Scouting.API.Domains.UserEntity;
using Scouting.API.Domains.VoteEntity;
using Scouting.API.Services;

namespace Scouting.API.Infrastructure;

public static class DependencyRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));
        });

        // Register entity configurations
        services.AddSingleton<IEntityConfiguration, UserConfiguration>();
        services.AddSingleton<IEntityConfiguration, PlayerConfiguration>();
        services.AddSingleton<IEntityConfiguration, AnalysisConfiguration>();
        services.AddSingleton<IEntityConfiguration, VoteConfiguration>();
        services.AddSingleton<IEntityConfiguration, ScouterFollowConfiguration>();

        // Current user
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Mediator
        services.AddMediator(options =>
        {
            options.AddHandlersFromAssemblyOf<AppDbContext>();
        });

        return services;
    }
}
