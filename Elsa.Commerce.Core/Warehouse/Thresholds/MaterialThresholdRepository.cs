using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.Thresholds
{
    public class MaterialThresholdRepository : IMaterialThresholdRepository
    {
        private const string c_cacheKey = "allMaterialThresholds";

        private readonly IPerProjectDbCache _cache;
        private readonly ISession _session;
        private readonly IMaterialRepository _materialRepository;
        private readonly IDatabase _database;
        private readonly IUnitConversionHelper _unitConvertor;

        public MaterialThresholdRepository(IPerProjectDbCache cache,
            ISession session,
            IMaterialRepository materialRepository,
            IDatabase database,
            IUnitConversionHelper unitConvertor)
        {
            _cache = cache;
            _session = session;
            _materialRepository = materialRepository;
            _database = database;
            _unitConvertor = unitConvertor;
        }

        public IMaterialThreshold SaveThreshold(int materialId, decimal value, int unitId)
        {
            var material = _materialRepository.GetMaterialById(materialId);
            if (material == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            if (!_unitConvertor.AreCompatible(material.NominalUnit.Id, unitId))
            {
                throw new InvalidOperationException($"Zvolenou jednotku nelze použít pro materiál \"{material.Name}\"");
            }

            var existing = GetThreshold(materialId) ??
                            _database.New<IMaterialThreshold>(th => th.MaterialId = materialId);

            if ((existing.UnitId == unitId) && (existing.ThresholdQuantity == value))
            {
                return existing;
            }

            existing.UnitId = unitId;
            existing.ThresholdQuantity = value;
            existing.ProjectId = _session.Project.Id;
            existing.UpdateUserId = _session.User.Id;
            existing.UpdateDt = DateTime.Now;

            _database.Save(existing);

            _cache.Remove(c_cacheKey);

            return existing;
        }

        public IEnumerable<IMaterialThreshold> GetAllThresholds()
        {
            return _cache.ReadThrough(c_cacheKey, q => q.SelectFrom<IMaterialThreshold>());
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

            _database.Delete(existing);
            _cache.Remove(c_cacheKey);
        }

        public bool HasThreshold(int materialId)
        {
            return GetAllThresholds().Any(t => t.MaterialId == materialId);
        }
    }
}
