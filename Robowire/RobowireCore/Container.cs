using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using CodeGeneration.Compilation;

using Robowire.Core;
using Robowire.Core.LocatorGeneration;
using Robowire.Plugin;
using Robowire.Plugin.Flow;

namespace Robowire
{
    public class Container : IContainer
    {
        private readonly ReaderWriterLockSlim m_compiledLocatorLock = new ReaderWriterLockSlim();

        private readonly IContainer m_parent;

        private readonly List<InstanceRecord> m_records = new List<InstanceRecord>();
        private readonly IPluginCollection m_plugins;
        private readonly List<IGeneratedCodeListener> m_generatorListeners = new List<IGeneratedCodeListener>();
        private Dictionary<string, Func<IServiceLocator, object>> m_factories;
        private ILocatorFactory m_locatorFactory;
        
        public Container(IContainer parent)
        {
            m_parent = parent;

            m_plugins = m_parent == null ? new PluginCollection() : new PluginCollection(parent.Plugins);
        }

        public Container() : this(null)
        {
        }

        public IContainer Parent => m_parent;

        public IReadOnlyPluginCollection Plugins => m_plugins as IReadOnlyPluginCollection;

        public IServiceLocator GetLocator()
        {
            try
            {
                m_compiledLocatorLock.EnterReadLock();

                if (!m_records.Any() && (m_parent != null))
                {
                    return m_parent.GetLocator();
                }

                if (m_locatorFactory != null)
                {
                    return m_locatorFactory.CreateLocatorInstance(m_parent?.GetLocator(), m_factories);
                }
            }
            finally
            {
                m_compiledLocatorLock.ExitReadLock();
            }

            try
            {
                m_compiledLocatorLock.EnterUpgradeableReadLock();

                if (m_locatorFactory != null)
                {
                    return m_locatorFactory.CreateLocatorInstance(m_parent?.GetLocator(), m_factories);
                }

                CompileLocator();

                return m_locatorFactory.CreateLocatorInstance(m_parent?.GetLocator(), m_factories);
            }
            finally
            {
                m_compiledLocatorLock.ExitUpgradeableReadLock();
            }
        }

        private void CompileLocator()
        {
            try
            {
                m_compiledLocatorLock.EnterWriteLock();

                SetApplicablePlugins();
                ResolveConstructors();

                m_factories = new Dictionary<string, Func<IServiceLocator, object>>();

                foreach (var record in m_records)
                {
                    if (record.Factory != null)
                    {
                        m_factories[record.Factory.Name] = record.Factory.Factory;
                    }

                    if (record.ConstructorParameters != null)
                    {
                        foreach (var param in record.ConstructorParameters.Where(p => p.ValueProvider != null))
                        {
                            m_factories[param.ValueProvider.Name] = param.ValueProvider.Factory;
                        }
                    }
                }

                var cplr = new Compiler();
                var bdr = new LocatorBuilder();
                bdr.BuildLocatorCode(m_records, cplr);

                var code = cplr.ToString();
                
                try
                {
                    var assembly = cplr.Compile();

                    var factoryType = assembly.GetTypes().FirstOrDefault(i => typeof(ILocatorFactory).IsAssignableFrom(i));
                    if (factoryType == null)
                    {
                        throw new InvalidOperationException("No Servicelocator compiled");
                    }

                    var ctor = factoryType.GetConstructors().FirstOrDefault();
                    if (ctor == null)
                    {
                        throw new InvalidOperationException("Compiled locator doesn't have expected constructor");
                    }

                    m_locatorFactory = (ILocatorFactory)ctor.Invoke(new object[] { m_parent?.GetLocator(), m_factories });

                    foreach (var listener in m_generatorListeners)
                    {
                        listener.OnContainerGenerated(code, false, null);
                    }
                }
                catch (Exception ex)
                {
                    foreach (var listener in m_generatorListeners)
                    {
                        listener.OnContainerGenerated(code, true, ex);
                    }

                    throw;
                }
            }
            finally
            {
                m_compiledLocatorLock.ExitWriteLock();
            }
        }

        public IContainer Setup(Action<IContainerSetup> setup)
        {
            var setupObject = new ContainerSetup(m_records, m_plugins, m_generatorListeners);
            
            try
            {
                m_compiledLocatorLock.EnterWriteLock();

                setup(setupObject);

                m_locatorFactory = null;
            }
            finally
            {
                m_compiledLocatorLock.ExitWriteLock();
            }

            return this;
        }

        private void SetApplicablePlugins()
        {
            foreach (var record in m_records)
            {
                SetApplicablePlugins(record);
            }
        }

        private void SetApplicablePlugins(InstanceRecord record)
        {
            var lst = new List<IPlugin>();

            Action<IEnumerable<IPlugin>> find = source =>
                {
                    lst.AddRange(source.Where(p => p.IsApplicable(record)));
                };

            find(m_plugins.CustomInstanceCreators);
            if (lst.Count > 1)
            {
                throw new InvalidOperationException($"Not more than one plugin from CustomInstanceCreators could be applicable to the resource. For {record.InterfaceType.Name} are applicable: {string.Join(", ", lst.Select(pl => pl.GetType().Name))}");
            }

            if (lst.Count == 0)
            {
                find(m_plugins.DefaultInstanceCreators);
            }

            if (lst.Count == 0)
            {
                throw new InvalidOperationException($"No instance creator found for {record.InterfaceType.Name}");
            }

            find(m_plugins.AfterNewInstanceCreated);
            find(m_plugins.LifecyclePlugins);

            record.ApplicablePlugins = lst;
        }

        private void ResolveConstructors()
        {
            foreach (var record in m_records)
            {
                ResolveConstructor(record);
            }
        }

        private void ResolveConstructor(InstanceRecord record)
        {
            if (record.ImplementingType == null)
            {
                return;
            }

            var ctor = ChooseConstructor(record);

            var paramsToResolve = ctor.GetParameters().ToList();
            var setupsStack = record.ConstructorParameters?.ToList() ?? new List<CtorParamSetupRecord>(0);

            var alignedParams = new List<CtorParamSetupRecord>();

            while (paramsToResolve.Any())
            {
                var toResolve = paramsToResolve[0];
                paramsToResolve.RemoveAt(0);

                var paramDefinition = (setupsStack.FirstOrDefault(p => p.ParameterName == toResolve.Name)
                                       ?? setupsStack.FirstOrDefault(p => (p.ParameterType == toResolve.ParameterType) && string.IsNullOrWhiteSpace(p.ParameterName))
                                       ?? setupsStack.FirstOrDefault(p => toResolve.ParameterType.IsAssignableFrom(p.ParameterType) && string.IsNullOrWhiteSpace(p.ParameterName)))
                                      ?? new CtorParamSetupRecord();

                paramDefinition.ParameterName = toResolve.Name;
                paramDefinition.ParameterType = toResolve.ParameterType;
                paramDefinition.ValueProvider = paramDefinition.ValueProvider;
                alignedParams.Add(paramDefinition);
                setupsStack.Remove(paramDefinition);
            }

            if (setupsStack.Any())
            {
                var firstFail = setupsStack[0];
                throw new InvalidOperationException($"Cannot resolve constructor parameter. Construcotr owner = {record.ImplementingType}, invalid param = {firstFail.ParameterName} {firstFail.ParameterType}");
            }

            record.ConstructorParameters = alignedParams;
            record.PreferredConstructor = ctor;
        }

        private ConstructorInfo ChooseConstructor(InstanceRecord record)
        {
            var ctors = record.ImplementingType.GetConstructors();

            if (ctors.Length == 1)
            {
                return ctors[0];
            }

            var ptype = record.ConstructorParameters.Select(p => p.ParameterType).ToArray();

            var typedCtor = record.ImplementingType.GetConstructor(ptype);
            if (typedCtor != null)
            {
                return typedCtor;
            }

            throw new InvalidOperationException($"Cannot choose the constructor of {record.ImplementingType}");
        }
    }
}

