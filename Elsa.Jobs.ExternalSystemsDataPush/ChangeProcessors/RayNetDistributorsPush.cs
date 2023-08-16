using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Jobs.Common.EntityChangeProcessing;
using Elsa.Jobs.Common.EntityChangeProcessing.Helpers;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.ExternalSystemsDataPush.ChangeProcessors
{
    public class RayNetDistributorsPush : IEntityChangeProcessor<ICustomer>
    {        
        public string ProcessorUniqueName { get; } = "Raynet_Customers_Push";

        public IEnumerable<object> GetComparedValues(ICustomer e)
        {
            yield return e.Email;
            yield return e.Phone;
            yield return e.Name;
            yield return e.NewsletterSubscriber;
            yield return e.IsDistributor;
            yield return e.VatId;
        }

        public long GetEntityId(ICustomer ett)
        {
            return ett.Id;
        }

        public EntityChunk<ICustomer> LoadChunkToCompare(IDatabase db, int projectId, EntityChunk<ICustomer> previousChunk, int alreadyProcessedRowsCount)
        {
            var data = db.SelectFrom<ICustomer>()
                .Where(c => c.ProjectId == projectId)
                .Where(c => c.IsDistributor)
                .OrderBy(c => c.Id)
                .Skip(alreadyProcessedRowsCount)
                .Take(100)
                .Execute()
                .ToList();

            return new EntityChunk<ICustomer>(data, data.Count < 100);
        }

        public void Process(IEnumerable<EntityChangeEvent<ICustomer>> changedEntities, IEntityProcessCallback<ICustomer> callback, ILog log)
        {
            foreach (var e in changedEntities) 
            {
                log.Info($"Processing: {(e.IsNew ? "INSERT" : "UPDATE")} {e.Entity.Email}");
                callback.OnProcessed(e.Entity, $"extId{e.Entity.Email}", "cudata");
            }
        }
    }
}
