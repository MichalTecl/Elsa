using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Production.Model;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Production
{
    public class ProductionFacade : IProductionFacade
    {
        private readonly IDatabase m_database;
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitConversionHelper m_unitConversion;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly ILog m_log;
        private readonly IUnitRepository m_unitRepository;
        private readonly ISession m_session;

        public ProductionFacade(
            IDatabase database,
            IMaterialBatchRepository batchRepository,
            IMaterialRepository materialRepository,
            IUnitConversionHelper unitConversion, 
            AmountProcessor amountProcessor, 
            ILog log, 
            IMaterialBatchFacade batchFacade, IUnitRepository unitRepository, ISession session)
        {
            m_database = database;
            m_batchRepository = batchRepository;
            m_materialRepository = materialRepository;
            m_unitConversion = unitConversion;
            m_amountProcessor = amountProcessor;
            m_log = log;
            m_batchFacade = batchFacade;
            m_unitRepository = unitRepository;
            m_session = session;
        }

        public IEnumerable<IMaterialBatch> LoadProductionBatches(long? fromDt, int pageSize)
        {
            var query =
                m_database.SelectFrom<IMaterialBatch>()
                    .Join(b => b.Material)
                    .Join(b => b.Unit)
                    .Join(b => b.Author)
                    .Where(b => b.ProjectId == m_session.Project.Id)
                    .OrderByDesc(b => b.Created)
                    .Take(pageSize);

            if (fromDt != null)
            {
                var d = new DateTime(fromDt.Value);

                query = query.Where(b => b.Created < d);
            }

            return query.Execute();
        }
       
    }
}

