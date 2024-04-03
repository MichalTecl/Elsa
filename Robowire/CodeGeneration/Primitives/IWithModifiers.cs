namespace CodeGeneration.Primitives
{
    public interface IWithModifiers<T>
    {
        T WithModifier(string modifier);
    }
}
