using System;

using CodeGeneration.Primitives;
using CodeGeneration.Primitives.Internal;

namespace CodeGeneration.Impl
{
    public class MethodBuilder : MemberBuilderBase, IMethodBuilder
    {
        private readonly int m_indent;

        private readonly CodeBlockBuilder m_body;

        private INamedReference m_returns = null;

        public MethodBuilder(string name, int indent)
            : base(name)
        {
            m_indent = indent;
            m_body = new CodeBlockBuilder(m_indent + 1);
            ReturnsVoid();
        }

        public virtual void Render(ICompiler compiler)
        {
            WriteIndent(compiler, m_indent);
            WriteModifiers(compiler);

            compiler.Write(" ").Write(m_returns.Name).Write(" ").Write(Name);

            WriteParameters(compiler);

            WriteBody(compiler);

            RegisterTypes(compiler);
        }

        protected void WriteBody(ICompiler compiler)
        {
            compiler.WriteLine();

            WriteIndent(compiler, m_indent);

            compiler.WriteLine("{");
            WriteIndent(compiler, m_indent + 1);
            m_body.Render(compiler);

            compiler.WriteLine();
            WriteIndent(compiler, m_indent);
            compiler.WriteLine("}");
        }

        public IMethodBuilder WithModifier(string modifier)
        {
            AddModifier(modifier);
            return this;
        }

        public ICodeBlockBuilder Body => m_body;

        public IMethodBuilder Returns(Type returnType)
        {
            return Returns(GetTypeReference(returnType));
        }

        public IMethodBuilder Returns<T>()
        {
            return Returns(typeof(T));
        }

        public IMethodBuilder Returns(INamedReference typeReference)
        {
            m_returns = typeReference;

            return this;
        }

        public IMethodBuilder ReturnsVoid()
        {
            return Returns(new NamedReference("void"));
        }
    }
}
