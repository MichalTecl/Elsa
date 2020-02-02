using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse
{
    internal class BatchPriceBulkProvider : IBatchPriceBulkProvider
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly DateTime m_from;
        private readonly DateTime m_to;

        private readonly Dictionary<int, List<PriceComponentModel>> m_prices = new Dictionary<int, List<PriceComponentModel>>();

        public BatchPriceBulkProvider(IDatabase database, ISession session, IMaterialBatchFacade batchFacade, DateTime from, DateTime to)
        {
            m_database = database;
            m_session = session;
            m_batchFacade = batchFacade;
            m_from = from;
            m_to = to;

            Preload();
        }
        
        public List<PriceComponentModel> GetBatchPriceComponents(int batchId)
        {
            if (!m_prices.TryGetValue(batchId, out var prices))
            {
                prices = m_batchFacade.GetPriceComponents(batchId, false).ToList();
                m_prices.Add(batchId, prices);
            }

            return prices;
        }

        private void Preload()
        {
            m_database.Sql().Call("PreloadBatchPrices")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@from", m_from)
                .WithParam("@to", m_to)
                .WithParam("@culture", m_session.Culture)
                .ReadRows<int, string, decimal, bool, int?>((batchId, txt, val, isWarn, sourceBatchId) =>
                {
                    if (!m_prices.TryGetValue(batchId, out var prices))
                    {
                        prices = new List<PriceComponentModel>(10);
                        m_prices.Add(batchId, prices);
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
