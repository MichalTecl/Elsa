namespace CodeGeneration.Primitives.Internal
{
    internal class NamedReference : INamedReference
    {
        public NamedReference(string name)
        {
            Name = name;
        }

        public string Name
        {
            get;
        }
    }
}
