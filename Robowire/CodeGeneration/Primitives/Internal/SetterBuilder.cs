namespace CodeGeneration.Primitives.Internal
{
    public class SetterBuilder : CodeBlockBuilder, ISetterBuilder
    {
        private static readonly INamedReference s_valueRef = new NamedReference("value");

        public SetterBuilder(int indent)
            : base(indent)
        {
        }

        public ISetterBuilder WithModifier(string modifier)
        {
            AddModifier(modifier);

            return this;
        }

        public INamedReference ValueParameter => s_valueRef;
    }
}
