using System.Linq;

using Robowire.Core;

namespace Robowire.Behavior
{
    public static class ServiceRecordExtensions
    {
        public static T GetBehavior<T>(this IServiceSetupRecord record, T defaultBehavior = null) where T : class, IBehavior
        {
            T lastBehavior = null;

            var behaviors = record.Behaviors.OfType<T>();
            foreach (var currentBehavior in behaviors)
            {
                if (lastBehavior != null)
                {
                    currentBehavior.InheritPreviousBehavior(lastBehavior);
                }

                lastBehavior = currentBehavior;
            }

            return lastBehavior ?? defaultBehavior;
        }
    }
}
