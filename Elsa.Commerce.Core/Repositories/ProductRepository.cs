using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Commerce;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly List<IProduct> _index = new List<IProduct>();

        public ProductRepository(IDatabase database, ISession session)
        {
            _database = database;
            _session = session;
        }

        public void PreloadCache(int erpId)
        {
            _index.Clear();
            _index.AddRange(
                _database.SelectFrom<IProduct>()
                    .Where(p => p.ProjectId == _session.Project.Id)
                    .Where(p => p.ErpId == erpId)
                    .Execute());
        }

        public IProduct GetProduct(int erpId, string erpProductId, DateTime orderDt, string productName)
        {
            var product =  _index.FirstOrDefault(p => (p.ErpId == erpId) && (p.ErpProductId == erpProductId))
                        ?? GetProductFromDatabase(erpId, erpProductId, productName);

            if (product == null)
            {
                product = _database.New<IProduct>();
                product.ErpId = erpId;
                product.ErpProductId = erpProductId;
                product.ProjectId = _session.Project.Id;
                product.Name = productName;
                product.ProductNameReceivedAt = orderDt;

                _database.Save(product);
                _index.Add(product);
            }
            else if ((product.Name != productName) && (product.ProductNameReceivedAt < orderDt))
            {
                product.Name = productName;
                product.ProductNameReceivedAt = orderDt;

                _database.Save(product);
                _index.Remove(product);
                _index.Add(product);
            }

            return product;
        }

        private IProduct GetProductFromDatabase(int erpId, string erpProductId, string erpProductName)
        {           
            if (string.IsNullOrWhiteSpace(erpProductId))
            {
                return _database.SelectFrom<IProduct>()
                    .Where(p => p.ErpId == erpId)
                    .Where(p => p.Name == erpProductName)
                    .Execute()
                    .FirstOrDefault();
            }

            return
                _database.SelectFrom<IProduct>()
                    .Where(p => p.ErpId == erpId)
                    .Where(p => p.ErpProductId == erpProductId)
                    .Execute()
                    .FirstOrDefault();
        }
    }
}
