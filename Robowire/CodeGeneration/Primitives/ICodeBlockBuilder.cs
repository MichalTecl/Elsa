using System;

namespace CodeGeneration.Primitives
{
    public interface ICodeBlockBuilder : ICodeRenderer
    {
        ICodeBlockBuilder Write(string value);

        ICodeBlockBuilder Write(bool boolean);

        ICodeBlockBuilder Write(INamedReference reference);

        ICodeBlockBuilder Write(Type t);

        ICodeBlockBuilder NewLine();

        ICodeBlockBuilder Space();

        ICodeBlockBuilder EndStatement();

        void Returns(Action<ICodeBlockBuilder> value);

        /// <summary>
        /// Type name;
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        INamedReference DeclareLocal(string name, Type type);

        /// <summary>
        /// Type name;
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        INamedReference DeclareLocal<T>(string name);

        /// <summary>
        /// Type name;
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        INamedReference DeclareLocal(string name, INamedReference typeReference);

        /// <summary>
        /// target = assignment;
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assignment"></param>
        /// <returns></returns>
        ICodeBlockBuilder Assign(INamedReference target, Action<ICodeBlockBuilder> assignment);

        /// <summary>
        /// target ?? (target = assignment)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assignment"></param>
        /// <returns></returns>
        ICodeBlockBuilder LazyReadOrAssign(INamedReference target, Action<ICodeBlockBuilder> assignment);

        ICodeBlockBuilder Invoke(INamedReference target, Action<IInvocationBuilder> invocation);

        ICodeBlockBuilder Invoke(INamedReference targetObject, string methodName, Action<IInvocationBuilder> invocation);

        ICodeBlockBuilder Invoke(Type staticClass, string methodName, Action<IInvocationBuilder> invocation);

        ICodeBlockBuilder Invoke(string targetName, Action<IInvocationBuilder> invocation);

        ICodeBlockBuilder InvokeGenericMethod(INamedReference targetObject, string methodName, Type[] genericArguments, Action<IInvocationBuilder> invocation);

        ICodeBlockBuilder InvokeGenericMethod(Type staticClass, string methodName, Type[] genericArguments, Action<IInvocationBuilder> invocation);

        ICodeBlockBuilder InvokeConstructor(Type ctorType, Action<IInvocationBuilder> invocation);

        ICodeBlockBuilder InvokeConstructor(INamedReference ctorType, Action<IInvocationBuilder> invocation);

        ICodeBlockBuilder InnerBlock(Action<ICodeBlockBuilder> inner);

        ICodeBlockBuilder ForEach(
            INamedReference collection,
            Action<INamedReference, ICodeBlockBuilder> itemVariableAndInnerBlock);

        ICodeBlockBuilder If(
            Action<ICodeBlockBuilder> condition,
            Action<ICodeBlockBuilder> thenBlock,
            Action<ICodeBlockBuilder> elseBlock = null);

        INamedReference Var(Action<ICodeBlockBuilder> assignment);

        ICodeBlockBuilder Compare(INamedReference a, INamedReference b);

        ICodeBlockBuilder Compare(string a, INamedReference b);

        ICodeBlockBuilder Compare(INamedReference a, string b);

        ICodeBlockBuilder Compare(INamedReference a, Action<ICodeBlockBuilder> b);

        ICodeBlockBuilder String(string s);

        ICodeBlockBuilder Typeof<T>();

        ICodeBlockBuilder Typeof(Type t);

    }
}
