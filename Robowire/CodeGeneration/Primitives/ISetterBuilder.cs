namespace CodeGeneration.Primitives
{
    public interface ISetterBuilder : ICodeRenderer, ICodeBlockBuilder, IWithModifiers<ISetterBuilder>
    {
        INamedReference ValueParameter { get; }
    }
}
