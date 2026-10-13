using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using CodeGeneration;
using CodeGeneration.Compilation;

using Robowire.Core;
using Robowire.Core.LocatorGeneration;
using Robowire.Diagnostics;
using Robowire.Plugin;
using Robowire.Plugin.Flow;

namespace Robowire
{
    public class Container : IContainer
    {
        private readonly ReaderWriterLockSlim _compiledLocatorLock = new ReaderWriterLockSlim();

        private readonly IContainer _parent;

        private readonly List<InstanceRecord> _records = new List<InstanceRecord>();
        private readonly IPluginCollection _plugins;
        private readonly List<IGeneratedCodeListener> _generatorListeners = new List<IGeneratedCodeListener>();
        private Dictionary<string, Func<IServiceLocator, object>> _factories;
        private ILocatorFactory _locatorFactory;
        
        public Container(IContainer parent)
        {
            _parent = parent;

            _plugins = _parent == null ? new PluginCollection() : new PluginCollection(parent.Plugins);
        }

        public Container() : this(null)
        {
        }

        public IContainer Parent => _parent;

        public IReadOnlyPluginCollection Plugins => _plugins as IReadOnlyPluginCollection;

        public IServiceLocator GetLocator()
        {
            try
            {
                _compiledLocatorLock.EnterReadLock();

                if (!_records.Any() && (_parent != null))
                {
                    return _parent.GetLocator();
                }

                if (_locatorFactory != null)
                {
                    return _locatorFactory.CreateLocatorInstance(_parent?.GetLocator(), _factories);
                }
            }
            finally
            {
                _compiledLocatorLock.ExitReadLock();
            }

            try
            {
                _compiledLocatorLock.EnterUpgradeableReadLock();

                if (_locatorFactory != null)
                {
                    return _locatorFactory.CreateLocatorInstance(_parent?.GetLocator(), _factories);
                }

                CompileLocator();

                return _locatorFactory.CreateLocatorInstance(_parent?.GetLocator(), _factories);
            }
            finally
            {
                _compiledLocatorLock.ExitUpgradeableReadLock();
            }
        }

        private void CompileLocator()
        {
            try
            {
                _compiledLocatorLock.EnterWriteLock();

                StartupLog.Write($"Container: locator compilation started ({_records.Count} records)");

                var cacheKey = TryGetCacheKey();

                StartupLog.Write("Container: cache key computed");

                if (TryCreateLocatorFromCache(cacheKey))
                {
                    StartupLog.Write("Container: locator loaded from cache");
                    return;
                }

                SetApplicablePlugins();
                ResolveConstructors();

                _factories = CollectFactories();

                var cplr = new Compiler();
                var bdr = new LocatorBuilder();

                // Names in the generated code are numbered from the beginning for each locator,
                // so the same setup produces the same code on every start.
                using (UniqueNames.BeginScope())
                {
                    bdr.BuildLocatorCode(_records, cplr);
                }

                StartupLog.Write("Container: locator code generated");

                var code = cplr.ToString();
                
                try
                {
                    var assembly = cplr.Compile(cacheKey);

                    StartupLog.Write("Container: assembly compiled");

                    _locatorFactory = CreateLocatorFactory(assembly);

                    StartupLog.Write("Container: locator ready");

                    foreach (var listener in _generatorListeners)
                    {
                        listener.OnContainerGenerated(code, false, null);
                    }
                }
                catch (Exception ex)
                {
                    foreach (var listener in _generatorListeners)
                    {
                        listener.OnContainerGenerated(code, true, ex);
                    }

                    throw;
                }
            }
            finally
            {
                _compiledLocatorLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// The key identifies the generated locator by the application build and the container setup,
        /// so the cached assembly can be used without generating the code. Returns null when the cache can't be used.
        /// </summary>
        private string TryGetCacheKey()
        {
            if (!AssemblyCache.Enabled || _generatorListeners.Any())
            {
                // Listeners need the generated code
                return null;
            }

            try
            {
                return $"{ApplicationBuild.Hash}\n{SetupFingerprint.Compute(_records, _plugins)}";
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool TryCreateLocatorFromCache(string cacheKey)
        {
            if (cacheKey == null)
            {
                return false;
            }

            var assembly = AssemblyCache.TryLoad(cacheKey);
            if (assembly == null)
            {
                return false;
            }

            try
            {
                _factories = CollectFactories();
                _locatorFactory = CreateLocatorFactory(assembly);
                return true;
            }
            catch (Exception)
            {
                // The cached assembly doesn't fit (it should not happen); the locator gets generated again
                AssemblyCache.TryRemove(cacheKey);

                _factories = null;
                _locatorFactory = null;

                return false;
            }
        }

        private Dictionary<string, Func<IServiceLocator, object>> CollectFactories()
        {
            var factories = new Dictionary<string, Func<IServiceLocator, object>>();

            foreach (var record in _records)
            {
                if (record.Factory != null)
                {
                    factories[record.Factory.Name] = record.Factory.Factory;
                }

                if (record.ConstructorParameters != null)
                {
                    foreach (var param in record.ConstructorParameters.Where(p => p.ValueProvider != null))
                    {
                        factories[param.ValueProvider.Name] = param.ValueProvider.Factory;
                    }
                }
            }

            return factories;
        }

        private ILocatorFactory CreateLocatorFactory(Assembly assembly)
        {
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

            return (ILocatorFactory)ctor.Invoke(new object[] { _parent?.GetLocator(), _factories });
        }

        public IContainer Setup(Action<IContainerSetup> setup)
        {
            var setupObject = new ContainerSetup(_records, _plugins, _generatorListeners);
            
            try
            {
                _compiledLocatorLock.EnterWriteLock();

                setup(setupObject);

                _locatorFactory = null;
            }
            finally
            {
                _compiledLocatorLock.ExitWriteLock();
            }

            return this;
        }

        private void SetApplicablePlugins()
        {
            foreach (var record in _records)
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

            find(_plugins.CustomInstanceCreators);
            if (lst.Count > 1)
            {
                throw new InvalidOperationException($"Not more than one plugin from CustomInstanceCreators could be applicable to the resource. For {record.InterfaceType.Name} are applicable: {string.Join(", ", lst.Select(pl => pl.GetType().Name))}");
            }

            if (lst.Count == 0)
            {
                find(_plugins.DefaultInstanceCreators);
            }

            if (lst.Count == 0)
            {
                throw new InvalidOperationException($"No instance creator found for {record.InterfaceType.Name}");
            }

            find(_plugins.AfterNewInstanceCreated);
            find(_plugins.LifecyclePlugins);

            record.ApplicablePlugins = lst;
        }

        private void ResolveConstructors()
        {
            foreach (var record in _records)
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

