using Microsoft.EntityFrameworkCore.Metadata.Builders;
using scommon;
using Scouting.API.Infrastructure;

namespace Scouting.API.Domains.ScouterFollowEntity;

public class ScouterFollowConfiguration : BaseConfiguration<ScouterFollow>
{
    public override void Map(EntityTypeBuilder<ScouterFollow> model)
    {
        model.HasIndex(e => new { e.FollowerId, e.ScouterId }).IsUnique();
        model.HasIndex(e => e.ScouterId);
        model.HasIndex(e => e.FollowerId);

        base.Map(model);
    }

    public override string GetSchemaName() => DbSchemaNames.Scouting;
    public override string GetTableName() => nameof(ScouterFollow);
}
