using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scouting.Web;
using Scouting.Web.DbSettings;
using Scouting.Web.Infrastructure;

namespace Scouting.Web.Domains.VoteEntity;

public class VoteConfiguration : BaseConfiguration<Vote>
{
    public override void Map(EntityTypeBuilder<Vote> model)
    {
        model.Property(e => e.VoteType)
            .HasMaxLength(8)
            .HasConversion(e => e.ToString(), e => Enum.Parse<VoteType>(e))
            .IsRequired();
        base.Map(model);
    }

    public override string GetSchemaName() => DbSchemaNames.Scouting;
    public override string GetTableName() => nameof(Vote);
}
