using Elsa.Common.Logging;
using Elsa.Jobs.Common.EntityChangeProcessing.Helpers;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.Common.EntityChangeProcessing
{
    public interface IEntityChangeProcessor<TEntity>
    {
        string ProcessorUniqueName { get; }
        long GetEntityId(TEntity ett);
        EntityChunk<TEntity> LoadChunkToCompare(IDatabase db, int projectId, EntityChunk<TEntity> previousChunk, int alreadyProcessedRowsCount);
        IEnumerable<object> GetComparedValues(TEntity e);
        void Process(IEnumerable<EntityChangeEvent<TEntity>> changedEntities, IEntityProcessCallback<TEntity> callback, ILog log);        
    }
}
