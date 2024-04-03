using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeGeneration.Primitives.Internal
{
    public class MemberBuilderBase
    {
        private static readonly string[] s_modifiersOrder =
            {
                "private", "public", "protected", "virtual", "static",
                "sealed", "abstract", "override"
            };

        private readonly HashSet<string> m_modifiers = new HashSet<string>();
        private readonly Dictionary<Type, INamedReference> m_typeReferences = new Dictionary<Type, INamedReference>();
        private readonly List<KeyValuePair<string, INamedReference>> m_parameters = new List<KeyValuePair<string, INamedReference>>();
        private readonly HashSet<Type> m_registeredTypes = new HashSet<Type>();

        public MemberBuilderBase(string name)
        {
            Name = name;
        }

        public string Name { get; }

        protected void AddModifier(string modifier)
        {
            m_modifiers.Add(modifier);
        }

        protected INamedReference GetTypeReference(Type t)
        {
            INamedReference reference;
            if (!m_typeReferences.TryGetValue(t, out reference))
            {
                m_registeredTypes.Add(t);
                var sb = new StringBuilder();
                CreateTypeString(t, sb);
                reference = new NamedReference(sb.ToString());
                m_typeReferences.Add(t, reference);
            }

            return reference;
        }

        protected void WriteModifiers(ICompiler target)
        {
            var modIndex = m_modifiers.ToList();

            var firstModifier = true;
            foreach (var pMod in s_modifiersOrder)
            {
                if (!m_modifiers.Contains(pMod))
                {
                    continue;
                }

                if (!firstModifier)
                {
                    target.Write(" ");
                }
                firstModifier = false;
                target.Write(pMod);
                modIndex.Remove(pMod);
            }

            foreach (var p in modIndex)
            {
                if (!firstModifier)
                {
                    target.Write(" ");
                }
                firstModifier = false;
                target.Write(p);
            }
        }

        protected void WriteIndent(ICompiler compiler, int indent)
        {
            for (var i = 0; i < indent; i++)
            {
                compiler.Write("    ");
            }
        }

        protected void WriteParameters(ICompiler compiler)
        {
            compiler.Write("(");

            for (var i = 0; i < m_parameters.Count; i++)
            {
                if (i > 0)
                {
                    compiler.Write(", ");
                }

                compiler.Write(m_parameters[i].Value.Name)
                        .Write(" ")
                        .Write(m_parameters[i].Key);
            }

            compiler.Write(")");
        }

        public INamedReference WithParam(string name, Type type)
        {
            return WithParam(name, GetTypeReference(type));
        }

        public INamedReference WithParam<T>(string name)
        {
            return WithParam(name, typeof(T));
        }

        public INamedReference WithParam(string name, INamedReference typeReference)
        {
            if (!m_parameters.Any(p => p.Key == name))
            {
                m_parameters.Add(new KeyValuePair<string, INamedReference>(name, typeReference));
            }
            return new NamedReference(name);
        }

        protected void RegisterTypes(ICompiler compiler)
        {
            foreach (var registeredType in m_registeredTypes)
            {
                compiler.RegisterType(registeredType);
            }
        }

        private static void CreateTypeString(Type t, StringBuilder sb)
        {
            if (t == typeof(void))
            {
                sb.Append("void");
                return;
            }

            var tFullName = t.FullName;
            if (t.IsGenericType)
            {
                var apoInx = tFullName.IndexOf("`", StringComparison.Ordinal);
                if (apoInx > 0)
                {
                    tFullName = tFullName.Substring(0, apoInx);
                }
            }
            
            if (tFullName?.Contains("+") ?? false)
            {
                CreateTypeString(t.DeclaringType, sb);
            }
            else
            {
                sb.Append(t.Namespace);
            }
            
            sb.Append(".");
            
            var tname = t.Name;
            var apInx = tname.IndexOf("`", StringComparison.Ordinal);
            if (apInx > 0)
            {
                tname = tname.Substring(0, apInx);
            }

            sb.Append(tname);

            var genericArgs = t.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                sb.Append("<");

                for (var i = 0; i < genericArgs.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    CreateTypeString(genericArgs[i], sb);
                }

                sb.Append(">");
            }
        }
    }
}
