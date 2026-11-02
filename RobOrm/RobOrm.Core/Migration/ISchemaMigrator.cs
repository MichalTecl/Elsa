namespace RobOrm.Core.Migration
{
    public interface ISchemaMigrator
    {
        void MigrateStructure(IMigrationScriptBuilder script);
    }
}
