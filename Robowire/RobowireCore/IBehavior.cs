using Robowire.Core;

namespace Robowire
{
    public interface IBehavior
    {
        /// <summary>
        /// Implementing class can use this method to setup this behavior for particular setup
        /// </summary>
        /// <param name="setup"></param>
        void BindTo(IServiceSetupRecord setup);

        /// <summary>
        /// If one service has setup more than single behavior of the same type, 2nd one will be called to inherit 1st one, then 3rd to inherit 2nd ...
        /// </summary>
        /// <param name="behavior"></param>
        void InheritPreviousBehavior(IBehavior behavior);
    }
}
