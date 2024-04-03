using System;
using System.Reflection;

namespace CodeGeneration.Primitives
{
    public interface IClassBuilder : ICodeRenderer, INamedReference, IWithModifiers<IClassBuilder>
    {
        IConstructorBuilder WithConstructor();

        IClassBuilder Inherits(Type baseClassType);

        IClassBuilder Inherits<T>();

        IClassBuilder Implements(Type interfaceType);

        IClassBuilder Implements<T>();

        IClassBuilder Inherits(INamedReference baseClassReference);

        IClassBuilder Implements(INamedReference interfaceReference);

        IMethodBuilder HasMethod(string name);

        IMethodBuilder HasMethod(string name, out bool alreadyExisted);

        IMethodBuilder OverridesMethod(MethodInfo method);

        IMethodBuilder ImplementsMethod(MethodInfo method);
        
        IClassBuilder HasNestedClass(string name);

        IClassFieldBuilder HasField(string name, Type type);

        IClassFieldBuilder HasField<T>(string name);

        IClassFieldBuilder HasField(INamedReference typeReference);

        IClassFieldBuilder HasField(Type type);

        IClassFieldBuilder HasField<T>();

        IClassFieldBuilder HasField(string name, INamedReference typeReference);
        
        IPropertyBuilder HasProperty(string name, Type t);

        IPropertyBuilder HasProperty<T>(string name);

        IPropertyBuilder HasProperty(string name, INamedReference typeReference);

        IPropertyBuilder HasPublicProperty<T>(string name);
    }
}
