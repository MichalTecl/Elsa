using System;

namespace CodeGeneration.Primitives.Internal
{
    public class PropertyBuilder : MemberBuilderBase, IPropertyBuilder
    {
        private ICodeBlockBuilder m_getter = null;

        private ISetterBuilder m_setter = null;

        private readonly INamedReference m_type;

        private readonly int m_indent;

        public PropertyBuilder(string name, INamedReference typReference, int indent)
            : base(name)
        {
            m_indent = indent;
            m_type = typReference;
        }

        public void Render(ICompiler compiler)
        {
            
            WriteIndent(compiler, m_indent);
            WriteModifiers(compiler);

            compiler.Write(" ").Write(m_type.Name).Write(" ").Write(Name).WriteLine();
            WriteIndent(compiler, m_indent);
            compiler.Write("{");

            if (m_getter != null)
            {
                compiler.WriteLine();
                WriteIndent(compiler, m_indent + 1);
                compiler.WriteLine("get");
                WriteIndent(compiler, m_indent + 1);
                compiler.WriteLine("{");
                WriteIndent(compiler, m_indent + 2);
                m_getter.Render(compiler);
                WriteIndent(compiler, m_indent - 1);
                compiler.WriteLine("}");
            }

            if (m_setter != null)
            {
                compiler.WriteLine();
                WriteIndent(compiler, m_indent + 1);
                compiler.WriteLine("set");
                WriteIndent(compiler, m_indent + 1);
                compiler.WriteLine("{");
                WriteIndent(compiler, m_indent + 2);
                m_setter.Render(compiler);
                WriteIndent(compiler, m_indent - 1);
                compiler.WriteLine("}");
            }

            WriteIndent(compiler, m_indent);
            compiler.WriteLine("}");

            RegisterTypes(compiler);
        }

        public IPropertyBuilder WithModifier(string modifier)
        {
            AddModifier(modifier);

            return this;
        }

        public ISetterBuilder HasSetter()
        {
            return m_setter ?? (m_setter = new SetterBuilder(m_indent + 1));
        }

        public ICodeBlockBuilder HasGetter()
        {
            return m_getter ?? (m_getter = new CodeBlockBuilder(m_indent + 1));
        }

        public void Returns(Action<ICodeBlockBuilder> value)
        {
            HasGetter().Returns(value);
        }
    }
}
