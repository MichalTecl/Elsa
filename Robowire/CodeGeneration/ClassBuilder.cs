using System;
using System.Reflection;
using System.Text;

using CodeGeneration.Impl;
using CodeGeneration.Primitives;
using CodeGeneration.Primitives.Internal;

namespace CodeGeneration
{
    public class ClassBuilder : MemberBuilderBase, IClassBuilder
    {
        private INamedReference m_baseClass;
        private readonly LazyCollection m_interfaces = new LazyCollection();
        
        private readonly LazyCollection<IMethodBuilder> m_methodBuilders = new LazyCollection<IMethodBuilder>();
        private readonly LazyCollection<IClassBuilder> m_nestedClasses = new LazyCollection<IClassBuilder>();
        private readonly LazyCollection<IClassFieldBuilder> m_fields = new LazyCollection<IClassFieldBuilder>();
        private readonly LazyCollection<IPropertyBuilder> m_properties = new LazyCollection<IPropertyBuilder>();
        
        private readonly IConstructorBuilder m_constructor;

        private readonly int m_indent;

        public ClassBuilder(string name, int indent)
            : base(name)
        {
            m_baseClass = GetTypeReference(typeof(object));
            m_indent = indent;
            m_constructor = new ConstructorBuilder(name, indent + 1);
        }

        public ClassBuilder(string name) : this(name, 0) { }

        public void Render(ICompiler compiler)
        {
            WriteIndent(compiler, m_indent);
            WriteModifiers(compiler);
            //TODO indent
            compiler.Write(" class ").Write(Name).Write(" : ").Write(m_baseClass.Name);

            for(var i = 0; i < m_interfaces.Count; i++)
            {
                compiler.Write(", ").Write(m_interfaces[i].Name);
            }
            
            compiler.WriteLine();
            WriteIndent(compiler, m_indent);
            compiler.WriteLine("{");

            foreach (var field in m_fields)
            {
                field.Render(compiler);
            }

            compiler.WriteLine();

            m_constructor.Render(compiler);

            compiler.WriteLine();

            foreach (var property in m_properties)
            {
                property.Render(compiler);
                compiler.WriteLine();
            }

            foreach (var method in m_methodBuilders)
            {
                method.Render(compiler);
                compiler.WriteLine();
            }

            foreach (var nested in m_nestedClasses)
            {
                nested.Render(compiler);
                compiler.WriteLine();
            }

            WriteIndent(compiler, m_indent);
            compiler.WriteLine("}");

            RegisterTypes(compiler);
        }
        
        public IConstructorBuilder WithConstructor()
        {
            return m_constructor;
        }

        public IClassBuilder Inherits(Type baseClassType)
        {
            return Inherits(GetTypeReference(baseClassType));
        }

        public IClassBuilder Inherits<T>()
        {
            return Inherits(typeof(T));
        }

        public IClassBuilder Implements(Type interfaceType)
        {
            return Implements(GetTypeReference(interfaceType));
        }

        public IClassBuilder Implements<T>()
        {
            return Implements(typeof(T));
        }

        public IClassBuilder Inherits(INamedReference baseClassReference)
        {
            m_baseClass = baseClassReference;

            return this;
        }

        public IClassBuilder Implements(INamedReference interfaceReference)
        {
            m_interfaces.TryAdd(interfaceReference);

            return this;
        }

        public IMethodBuilder HasMethod(string name)
        {
            bool dummy;
            return HasMethod(name, out dummy);
        }

        public IMethodBuilder HasMethod(string name, out bool alreadyExisted)
        {
            return m_methodBuilders.TryAdd(new MethodBuilder(name, m_indent + 1), out alreadyExisted);
        }

        public IMethodBuilder OverridesMethod(MethodInfo method)
        {
            var mb = CreateMethodWithoutModifiers(method);

            mb.WithModifier("override");
            mb.WithModifier(method.IsPublic ? "public" : "protected");

            return mb;
        }

        public IMethodBuilder ImplementsMethod(MethodInfo method)
        {
            var mb = CreateMethodWithoutModifiers(method);

            mb.WithModifier("public");

            return mb;
        }

        public IClassBuilder HasNestedClass(string name)
        {
            return m_nestedClasses.TryAdd(new ClassBuilder(name, m_indent + 1));
        }

        public IClassFieldBuilder HasField(string name, Type type)
        {
            return HasField(name, GetTypeReference(type));
        }

        public IClassFieldBuilder HasField<T>(string name)
        {
            return HasField(name, typeof(T));
        }

        public IClassFieldBuilder HasField(INamedReference typeReference)
        {
            return HasField($"_f{Guid.NewGuid():N}", typeReference);
        }

        public IClassFieldBuilder HasField(Type type)
        {
            return HasField(GetTypeReference(type));
        }

        public IClassFieldBuilder HasField<T>()
        {
            return HasField(typeof(T));
        }

        public IClassFieldBuilder HasField(string name, INamedReference typeReference)
        {
            return m_fields.TryAdd(new ClassFieldBuilder(name, typeReference, m_indent + 1));
        }

        public IPropertyBuilder HasProperty(string name, Type t)
        {
            return HasProperty(name, GetTypeReference(t));
        }

        public IPropertyBuilder HasProperty<T>(string name)
        {
            return HasProperty(name, typeof(T));
        }

        public IPropertyBuilder HasProperty(string name, INamedReference typeReference)
        {
            return m_properties.TryAdd(new PropertyBuilder(name, typeReference, m_indent + 1));
        }

        public IPropertyBuilder HasPublicProperty<T>(string name)
        {
            return HasProperty<T>(name).WithModifier("public");
        }

        public IClassBuilder WithModifier(string modifier)
        {
            AddModifier(modifier);
            return this;
        }

        private IMethodBuilder CreateMethodWithoutModifiers(MethodInfo method)
        {
            var mb = HasMethod(method.Name).Returns(method.ReturnType);

            foreach (var p in method.GetParameters())
            {
                mb.WithParam(p.Name, p.ParameterType);
            }

            return mb;
        }

        public override string ToString()
        {
            var compiler = new DebugCompiler();
            Render(compiler);

            return compiler.ToString();
        }

        private class DebugCompiler : ICompiler
        {
            private readonly StringBuilder m_stringBuilder = new StringBuilder();

            public ICompiler Write(string value)
            {
                m_stringBuilder.Append(value);
                return this;
            }

            public ICompiler WriteLine(string value = null)
            {
                m_stringBuilder.AppendLine(value ?? string.Empty);
                return this;
            }

            public void RegisterType(Type type)
            {
            }

            public Assembly Compile()
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return m_stringBuilder.ToString();
            }
        }
    }
}
