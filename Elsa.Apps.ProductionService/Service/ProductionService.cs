using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Apps.ProductionService.Models;
using Elsa.Apps.ProductionService.Service.Process;
using Elsa.Apps.ProductionService.Service.Process.Steps;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Apps.ProductionService.Service
{
    public class ProductionService : IProductionService
    {
        private static readonly Type[] s_steps = new[]
        {
            typeof(CheckSourceSegmentEditability),
            typeof(ApplySourceSegmentId),
            typeof(LoadRecipe),
            typeof(SanitizeReceivedRequest),
            typeof(ApplyResultingMaterial),
            typeof(ValidateAmountAndPrice),
            typeof(SetComponents)
        };

        private static readonly object s_lock = new object();

        private readonly IServiceLocator m_serviceLocator;
        private readonly ISession m_session;
        private readonly IDatabase m_database;
        private readonly Lazy<IMaterialBatchRepository> m_batchRepository;
        private readonly IUnitRepository m_unitRepository;

        public ProductionService(IServiceLocator serviceLocator, ISession session, IDatabase database, Lazy<IMaterialBatchRepository> batchRepository, IUnitRepository unitRepository)
        {
            m_serviceLocator = serviceLocator;
            m_session = session;
            m_database = database;
            m_batchRepository = batchRepository;
            m_unitRepository = unitRepository;
        }


        public void ValidateRequest(ProductionRequest request)
        {
            //TODO i wouldn't even try to process multiple allocations in parallel, but should be optimized to lock by user (project?)
            lock (s_lock)
            {
                using (var tx = m_database.OpenTransaction())
                {
                    Validate(request);

                    tx.Commit();
                }
            }
        }

        private ProductionRequestContext Validate(ProductionRequest request)
        {
            var context = new ProductionRequestContext(m_session, request);

            foreach (var step in GetSteps())
            {
                step.Process(context);
            }

            request.IsFirstRound = false;
            request.IsValid = request.IsValid && request.Components.All(c => c.IsValid);

            return context;
        }

        public void ProcessRequest(ProductionRequest request)
        {
            lock (s_lock)
            {
                using (var tx = m_database.OpenTransaction())
                {
                    var context = Validate(request);

                    if (!request.IsValid)
                    {
                        throw new InvalidOperationException(request.Messages.FirstOrDefault()?.Text ??
                                                            request.Components.SelectMany(c => c.Messages)
                                                                .FirstOrDefault()?.Text ??
                                                            "chyba :(");
                    }

                    var components = new List<Tuple<BatchKey, Amount>>();

                    foreach (var component in request.Components)
                    {
                        foreach (var aloc in component.Resolutions.Where(r => r.Amount > 0m))
                        {
                            components.Add(new Tuple<BatchKey, Amount>(new BatchKey(component.MaterialId, aloc.BatchNumber), new Amount(aloc.Amount, m_unitRepository.GetUnitBySymbol(aloc.UnitSymbol).Ensure())));
                        }
                    }

                    m_batchRepository.Value.CreateBatchWithComponents(request.RecipeId, context.RequestedAmount,
                        request.ProducingBatchNumber.Trim(), request.ProducingPrice ?? 0, components, request.SourceSegmentId);

                    tx.Commit();
                }
            }
        }

        private IEnumerable<IProductionRequestProcessingStep> GetSteps()
        {
            foreach (var stepType in s_steps)
            {
                yield return (IProductionRequestProcessingStep)m_serviceLocator.InstantiateNow(stepType);
            }
        }
    }
}
