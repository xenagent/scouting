using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using scommon;
using Scouting.Web.Infrastructure;

namespace Scouting.Web.Domains.AnalysisLikeEntity;

public class AnalysisLikeConfiguration : IEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalysisLike>(b =>
        {
            b.ToTable("analysis_likes", DbSchemaNames.Scouting);
            b.HasIndex(l => new { l.AnalysisId, l.UserId }).IsUnique();
        });
    }
}
