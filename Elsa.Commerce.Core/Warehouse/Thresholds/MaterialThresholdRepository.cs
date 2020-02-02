using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Thresholds
{
    public class MaterialThresholdRepository : IMaterialThresholdRepository
    {
        private const string c_cacheKey = "allMaterialThresholds";

        private readonly IPerProjectDbCache m_cache;
        private readonly ISession m_session;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IDatabase m_database;
        private readonly IUnitConversionHelper m_unitConvertor;

        public MaterialThresholdRepository(IPerProjectDbCache cache,
            ISession session,
            IMaterialRepository materialRepository,
            IDatabase database,
            IUnitConversionHelper unitConvertor)
        {
            m_cache = cache;
            m_session = session;
            m_materialRepository = materialRepository;
            m_database = database;
            m_unitConvertor = unitConvertor;
        }

        public IMaterialThreshold SaveThreshold(int materialId, decimal value, int unitId)
        {
            var material = m_materialRepository.GetMaterialById(materialId);
            if (material == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            if (!m_unitConvertor.AreCompatible(material.NominalUnit.Id, unitId))
            {
                throw new InvalidOperationException($"Zvolenou jednotku nelze použít pro materiál \"{material.Name}\"");
            }

            var existing = GetThreshold(materialId) ??
                            m_database.New<IMaterialThreshold>(th => th.MaterialId = materialId);

            if ((existing.UnitId == unitId) && (existing.ThresholdQuantity == value))
            {
                return existing;
            }

            existing.UnitId = unitId;
            existing.ThresholdQuantity = value;
            existing.ProjectId = m_session.Project.Id;
            existing.UpdateUserId = m_session.User.Id;
            existing.UpdateDt = DateTime.Now;

            m_database.Save(existing);

            m_cache.Remove(c_cacheKey);

            return existing;
        }

        public IEnumerable<IMaterialThreshold> GetAllThresholds()
        {
            return m_cache.ReadThrough(c_cacheKey, q => q.SelectFrom<IMaterialThreshold>());
        }

        public IMaterialThreshold GetThreshold(int materialId)
        {
            return GetAllThresholds().FirstOrDefault(t => t.MaterialId == materialId);
        }

        public void DeleteThreshold(int materialId)
        {
            var existing = GetThreshold(materialId);
            if (existing == null)
            {
                return;
            }

            m_database.Delete(existing);
            m_cache.Remove(c_cacheKey);
        }

        public bool HasThreshold(int materialId)
        {
            return GetAllThresholds().Any(t => t.MaterialId == materialId);
        }
    }
}
