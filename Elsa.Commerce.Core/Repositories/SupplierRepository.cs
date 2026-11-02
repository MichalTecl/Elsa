using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Commerce;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly ISession _session;

        public SupplierRepository(IDatabase database, ICache cache, ISession session)
        {
            _database = database;
            _cache = cache;
            _session = session;
        }

        public ISupplier GetSupplier(int supplierId)
        {
            return GetSuppliers().FirstOrDefault(s => s.Id == supplierId);
        }

        public ISupplier GetSupplier(string supplierName)
        {
            return GetSuppliers()
                .FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.InvariantCultureIgnoreCase));
        }

        public ISupplier SaveSupplier(ISupplier supplier)
        {
            if (supplier.Id < 1)
            {
                supplier.InsertUserId = _session.User.Id;
                supplier.InsertDt = DateTime.Now;
            }

            supplier.ProjectId = _session.Project.Id;

            _database.Save(supplier);

            _cache.Remove(GetSuppliersCacheKey());

            return GetSupplier(supplier.Id);
        }

        public ISupplier WriteSupplier(int? id, Action<ISupplier> populate)
        {
            ISupplier entity;

            if (id != null)
            {
                entity = GetSupplier(id.Value);
                if (entity == null)
                {
                    throw new InvalidOperationException("Invalid entity reference");
                }
            }
            else
            {
                entity = _database.New<ISupplier>();
            }

            populate(entity);

            return SaveSupplier(entity);
        }

        public void DeleteSupplier(int supplierId)
        {
            var entity = GetSupplier(supplierId);
            if (entity == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            entity.DeleteDt = DateTime.Now;
            entity.DeleteUserId = _session.User.Id;

            SaveSupplier(entity);
        }

        public IEnumerable<ISupplier> GetSuppliers()
        {
            return _cache.ReadThrough(GetSuppliersCacheKey(),
                TimeSpan.FromHours(1),
                () =>
                    _database.SelectFrom<ISupplier>()
                        .Join(s => s.Currency)
                        .Where(s => s.ProjectId == _session.Project.Id)
                        .Where(s => s.DeleteDt == null)
                        .Execute());
        }

        private string GetSuppliersCacheKey()
        {
            return $"all_suppliers_{_session.Project.Id}";
        }
    }
}
