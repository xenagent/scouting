using Microsoft.EntityFrameworkCore;
using Scouting.Web;
using Scouting.Web.Auths;
using Scouting.Web.DbSettings;
using Scouting.Web.Domains.AnalysisEntity;
using Scouting.Web.Domains.AnalysisLikeEntity;
using Scouting.Web.Domains.PlayerEntity;
using Scouting.Web.Domains.ScouterFollowEntity;
using Scouting.Web.Domains.UserEntity;
using Scouting.Web.Domains.VoteEntity;
using Scouting.Web.Services;

namespace Scouting.Web.Infrastructure;

public static class DependencyRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        // Transfermarkt scrape.do HTTP client + sync
        services.Configure<TransfermarktOptions>(
            configuration.GetSection("Transfermarkt"));
        services.AddHttpClient<TransfermarktService>();
        services.AddScoped<ITransfermarktService, TransfermarktService>();
        services.AddSingleton<TmSyncQueue>();
        services.AddHostedService<TransfermarktSyncJob>();

        // DbContext — InMemory for development, PostgreSQL for production
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            if (env.IsDevelopment())
                options.UseInMemoryDatabase("ScoutingDb");
            else
                options.UseNpgsql(configuration.GetConnectionString("Default"));
        });

        // Entity configurations (used by BaseDbContext to auto-map)
        services.AddSingleton<IEntityConfiguration, UserConfiguration>();
        services.AddSingleton<IEntityConfiguration, PlayerConfiguration>();
        services.AddSingleton<IEntityConfiguration, AnalysisConfiguration>();
        services.AddSingleton<IEntityConfiguration, VoteConfiguration>();
        services.AddSingleton<IEntityConfiguration, ScouterFollowConfiguration>();
        services.AddSingleton<IEntityConfiguration, AnalysisLikeConfiguration>();

        // Current user (reads from cookie auth claims via IHttpContextAccessor)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, BlazorCurrentUser>();

        // Business services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IAnalysisService, AnalysisService>();
        services.AddScoped<IVoteService, VoteService>();
        services.AddScoped<IScouterService, ScouterService>();
        services.AddScoped<IFileService, FileService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IAIAnalysisService, StubAIAnalysisService>();
        services.AddScoped<IAnalysisLikeService, AnalysisLikeService>();

        return services;
    }
}
