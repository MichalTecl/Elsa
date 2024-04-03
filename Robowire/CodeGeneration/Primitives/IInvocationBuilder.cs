using System;

namespace CodeGeneration.Primitives
{
    public interface IInvocationBuilder 
    {
        IInvocationBuilder WithParam(string value);

        IInvocationBuilder WithParam(INamedReference valueReference);

        IInvocationBuilder WithParam(Action<ICodeBlockBuilder> paramCode);
    }
}
