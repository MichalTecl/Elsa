using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;

using CodeGeneration.Primitives;

using Microsoft.CSharp;

namespace CodeGeneration.Compilation
{
    public class Compiler : ICompiler
    {
        private readonly StringBuilder m_code = new StringBuilder();
        private readonly HashSet<Assembly> m_registeredAssemblies = new HashSet<Assembly>();

        public ICompiler Write(string value)
        {
            m_code.Append(value);
            return this;
        }

        public ICompiler WriteLine(string value = null)
        {
            if (value == null)
            {
                m_code.AppendLine();
            }
            else
            {
                m_code.AppendLine(value);
            }

            return this;
        }

        public void RegisterType(Type type)
        {
            RegisterAssembly(type.Assembly);
        }

        public void RegisterAssembly(Assembly a)
        {
            if (m_registeredAssemblies.Contains(a))
            {
                return;
            }

            m_registeredAssemblies.Add(a);

            foreach (var referenced in a.GetReferencedAssemblies())
            {
                if (m_registeredAssemblies.Any(i => i.GetName() == referenced))
                {
                    continue;
                }

                var loaded = Assembly.Load(referenced);
                RegisterAssembly(loaded);
            }
        }

        public Assembly Compile()
        {
            using (var provider = new CSharpCodeProvider())
            {
                var parameters = new CompilerParameters();

                SetReferencedAssemblies(parameters.ReferencedAssemblies);

                parameters.GenerateInMemory = true;
                parameters.GenerateExecutable = false;

                var code = m_code.ToString();
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

                return results.CompiledAssembly;
            }
        }

        private void SetReferencedAssemblies(StringCollection target)
        {
            foreach (var asm in m_registeredAssemblies)
            {
                target.Add(asm.Location);
            }
        }

        public override string ToString()
        {
            return m_code.ToString();
        }
    }
}
