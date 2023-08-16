using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Jobs.Common.EntityChangeProcessing.Helpers;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Utils;

namespace Elsa.Jobs.Common.EntityChangeProcessing.Entities
{
    internal class ChangeProcessorHost<T> : IChangeProcessorHost<T>
    {
        private readonly ILog _log;
        private readonly IDatabase _db;
        private readonly ISession _session;

        internal ChangeProcessorHost(ILog log, IDatabase db, ISession session)
        {
            _log = log;
            _db = db;
            _session = session;
        }
        
        public void Execute(IEntityChangeProcessor<T> processor)
        {
            _log.Info($"Starting processing of {processor.ProcessorUniqueName}");
            int processedCount = 0;
            EntityChunk<T> chunk = null;
            do
            {
                chunk = processor.LoadChunkToCompare(_db, _session.Project.Id, chunk, processedCount);
                if (chunk == null) 
                {
                    _log.Info($"Recevied NULL chunk - finishing");
                    break;
                }

                processedCount += chunk.Data.Count;

                ProcessChunk(processor, chunk);

            } while (chunk?.IsLastPage == false);

            _log.Info($"All data processed - received chunk.IsLastPage={chunk?.IsLastPage}");
        }

        private void ProcessChunk(IEntityChangeProcessor<T> processor, EntityChunk<T> chunk)
        {
            _log.Info($"Received chunk of {chunk.Data.Count} item(s); customData={chunk.CustomData}");

            if (chunk.Data.Count == 0) 
            {
                _log.Info("0 items, skipping chunk processing");
                return;
            }

            var entities = chunk.Data.ToDictionary(i => processor.GetEntityId(i), i => new EttInfo() { Entity = i, CurrentHash = EntityHashCalculator.GetHashCode(processor.GetComparedValues(i)) });

            _log.Info($"Got {entities.Count} entity Id(s)");
                                    
            LoadEntitiesLog(processor.ProcessorUniqueName, entities);

            RemoveWhere(entities, e => e.CurrentHash == e.Log?.EntityHash);

            _log.Info($"Detected {entities.Count} of changed entities");

            if (entities.Count == 0)
            {
                _log.Info($"No changes found in chunk of {chunk.Data.Count} item(s); customData={chunk.CustomData} - finishing this chunk");
                return;
            }

            processor.Process(entities.Values.Select(e => new EntityChangeEvent<T>(e.Entity, e.Log?.ExternalId, e.Log == null)), new Callback(processor, entities), _log);

            RemoveWhere(entities, e => !e.CalledBack);

            _log.Info($"{entities.Count} entites to be marked as processed");

            if (entities.Count == 0)
            {
                _log.Info($"No entities to be marked as processed");
            }
            else
            {
                SaveChangeLog(processor.ProcessorUniqueName, entities);
            }
                        
            _log.Info($"Processed chunk of {chunk.Data.Count} item(s); customData={chunk.CustomData}");
        }

        private void RemoveWhere(Dictionary<long, EttInfo> entities, Func<EttInfo, bool> predicate)
        {
            var ids = entities.Keys.ToArray();

            foreach(var id in ids) 
            {
                var entity = entities[id];

                if(predicate(entity))
                    entities.Remove(id);
            }            
        }

        private void LoadEntitiesLog(string processorName, Dictionary<long, EttInfo> infos) 
        {
            var ids = infos.Keys;

            foreach(var idChunk in ids.Chunk(100)) 
            {
                var log = _db.SelectFrom<IEntityChangeProcessingLog>()
                    .Where(l => l.ProcessorName == processorName)
                    .Where(l => l.EntityId.InCsv(idChunk))
                    .Execute();

                foreach (var l in log)
                    infos[l.EntityId].Log = l;
            }
        }

        private void SaveChangeLog(string processorName, Dictionary<long, EttInfo> infos) 
        {
            _db.Sql().Call("SaveEntityChangeProcessingLog")
                .WithParam("@processorName", processorName)
                .WithStructuredParam("@ChangeLog"
                , "UDT_EntityChangeProcessingLog"
                , infos
                , new[] { "EntityId", "EntityHash", "ExternalId", "CustomData" }
                , info => new object[] { info.Key,
                                         info.Value.CurrentHash,
                                         info.Value.NewExtId ?? info.Value.Log?.ExternalId,
                                         info.Value.CustomData ?? info.Value.Log?.CustomData  })
                .NonQuery();                
        }
        
        private class Callback : IEntityProcessCallback<T> 
        {
            private readonly IEntityChangeProcessor<T> _processor;
            private readonly Dictionary<long, EttInfo> _entities;

            public Callback(IEntityChangeProcessor<T> processor, Dictionary<long, EttInfo> entities)
            {
                _processor = processor;
                _entities = entities;
            }

            public void OnProcessed(T entity, string obtainedExternalId, string customData)
            {
                var id = _processor.GetEntityId(entity);

                if (!_entities.TryGetValue(id, out var target))
                    throw new InvalidOperationException("entity reference not found");

                target.CalledBack = true;
                target.NewExtId = obtainedExternalId;
                target.CustomData = customData;
            }
        }

        private sealed class EttInfo 
        {
            public T Entity;
            public IEntityChangeProcessingLog Log;
            public string CurrentHash;
            public bool CalledBack;
            public string NewExtId;

            public string CustomData { get; internal set; }
        }
    }
}
