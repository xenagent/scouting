using Microsoft.EntityFrameworkCore;
using scommon;
using scommon.Auths;
using scommon.DbSettings;
using Scouting.API.Domains.AnalysisEntity;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Domains.ScouterFollowEntity;
using Scouting.API.Domains.UserEntity;
using Scouting.API.Domains.VoteEntity;

namespace Scouting.API.Infrastructure;

public class AppDbContext : BaseDbContext<AppDbContext>
{
    public AppDbContext(
        IServiceProvider serviceProvider,
        DbContextOptions<AppDbContext> options,
        ICurrentUser currentUser)
        : base(serviceProvider, options, currentUser)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<ScouterFollow> ScouterFollows => Set<ScouterFollow>();
}
