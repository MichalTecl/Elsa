using Robowire.Core;

namespace Robowire.RobOrm.Core.EntityGeneration
{
    public class EntityBehavior : IBehavior
    {
        public string EntityName { get; set; }

        public string PrimaryKeyProperty { get; set; }

        public void BindTo(IServiceSetupRecord setup)
        {
        }

        public void InheritPreviousBehavior(IBehavior behavior)
        {
        }
    }
}
