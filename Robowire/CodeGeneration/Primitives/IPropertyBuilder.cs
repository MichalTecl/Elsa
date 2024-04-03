using System;

namespace CodeGeneration.Primitives
{
    public interface IPropertyBuilder : ICodeRenderer, INamedReference, IWithModifiers<IPropertyBuilder>
    {
        ISetterBuilder HasSetter();

        ICodeBlockBuilder HasGetter();

        void Returns(Action<ICodeBlockBuilder> value);
    }
}
