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
        private INamedReference _baseClass;
        private readonly LazyCollection _interfaces = new LazyCollection();
        
        private readonly LazyCollection<IMethodBuilder> _methodBuilders = new LazyCollection<IMethodBuilder>();
        private readonly LazyCollection<IClassBuilder> _nestedClasses = new LazyCollection<IClassBuilder>();
        private readonly LazyCollection<IClassFieldBuilder> _fields = new LazyCollection<IClassFieldBuilder>();
        private readonly LazyCollection<IPropertyBuilder> _properties = new LazyCollection<IPropertyBuilder>();
        
        private readonly IConstructorBuilder _constructor;

        private readonly int _indent;

        public ClassBuilder(string name, int indent)
            : base(name)
        {
            _baseClass = GetTypeReference(typeof(object));
            _indent = indent;
            _constructor = new ConstructorBuilder(name, indent + 1);
        }

        public ClassBuilder(string name) : this(name, 0) { }

        public void Render(ICompiler compiler)
        {
            WriteIndent(compiler, _indent);
            WriteModifiers(compiler);
            //TODO indent
            compiler.Write(" class ").Write(Name).Write(" : ").Write(_baseClass.Name);

            for(var i = 0; i < _interfaces.Count; i++)
            {
                compiler.Write(", ").Write(_interfaces[i].Name);
            }
            
            compiler.WriteLine();
            WriteIndent(compiler, _indent);
            compiler.WriteLine("{");

            foreach (var field in _fields)
            {
                field.Render(compiler);
            }

            compiler.WriteLine();

            _constructor.Render(compiler);

            compiler.WriteLine();

            foreach (var property in _properties)
            {
                property.Render(compiler);
                compiler.WriteLine();
            }

            foreach (var method in _methodBuilders)
            {
                method.Render(compiler);
                compiler.WriteLine();
            }

            foreach (var nested in _nestedClasses)
            {
                nested.Render(compiler);
                compiler.WriteLine();
            }

            WriteIndent(compiler, _indent);
            compiler.WriteLine("}");

            RegisterTypes(compiler);
        }
        
        public IConstructorBuilder WithConstructor()
        {
            return _constructor;
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
            _baseClass = baseClassReference;

            return this;
        }

        public IClassBuilder Implements(INamedReference interfaceReference)
        {
            _interfaces.TryAdd(interfaceReference);

            return this;
        }

        public IMethodBuilder HasMethod(string name)
        {
            bool dummy;
            return HasMethod(name, out dummy);
        }

        public IMethodBuilder HasMethod(string name, out bool alreadyExisted)
        {
            return _methodBuilders.TryAdd(new MethodBuilder(name, _indent + 1), out alreadyExisted);
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
            return _nestedClasses.TryAdd(new ClassBuilder(name, _indent + 1));
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
            return HasField($"_f{UniqueNames.Next()}", typeReference);
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
            return _fields.TryAdd(new ClassFieldBuilder(name, typeReference, _indent + 1));
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
            return _properties.TryAdd(new PropertyBuilder(name, typeReference, _indent + 1));
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
            private readonly StringBuilder _stringBuilder = new StringBuilder();

            public ICompiler Write(string value)
            {
                _stringBuilder.Append(value);
                return this;
            }

            public ICompiler WriteLine(string value = null)
            {
                _stringBuilder.AppendLine(value ?? string.Empty);
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
                return _stringBuilder.ToString();
            }
        }
    }
}
