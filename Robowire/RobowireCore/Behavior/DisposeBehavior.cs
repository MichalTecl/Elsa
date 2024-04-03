using Robowire.Core;

namespace Robowire.Behavior
{
    public class DisposeBehavior : IBehavior
    {
        public bool Dispose { get; set; } = true;

        public void BindTo(IServiceSetupRecord setup)
        {
        }

        public void InheritPreviousBehavior(IBehavior behavior)
        {
        }
    }
}
