using CodeGeneration.Impl;

namespace CodeGeneration.Primitives.Internal
{
    public class ConstructorBuilder : MethodBuilder, IConstructorBuilder
    {
        private readonly ICodeBlockBuilder m_baseCallBuilder = new CodeBlockBuilder(0);
        private IInvocationBuilder m_callToBase = null;

        private readonly int m_indent;

        public ConstructorBuilder(string name, int indent)
            : base(name, indent)
        {
            m_indent = indent;
        }

        public new IConstructorBuilder WithModifier(string modifier)
        {
            AddModifier(modifier);
            return this;
        }

        public IInvocationBuilder CallsBase()
        {
            return m_callToBase ?? (m_callToBase = new InvocationBuilder(m_baseCallBuilder));
        }

        public override void Render(ICompiler compiler)
        {
            WriteIndent(compiler, m_indent);
            WriteModifiers(compiler);

            compiler.Write(" ").Write(Name);

            WriteParameters(compiler);

            if (m_callToBase != null)
            {
                compiler.Write(" : base(");
                m_baseCallBuilder.Render(compiler);
                compiler.Write(")");
            }

            WriteBody(compiler);

            RegisterTypes(compiler);
        }
    }
}
