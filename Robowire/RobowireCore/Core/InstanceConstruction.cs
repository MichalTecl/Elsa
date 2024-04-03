using System;
using System.Collections.Generic;
using System.Linq;

using Robowire.Plugin;

namespace Robowire.Core
{
    internal class InstanceConstruction : SetupElementBase, IInstanceConstruction
    {
        private readonly InstanceRecord m_record;

        public InstanceConstruction(InstanceRecord record, IEnumerable<IPlugin> plugins) : base(record, plugins)
        {
            m_record = record;
        }

        public IDependencyResolutionSetup With<T>(T value)
        {
            Setup<T>(null, p => p.ValueProvider = new NamedFactory(sl => value));
            return this;
        }

        public IDependencyResolutionSetup With<T>(Func<IServiceLocator, T> valueFactory)
        {
            Setup<T>(null, p => p.ValueProvider = new NamedFactory(sl => valueFactory(sl)));
            return this;
        }

        public IDependencyResolutionSetup With<T>(string paramName, T value)
        {
            Setup<T>(paramName, p => p.ValueProvider = new NamedFactory(sl => value));
            return this;
        }

        public IDependencyResolutionSetup With<T>(string paramName, Func<IServiceLocator, T> valueFactory)
        {
            Setup<T>(paramName, p => p.ValueProvider = new NamedFactory(sl => valueFactory(sl)));
            return this;
        }

        private void Setup<T>(string paramName, Action<CtorParamSetupRecord> setup)
        {
            if (m_record.ConstructorParameters == null)
            {
                m_record.ConstructorParameters = new List<CtorParamSetupRecord>();
            }

            var targetRec = m_record.ConstructorParameters.FirstOrDefault(p => (p.ParameterType == typeof(T)) && (string.IsNullOrWhiteSpace(paramName) || (p.ParameterName == paramName)));
            if (targetRec == null)
            {
                targetRec = new CtorParamSetupRecord();
                m_record.ConstructorParameters.Add(targetRec);
            }

            targetRec.ParameterType = typeof(T);
            targetRec.ParameterName = paramName ?? targetRec.ParameterName;

            setup(targetRec);
        }

        public IDependencyResolutionSetup Constructor(params Type[] argumentType)
        {
            var ctor = m_record.ImplementingType.GetConstructor(argumentType);
            if (ctor == null)
            {
                throw new ArgumentException($"No matching constructor found Type={m_record.ImplementingType}");
            }

            m_record.ConstructorParameters = new List<CtorParamSetupRecord>();

            foreach (var parameter in ctor.GetParameters())
            {
                m_record.ConstructorParameters.Add(new CtorParamSetupRecord()
                                                       {
                                                           ParameterName = parameter.Name,
                                                           ParameterType = parameter.ParameterType,
                                                           ValueProvider = null
                                                       });
            }

            return this;
        }
    }
}
