using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Elsa.Jobs.OrdersPostprocessing.Steps;
using Robowire;
using System;
using System.Collections.Generic;

namespace Elsa.Jobs.OrdersPostprocessing
{
    public class OrdersPostprocessingJob : IExecutableJob
    {
        private static readonly Type[] _steps = { typeof(SendPaymentReminder) };

        private readonly ILog _log;
        private readonly IServiceLocator _serviceLocator;

        public OrdersPostprocessingJob(ILog log, IServiceLocator serviceLocator)
        {
            _log = log;
            _serviceLocator = serviceLocator;
        }

        public void Run(string customDataJson)
        {
            _log.Info("Starting orders postprocessing");

            var errors = new List<string>();

            foreach (var stepType in _steps)
            {
                IOrdersPostprocessStep step = null;

                void LogError(string error)
                {
                    _log.Error(error);
                    errors.Add($"{stepType.Name}: {error}");
                }

                try
                {
                    step = _serviceLocator.InstantiateNow<IOrdersPostprocessStep>(stepType);
                    _log.Info($"Starting orders postprocessing step {step}");
                    step.Process(LogError);
                    _log.Info($"Orders postprocessing step {step} completed");
                }
                catch (Exception ex)
                {
                    _log.Error($"Orders postprocessing step {stepType.Name} failed", ex);
                    errors.Add($"{stepType.Name}: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Orders postprocessing finished with {errors.Count} error(s): {string.Join("; ", errors)}");
            }

            _log.Info("Orders postprocessing completed");
        }
    }
}
