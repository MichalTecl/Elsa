using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly IDatabase m_database;
        private readonly ICache m_cache;
        private readonly ISession m_session;

        public SupplierRepository(IDatabase database, ICache cache, ISession session)
        {
            m_database = database;
            m_cache = cache;
            m_session = session;
        }

        public ISupplier GetSupplier(int supplierId)
        {
            return GetSuppliers().FirstOrDefault(s => s.Id == supplierId);
        }

        public ISupplier SaveSupplier(ISupplier supplier)
        {
            if (supplier.Id < 1)
            {
                supplier.InsertUserId = m_session.User.Id;
                supplier.InsertDt = DateTime.Now;
            }

            supplier.ProjectId = m_session.Project.Id;

            m_database.Save(supplier);

            m_cache.Remove(GetSuppliersCacheKey());

            return supplier;
        }

        public ISupplier CreateSupplier(Action<ISupplier> populate)
        {
            var supplier = m_database.New(populate);
            return SaveSupplier(supplier);
        }

        public void DeleteSupplier(int supplierId)
        {
            var entity = GetSupplier(supplierId);
            if (entity == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            entity.DeleteDt = DateTime.Now;
            entity.DeleteUserId = m_session.User.Id;

            SaveSupplier(entity);
        }

        public IEnumerable<ISupplier> GetSuppliers()
        {
            return m_cache.ReadThrough(GetSuppliersCacheKey(),
                TimeSpan.FromHours(1),
                () =>
                    m_database.SelectFrom<ISupplier>()
                        .Join(s => s.Currency)
                        .Where(s => s.ProjectId == m_session.Project.Id)
                        .Where(s => s.DeleteDt == null)
                        .Execute());
        }

        private string GetSuppliersCacheKey()
        {
            return $"all_suppliers_{m_session.Project.Id}";
        }
    }
}
