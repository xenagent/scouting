using Microsoft.EntityFrameworkCore.Metadata.Builders;
using scommon;
using Scouting.API.Infrastructure;

namespace Scouting.API.Domains.AnalysisEntity;

public class AnalysisConfiguration : BaseConfiguration<Analysis>
{
    public override void Map(EntityTypeBuilder<Analysis> model)
    {
        model.Property(e => e.VideoUrl).HasMaxLength(512).IsRequired();
        model.Property(e => e.Content).HasMaxLength(5000).IsRequired();
        model.Property(e => e.AISummary).HasMaxLength(1024);
        model.Property(e => e.RejectionReason).HasMaxLength(512);
        model.Property(e => e.AIScore).HasPrecision(4, 2);
        model.Property(e => e.Status)
            .HasMaxLength(16)
            .HasConversion(e => e.ToString(), e => Enum.Parse<AnalysisStatus>(e))
            .IsRequired();

        model.HasIndex(e => e.PlayerId);
        model.HasIndex(e => e.Status);
        model.HasIndex(e => e.CreatedUserId);

        base.Map(model);
    }

    public override string GetSchemaName() => DbSchemaNames.Scouting;
    public override string GetTableName() => nameof(Analysis);
}
