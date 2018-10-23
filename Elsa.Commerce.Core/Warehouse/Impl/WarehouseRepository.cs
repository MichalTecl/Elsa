using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Inventory;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Impl
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitConversionHelper m_conversionHelper;
        private readonly ICache m_cache;

        public WarehouseRepository(IDatabase database, ISession session, IMaterialRepository materialRepository, IUnitConversionHelper conversionHelper, ICache cache)
        {
            m_database = database;
            m_session = session;
            m_materialRepository = materialRepository;
            m_conversionHelper = conversionHelper;
            m_cache = cache;
        }

        public IMaterialStockEvent AddMaterialStockEvent(IMaterial material, decimal delta, IMaterialUnit unit, string note)
        {
            if (material.ProjectId != m_session.Project.Id || unit.ProjectId != m_session.Project.Id)
            {
                throw new InvalidOperationException("Cannot use entities from another project");
            }

            if (!m_conversionHelper.AreCompatible(material.NominalUnitId, unit.Id))
            {
                throw new InvalidOperationException($"Použitá jednotka '{unit.Symbol}' není převoditelná na nominální jednotku '{material.NominalUnit.Symbol}' materiálu '{material.Name}' ");
            }

            var evnt = m_database.New<IMaterialStockEvent>();
            evnt.Description = note;
            evnt.EventDt = DateTime.Now;
            evnt.UserId = m_session.User.Id;
            evnt.MaterialId = material.Id;
            evnt.UnitId = unit.Id;
            evnt.ProjectId = m_session.Project.Id;
            evnt.Volume = delta;

            m_database.Save(evnt);

            return GetStockEvents(null, evnt.Id).FirstOrDefault();
        }

        public IEnumerable<IMaterialStockEvent> GetStockEvents(long? lastSeenTime)
        {
            return GetStockEvents(lastSeenTime, null);
        }

        public IEnumerable<IStockLevelSnapshot> GetManualSnapshots(int? materialId)
        {
            var sourceMaterials = new List<IMaterial>();

            if (materialId != null)
            {
                var srcMat = m_materialRepository.GetMaterialById(materialId.Value);
                if (srcMat != null)
                {
                    sourceMaterials.Add(srcMat.Adaptee);
                }

                throw new NotImplementedException("Not supported yet");
            }
            else
            {
                sourceMaterials.AddRange(m_materialRepository.GetAllMaterials().Select(m => m.Adaptee));
            }

            var result = new List<IStockLevelSnapshot>(sourceMaterials.Count);

            foreach (var mat in sourceMaterials)
            {
                var snapshot = GetLatestStockLevelSnapshot(mat.Id, true) ?? new EmpptySnapshot()
                                                                                {
                                                                                    Material = mat,
                                                                                    SnapshotDt = DateTime.Now.AddYears(-100)
                                                                                };
                result.Add(snapshot);
            }

            return result.OrderBy(s => s.SnapshotDt);
        }

        public IStockLevelSnapshot GetLatestStockLevelSnapshot(int materialId, bool manualOnly)
        {
            var key = $"latestStockSnSh_matid:{materialId}manual:{manualOnly}";

            return m_cache.ReadThrough(
                key,
                TimeSpan.FromHours(1),
                () =>
                    {
                        var qry =
                            m_database.SelectFrom<IStockLevelSnapshot>()
                                .Join(s => s.User)
                                .Join(s => s.Material)
                                .Join(s => s.Unit)
                                .Where(s => s.ProjectId == m_session.Project.Id)
                                .Where(s => s.MaterialId == materialId)
                                .OrderByDesc(s => s.SnapshotDt)
                                .Take(1);

                        if (manualOnly)
                        {
                            qry = qry.Where(s => s.IsManual);
                        }

                        return qry.Execute().FirstOrDefault();
                    });
        }

        private IEnumerable<IMaterialStockEvent> GetStockEvents(long? lastSeenTime, int? eventId)
        {
            var qry =
                m_database.SelectFrom<IMaterialStockEvent>()
                    .Join(e => e.Material)
                    .Join(e => e.Unit)
                    .Join(e => e.User)
                    .Where(e => e.ProjectId == m_session.Project.Id)
                    .Take(10)
                    .OrderByDesc(e => e.EventDt);

            if (lastSeenTime != null)
            {
                var lastSeenDt = new DateTime(lastSeenTime.Value);
                qry = qry.Where(e => e.EventDt <= lastSeenDt);
            }

            if (eventId != null)
            {
                qry = qry.Where(e => e.Id == eventId.Value);
            }

            return qry.Execute();
        }

        #region Nested
        private class EmpptySnapshot : IStockLevelSnapshot
        {
            [JsonIgnore]
            public int ProjectId { get; set; }

            [JsonIgnore]
            public IProject Project { get; }

            [JsonIgnore]
            public int MaterialId { get; set; }


            public IMaterial Material { get; set; }

            [JsonIgnore]
            public int UnitId { get; set; }

            [JsonIgnore]
            public IMaterialUnit Unit { get; }

            [JsonIgnore]
            public decimal Volume { get; set; }

            [JsonIgnore]
            public long Id { get; }

            [JsonIgnore]
            public DateTime SnapshotDt { get; set; }

            [JsonIgnore]
            public bool IsManual { get; set; }

            [JsonIgnore]
            public int UserId { get; set; }

            [JsonIgnore]
            public IUser User { get; }

            [JsonIgnore]
            public string Note { get; set; }
        }

        #endregion
    }
}
