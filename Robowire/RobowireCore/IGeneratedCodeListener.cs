using System;

namespace Robowire
{
    public interface IGeneratedCodeListener
    {
        void OnContainerGenerated(string containerCode, bool hasErrors, Exception errors);
    }
}
