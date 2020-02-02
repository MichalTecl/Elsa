using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly List<IProduct> m_index = new List<IProduct>();

        public ProductRepository(IDatabase database, ISession session)
        {
            m_database = database;
            m_session = session;
        }

        public void PreloadCache(int erpId)
        {
            m_index.Clear();
            m_index.AddRange(
                m_database.SelectFrom<IProduct>()
                    .Where(p => p.ProjectId == m_session.Project.Id)
                    .Where(p => p.ErpId == erpId)
                    .Execute());
        }

        public IProduct GetProduct(int erpId, string erpProductId, DateTime orderDt, string productName)
        {
            var product =  m_index.FirstOrDefault(p => (p.ErpId == erpId) && (p.ErpProductId == erpProductId))
                        ?? GetProductFromDatabase(erpId, erpProductId);

            if (product == null)
            {
                product = m_database.New<IProduct>();
                product.ErpId = erpId;
                product.ErpProductId = erpProductId;
                product.ProjectId = m_session.Project.Id;
                product.Name = productName;
                product.ProductNameReceivedAt = orderDt;

                m_database.Save(product);
                m_index.Add(product);
            }
            else if ((product.Name != productName) && (product.ProductNameReceivedAt < orderDt))
            {
                product.Name = productName;
                product.ProductNameReceivedAt = orderDt;

                m_database.Save(product);
                m_index.Remove(product);
                m_index.Add(product);
            }

            return product;
        }

        private IProduct GetProductFromDatabase(int erpId, string erpProductId)
        {
            return
                m_database.SelectFrom<IProduct>()
                    .Where(p => p.ErpId == erpId)
                    .Where(p => p.ErpProductId == erpProductId)
                    .Execute()
                    .FirstOrDefault();
        }
    }
}
