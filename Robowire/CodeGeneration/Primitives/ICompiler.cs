using System;
using System.Reflection;

namespace CodeGeneration.Primitives
{
    public interface ICompiler
    {
        ICompiler Write(string value);

        ICompiler WriteLine(string value = null);

        void RegisterType(Type type);

        Assembly Compile();
    }
}
