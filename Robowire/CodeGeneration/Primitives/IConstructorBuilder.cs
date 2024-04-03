namespace CodeGeneration.Primitives
{
    public interface IConstructorBuilder : IMethodBuilder, IWithModifiers<IConstructorBuilder>
    {
        IInvocationBuilder CallsBase();

        new IConstructorBuilder WithModifier(string modifier);
    }
}
