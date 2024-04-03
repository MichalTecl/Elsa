namespace Robowire.RobOrm.Core.DefaultRules
{
    internal class DefaultRobOrmSetup : IRobOrmSetup
    {
        public IEntityNamingConvention EntityNamingConvention { get; } = new DefaultEntityNamingConvention();
    }
}
