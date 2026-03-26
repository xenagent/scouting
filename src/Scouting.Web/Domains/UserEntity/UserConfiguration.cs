using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scouting.Web;
using Scouting.Web.DbSettings;
using Scouting.Web.Infrastructure;

namespace Scouting.Web.Domains.UserEntity;

public class UserConfiguration : BaseConfiguration<User>
{
    public override void Map(EntityTypeBuilder<User> model)
    {
        model.Property(e => e.Email).HasMaxLength(256).IsRequired();
        model.Property(e => e.Username).HasMaxLength(32).IsRequired();
        model.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
        model.Property(e => e.Bio).HasMaxLength(512);
        model.Property(e => e.AvatarUrl).HasMaxLength(512);
        model.Property(e => e.Role)
            .HasMaxLength(16)
            .HasConversion(e => e.ToString(), e => Enum.Parse<UserRole>(e))
            .IsRequired();

        base.Map(model);
    }

    public override string GetSchemaName() => DbSchemaNames.Scouting;
    public override string GetTableName() => nameof(User);
}
