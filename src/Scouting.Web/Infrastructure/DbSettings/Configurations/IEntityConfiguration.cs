namespace Scouting.Web.DbSettings
{
    public interface IEntityConfiguration : ITransientDependency
    {
        string GetTableName();
        string GetSchemaName();
    }
}
