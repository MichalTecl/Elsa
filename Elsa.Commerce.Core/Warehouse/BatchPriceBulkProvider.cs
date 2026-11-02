using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse
{
    internal class BatchPriceBulkProvider : IBatchPriceBulkProvider
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly IMaterialBatchFacade _batchFacade;
        private readonly DateTime _from;
        private readonly DateTime _to;

        private readonly Dictionary<int, List<PriceComponentModel>> _prices = new Dictionary<int, List<PriceComponentModel>>();

        public BatchPriceBulkProvider(IDatabase database, ISession session, IMaterialBatchFacade batchFacade, DateTime from, DateTime to)
        {
            _database = database;
            _session = session;
            _batchFacade = batchFacade;
            _from = from;
            _to = to;

            Preload();
        }
        
        public List<PriceComponentModel> GetBatchPriceComponents(int batchId)
        {
            if (!_prices.TryGetValue(batchId, out var prices))
            {
                prices = _batchFacade.GetPriceComponents(batchId, false).ToList();
                _prices.Add(batchId, prices);
            }

            return prices;
        }

        private void Preload()
        {
            _database.Sql().Call("PreloadBatchPrices")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@from", _from)
                .WithParam("@to", _to)
                .WithParam("@culture", _session.Culture)
                .ReadRows<int, string, decimal, bool, int?>((batchId, txt, val, isWarn, sourceBatchId) =>
                {
                    if (!_prices.TryGetValue(batchId, out var prices))
                    {
                        prices = new List<PriceComponentModel>(10);
                        _prices.Add(batchId, prices);
                    }

                    prices.Add(new PriceComponentModel(batchId)
                    {
                        Text = txt,
                        RawValue = val,
                        SourceBatchId = sourceBatchId,
                        IsWarning = isWarn
                    });
                });
        }
    }
}
