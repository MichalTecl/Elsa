using System;

namespace CodeGeneration.Primitives
{
    public interface IClassFieldBuilder : INamedReference, ICodeRenderer, IWithModifiers<IClassFieldBuilder>
    {
        IClassFieldBuilder WithAssignment(Action<ICodeBlockBuilder> assignment);
    }
}
