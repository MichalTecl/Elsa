using System.IO;

namespace Elsa.Common.Noml.Core
{
    public interface IRenderable
    {
        void Render(TextWriter writer);
    }
}
