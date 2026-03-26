using Microsoft.EntityFrameworkCore;
using scommon;
using scommon.Auths;
using scommon.DbSettings;
using Scouting.Web.Domains.AnalysisEntity;
using Scouting.Web.Domains.PlayerEntity;
using Scouting.Web.Domains.ScouterFollowEntity;
using Scouting.Web.Domains.UserEntity;
using Scouting.Web.Domains.VoteEntity;

namespace Scouting.Web.Infrastructure;

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
