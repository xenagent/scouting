using Microsoft.EntityFrameworkCore.Metadata.Builders;
using scommon;
using Scouting.Web.Infrastructure;

namespace Scouting.Web.Domains.PlayerEntity;

public class PlayerConfiguration : BaseConfiguration<Player>
{
    public override void Map(EntityTypeBuilder<Player> model)
    {
        model.Property(e => e.Name).HasMaxLength(128).IsRequired();
        model.Property(e => e.Slug).HasMaxLength(160).IsRequired();
        model.Property(e => e.Team).HasMaxLength(128).IsRequired();
        model.Property(e => e.League).HasMaxLength(128).IsRequired();
        model.Property(e => e.Country).HasMaxLength(64).IsRequired();
        model.Property(e => e.ImageUrl).HasMaxLength(512);
        model.Property(e => e.RejectionReason).HasMaxLength(512);
        model.Property(e => e.Position)
            .HasMaxLength(8)
            .HasConversion(e => e.ToString(), e => Enum.Parse<PlayerPosition>(e))
            .IsRequired();
        model.Property(e => e.Status)
            .HasMaxLength(16)
            .HasConversion(e => e.ToString(), e => Enum.Parse<PlayerStatus>(e))
            .IsRequired();
        model.Property(e => e.TransfermarktId).HasMaxLength(32);
        model.Property(e => e.TransfermarktUrl).HasMaxLength(512);
        model.Property(e => e.Height).HasMaxLength(16);
        model.Property(e => e.PreferredFoot).HasMaxLength(16);
        model.Property(e => e.SeasonStatsJson).HasColumnType("text");
        model.HasIndex(e => e.Slug).IsUnique();
        model.HasIndex(e => e.TransfermarktId).IsUnique().HasFilter("\"TransfermarktId\" IS NOT NULL");
        model.HasIndex(e => e.Status);
        model.HasIndex(e => e.Score);
        base.Map(model);
    }

    public override string GetSchemaName() => DbSchemaNames.Scouting;
    public override string GetTableName() => nameof(Player);
}
