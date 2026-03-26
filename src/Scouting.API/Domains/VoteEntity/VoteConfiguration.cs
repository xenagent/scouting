using Microsoft.EntityFrameworkCore.Metadata.Builders;
using scommon;
using Scouting.API.Infrastructure;

namespace Scouting.API.Domains.VoteEntity;

public class VoteConfiguration : BaseConfiguration<Vote>
{
    public override void Map(EntityTypeBuilder<Vote> model)
    {
        model.Property(e => e.VoteType)
            .HasMaxLength(8)
            .HasConversion(e => e.ToString(), e => Enum.Parse<VoteType>(e))
            .IsRequired();

        model.HasIndex(e => new { e.PlayerId, e.UserId }).IsUnique();

        base.Map(model);
    }

    public override string GetSchemaName() => DbSchemaNames.Scouting;
    public override string GetTableName() => nameof(Vote);
}
