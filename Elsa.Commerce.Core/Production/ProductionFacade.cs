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

using RobOrm.Core;

namespace Elsa.Commerce.Core.Production
{
    public class ProductionFacade : IProductionFacade
    {
        private readonly IDatabase _database;
        private readonly IMaterialBatchRepository _batchRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IUnitConversionHelper _unitConversion;
        private readonly AmountProcessor _amountProcessor;
        private readonly IMaterialBatchFacade _batchFacade;
        private readonly ILog _log;
        private readonly IUnitRepository _unitRepository;
        private readonly ISession _session;

        public ProductionFacade(
            IDatabase database,
            IMaterialBatchRepository batchRepository,
            IMaterialRepository materialRepository,
            IUnitConversionHelper unitConversion, 
            AmountProcessor amountProcessor, 
            ILog log, 
            IMaterialBatchFacade batchFacade, IUnitRepository unitRepository, ISession session)
        {
            _database = database;
            _batchRepository = batchRepository;
            _materialRepository = materialRepository;
            _unitConversion = unitConversion;
            _amountProcessor = amountProcessor;
            _log = log;
            _batchFacade = batchFacade;
            _unitRepository = unitRepository;
            _session = session;
        }

        public IEnumerable<IMaterialBatch> LoadProductionBatches(long? fromDt, int pageSize)
        {
            var query =
                _database.SelectFrom<IMaterialBatch>()
                    .Join(b => b.Material)
                    .Join(b => b.Unit)
                    .Join(b => b.Author)
                    .Where(b => b.ProjectId == _session.Project.Id)
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

