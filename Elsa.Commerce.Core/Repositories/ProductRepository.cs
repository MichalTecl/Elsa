using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
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

        public IProduct GetProduct(string erpProductId)
        {
            throw new NotImplementedException("TODO");
            /*
            var product = m_index.FirstOrDefault(p => p.ErpProductId == erpProductId && p.ProjectId == m_session.Project.Id);
            if (product == null)
            {
                product =
                    m_database.SelectFrom<IProduct>()
                        .Where(p => p.ProjectId == m_session.Project.Id && p.ErpProductId == erpProductId)
                        .Execute()
                        .FirstOrDefault();

                if (product != null)
                {
                    m_index.Add(product);
                }
            }

            return product;*/
        }

        public void SaveProduct(IProduct product)
        {
            throw new NotImplementedException("TODO");
            /*
            var cachedProduct = m_index.FirstOrDefault(p => p.ErpProductId == product.ErpProductId && p.ProjectId == m_session.Project.Id);
            if (cachedProduct != null)
            {
                if (product.Id > 0 && product.Id == cachedProduct.Id && product.Name == cachedProduct.Name)
                {
                    return;
                }
            }


            if (product.Id > 0 && product.ProjectId != m_session.Project.Id)
            {
                throw new InvalidOperationException("Project assignment mischmatch");
            }

            product.ProjectId = m_session.Project.Id;

            m_database.Save(product);

            m_index.Clear();
            */
        }
    }
}
