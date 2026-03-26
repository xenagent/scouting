using Microsoft.EntityFrameworkCore.Metadata.Builders;
using scommon;
using Scouting.API.Infrastructure;

namespace Scouting.API.Domains.UserEntity;

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

        model.HasIndex(e => e.Email).IsUnique();
        model.HasIndex(e => e.Username).IsUnique();

        base.Map(model);
    }

    public override string GetSchemaName() => DbSchemaNames.Scouting;
    public override string GetTableName() => nameof(User);
}
