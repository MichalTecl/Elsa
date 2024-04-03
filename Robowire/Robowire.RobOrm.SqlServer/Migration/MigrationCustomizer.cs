namespace Robowire.RobOrm.SqlServer.Migration
{
    public class MigrationCustomizer
    {
        public string BeforeMigrationScript { get; set; }

        public string AfterMigrationScript { get; set; }

        public string ScriptsRoot { get; set; }
    }
}
