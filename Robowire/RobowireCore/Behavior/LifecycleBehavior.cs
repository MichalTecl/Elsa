using Robowire.Core;

namespace Robowire.Behavior
{
    public class LifecycleBehavior : IBehavior
    {
        public bool AlwaysNewInstance { get; set; } = false;

        public void BindTo(IServiceSetupRecord setup)
        {
        }

        public void InheritPreviousBehavior(IBehavior behavior)
        {
        }
    }
}
