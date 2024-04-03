namespace Robowire.RobOrm.Core.Migration
{
    public interface ISchemaMigrator
    {
        void MigrateStructure(IServiceLocator locator, IMigrationScriptBuilder script);
    }
}
