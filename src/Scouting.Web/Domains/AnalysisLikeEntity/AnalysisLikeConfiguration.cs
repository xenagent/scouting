using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scouting.Web;
using Scouting.Web.DbSettings;
using Scouting.Web.Infrastructure;

namespace Scouting.Web.Domains.AnalysisLikeEntity;

public class AnalysisLikeConfiguration : BaseConfiguration<AnalysisLike>
{
    public void Configure(ModelBuilder modelBuilder)
    {
       
    }
    public override string GetSchemaName() => DbSchemaNames.Scouting;
    public override string GetTableName() => nameof(AnalysisLike);
}
