using System;

namespace CodeGeneration.Primitives
{
    public interface IMethodBuilder : ICodeRenderer, INamedReference, IWithModifiers<IMethodBuilder>
    {
        ICodeBlockBuilder Body { get; }

        IMethodBuilder Returns(Type returnType);
        
        IMethodBuilder Returns<T>();

        IMethodBuilder Returns(INamedReference typeReference);

        IMethodBuilder ReturnsVoid();

        INamedReference WithParam(string name, Type type);

        INamedReference WithParam<T>(string name);

        INamedReference WithParam(string name, INamedReference typeReference);
    }
}
