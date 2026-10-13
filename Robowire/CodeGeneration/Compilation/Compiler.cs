using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using CodeGeneration.Primitives;

using Microsoft.CSharp;

namespace CodeGeneration.Compilation
{
    public class Compiler : ICompiler
    {
        private readonly StringBuilder _code = new StringBuilder();
        private readonly HashSet<Assembly> _registeredAssemblies = new HashSet<Assembly>();

        public ICompiler Write(string value)
        {
            _code.Append(value);
            return this;
        }

        public ICompiler WriteLine(string value = null)
        {
            if (value == null)
            {
                _code.AppendLine();
            }
            else
            {
                _code.AppendLine(value);
            }

            return this;
        }

        public void RegisterType(Type type)
        {
            RegisterAssembly(type.Assembly);
        }

        public void RegisterAssembly(Assembly a)
        {
            if (_registeredAssemblies.Contains(a))
            {
                return;
            }

            _registeredAssemblies.Add(a);

            foreach (var referenced in a.GetReferencedAssemblies())
            {
                if (_registeredAssemblies.Any(i => i.GetName() == referenced))
                {
                    continue;
                }

                var loaded = Assembly.Load(referenced);
                RegisterAssembly(loaded);
            }
        }

        public Assembly Compile()
        {
            return Compile(null);
        }

        /// <summary>
        /// Compiles the code and stores the result in <see cref="AssemblyCache"/> under the given key (when not null).
        /// </summary>
        public Assembly Compile(string cacheKey)
        {
            var assemblyBytes = CompileToBytes(_code.ToString());

            if (cacheKey != null)
            {
                AssemblyCache.TrySave(cacheKey, assemblyBytes);
            }

            return Assembly.Load(assemblyBytes);
        }

        private byte[] CompileToBytes(string code)
        {
            var outputPath = Path.Combine(Path.GetTempPath(), $"robowire_{Guid.NewGuid():N}.dll");

            try
            {
                using (var provider = new CSharpCodeProvider())
                {
                    var parameters = new CompilerParameters();

                    SetReferencedAssemblies(parameters.ReferencedAssemblies);

                    parameters.GenerateInMemory = false;
                    parameters.GenerateExecutable = false;
                    parameters.OutputAssembly = outputPath;

                    var results = provider.CompileAssemblyFromSource(parameters, code);

                    if (results.Errors.HasErrors)
                    {
                        StringBuilder sb = new StringBuilder();

                        foreach (CompilerError error in results.Errors)
                        {
                            sb.AppendLine(error.ToString());
                        }

                        throw new InvalidOperationException(sb.ToString());
                    }

                    return File.ReadAllBytes(outputPath);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                }
                catch (Exception)
                {
                    // Not important
                }
            }
        }

        private void SetReferencedAssemblies(StringCollection target)
        {
            foreach (var asm in _registeredAssemblies)
            {
                target.Add(asm.Location);
            }
        }

        public override string ToString()
        {
            return _code.ToString();
        }
    }
}
