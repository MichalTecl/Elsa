using System;

namespace CodeGeneration.Primitives.Internal
{
    internal class ClassFieldBuilder : MemberBuilderBase, IClassFieldBuilder
    {
        private readonly INamedReference m_type;

        private readonly int m_indent;

        private ICodeBlockBuilder m_assignment = null;

        public ClassFieldBuilder(string name, INamedReference type, int indent)
            : base(name)
        {
            m_type = type;
            m_indent = indent;
        }

        public void Render(ICompiler compiler)
        {
            WriteIndent(compiler, m_indent);
            WriteModifiers(compiler);
            compiler.Write(" ").Write(m_type.Name).Write(" ").Write(Name);

            if (m_assignment != null)
            {
                compiler.Write(" = ");
                m_assignment.Render(compiler);
            }

            compiler.Write(";").WriteLine();

            RegisterTypes(compiler);
        }

        public IClassFieldBuilder WithModifier(string modifier)
        {
            AddModifier(modifier);

            return this;
        }

        public IClassFieldBuilder WithAssignment(Action<ICodeBlockBuilder> assignment)
        {
            m_assignment = m_assignment ?? new CodeBlockBuilder(0);
            assignment(m_assignment);

            return this;
        }
    }
}
