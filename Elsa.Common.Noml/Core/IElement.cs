using System.Collections.Generic;

namespace Elsa.Common.Noml.Core
{
    public interface IElement : IRenderable
    {
        string Type { get; }

        List<IAttribute> Attributes { get; }

        List<IRenderable> Children { get; }

        string Content { get; set; }
    }
}
