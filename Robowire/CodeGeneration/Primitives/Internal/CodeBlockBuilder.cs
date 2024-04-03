using System;
using System.Text;

namespace CodeGeneration.Primitives.Internal
{
    public class CodeBlockBuilder : MemberBuilderBase, ICodeBlockBuilder
    {
        private readonly int m_indent;

        private readonly StringBuilder m_stringBuilder;

        private CodeBlockBuilder(int indent, StringBuilder sb)
            : base(null)
        {
            m_indent = indent;
            m_stringBuilder = sb;
        }

        public CodeBlockBuilder(int indent) : this(indent, new StringBuilder())
        {
            m_indent = indent;
        }

        public void Render(ICompiler compiler)
        {
            compiler.Write(m_stringBuilder.ToString());
            RegisterTypes(compiler);
        }

        public ICodeBlockBuilder Write(string value)
        {
            m_stringBuilder.Append(value);
            return this;
        }

        public ICodeBlockBuilder Write(bool boolean)
        {
            return Write(boolean ? "true" : "false");
        }

        public ICodeBlockBuilder Write(INamedReference reference)
        {
            return Write(reference.Name);
        }

        public ICodeBlockBuilder Write(Type t)
        {
            return Write(GetTypeReference(t));
        }

        public ICodeBlockBuilder NewLine()
        {
            m_stringBuilder.AppendLine();
            Indent();
            return this;
        }

        public ICodeBlockBuilder Space()
        {
            return Write(" ");
        }

        public ICodeBlockBuilder EndStatement()
        {
            return Write(";").NewLine();
        }
        
        public INamedReference DeclareLocal(string name, Type type)
        {
            return DeclareLocal(name, GetTypeReference(type));
        }

        public INamedReference DeclareLocal<T>(string name)
        {
            return DeclareLocal(name, typeof(T));
        }

        public INamedReference DeclareLocal(string name, INamedReference typeReference)
        {
            Write(typeReference).Space().Write(name).EndStatement();
            return new NamedReference(name);
        }

        public ICodeBlockBuilder Assign(INamedReference target, Action<ICodeBlockBuilder> assignment)
        {
            Write(target).Write(" = ");
            assignment(this);
            return EndStatement();
        }

        public ICodeBlockBuilder LazyReadOrAssign(INamedReference target, Action<ICodeBlockBuilder> assignment)
        {
            Write("(").Write(target).Write(" ?? (").Write(target).Write(" = ");
            assignment(this);
            return Write("))");
        }

        public ICodeBlockBuilder Invoke(INamedReference target, Action<IInvocationBuilder> invocation)
        {
            var ib = new InvocationBuilder(this);

            Write(target).Write("(");

            invocation(ib);

            return Write(")");
        }

        public ICodeBlockBuilder Invoke(INamedReference targetObject, string methodName, Action<IInvocationBuilder> invocation)
        {
            Write(targetObject).Write(".").Write(methodName).Write("(");

            var ib = new InvocationBuilder(this);
            invocation(ib);

            return Write(")");
        }

        public ICodeBlockBuilder Invoke(Type staticClass, string methodName, Action<IInvocationBuilder> invocation)
        {
            return Invoke(GetTypeReference(staticClass), methodName, invocation);
        }

        public ICodeBlockBuilder Invoke(string targetName, Action<IInvocationBuilder> invocation)
        {
            return Invoke(new NamedReference(targetName), invocation);
        }

        public ICodeBlockBuilder InvokeGenericMethod(
            INamedReference targetObject,
            string methodName,
            Type[] genericArguments,
            Action<IInvocationBuilder> invocation)
        {
            Write(targetObject).Write(".").Write(methodName);

            if ((genericArguments != null) && (genericArguments.Length > 0))
            {
                Write("<");

                for (var i = 0; i < genericArguments.Length; i++)
                {
                    if (i > 0) Write(", ");

                    Write(genericArguments[i]);
                }

                Write(">");
            }

            Write("(");

            var ib = new InvocationBuilder(this);
            invocation(ib);

            return Write(")");
        }

        public ICodeBlockBuilder InvokeGenericMethod(Type staticClass, string methodName, Type[] genericArguments, Action<IInvocationBuilder> invocation)
        {
            return InvokeGenericMethod(GetTypeReference(staticClass), methodName, genericArguments, invocation);
        }

        public ICodeBlockBuilder InvokeConstructor(INamedReference ctorType, Action<IInvocationBuilder> invocation)
        {
            var ib = new InvocationBuilder(this);

            Write("new ").Write(ctorType).Write("(");

            invocation(ib);

            return Write(")");
        }

        public ICodeBlockBuilder InvokeConstructor(Type ctorType, Action<IInvocationBuilder> invocation)
        {
            return InvokeConstructor(GetTypeReference(ctorType), invocation);
        }

        public ICodeBlockBuilder InnerBlock(Action<ICodeBlockBuilder> inner)
        {
            Write("{").NewLine();

            var childBuilder = new CodeBlockBuilder(m_indent+1, m_stringBuilder);

            inner(childBuilder);

            return Write("}");
        }

        public void Returns(Action<ICodeBlockBuilder> value)
        {
            Write("return ");
            value(this);
            EndStatement();
        }

        public ICodeBlockBuilder ForEach(INamedReference collection, Action<INamedReference, ICodeBlockBuilder> itemVariableAndInnerBlock)
        {
            var variableName = new NamedReference($"v{Guid.NewGuid():N}");
            Write("foreach(var ").Write(variableName).Write(" in ").Write(collection).Write(")").NewLine();

            InnerBlock(i => itemVariableAndInnerBlock(variableName, i));

            return NewLine();
        }

        public ICodeBlockBuilder If(Action<ICodeBlockBuilder> condition, Action<ICodeBlockBuilder> thenBlock, Action<ICodeBlockBuilder> elseBlock = null)
        {
            Write("if(");
            condition(this);
            Write(")").NewLine();

            InnerBlock(thenBlock);

            if (elseBlock == null)
            {
                return this;
            }

            Write("else").NewLine();

            InnerBlock(elseBlock).NewLine();

            return this;
        }

        /// <summary>
        /// var new_var_name = assignment;
        /// </summary>
        /// <param name="assignment"></param>
        /// <returns></returns>
        public INamedReference Var(Action<ICodeBlockBuilder> assignment)
        {
            var variableName = new NamedReference($"v{Guid.NewGuid():N}");

            Write("var ").Write(variableName).Write(" = ");
            assignment(this);

            EndStatement();

            return variableName;
        }

        public ICodeBlockBuilder Compare(INamedReference a, INamedReference b)
        {
            return Write(a).Write(" == ").Write(b);
        }

        public ICodeBlockBuilder Compare(string a, INamedReference b)
        {
            return Write(a).Write(" == ").Write(b);
        }

        public ICodeBlockBuilder Compare(INamedReference a, string b)
        {
            return Write(a).Write(" == ").Write(b);
        }

        public ICodeBlockBuilder Compare(INamedReference a, Action<ICodeBlockBuilder> b)
        {
            Write(a);
            Write(" == ");
            b(this);

            return this;
        }

        public ICodeBlockBuilder String(string s)
        {
            return Write("\"").Write(s).Write("\"");
        }

        public ICodeBlockBuilder Typeof<T>()
        {
            return Typeof(typeof(T));
        }

        public ICodeBlockBuilder Typeof(Type t)
        {
            return Write("typeof(").Write(t).Write(")");
        }

        private void Indent()
        {
            for (var i = 0; i < m_indent; i++)
            {
                m_stringBuilder.Append("    ");
            }
        }
    }
}
