using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Elsa.App.CommonReports.Model;
using Elsa.Common;
using Robowire.RobOrm.Core;

namespace Elsa.App.CommonReports
{
    public class StockReportLoader : IStockReportLoader
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        public StockReportLoader(IDatabase database, ISession session)
        {
            m_database = database;
            m_session = session;
        }

        public IEnumerable<StockReportItemModel> LoadStockReport(DateTime forDateTime)
        {
            var result = new List<StockReportItemModel>(1000);

            m_database.Sql().Call("GetStockReport")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@culture", m_session.Culture)
                .WithParam("@reportDate", forDateTime)
                .ReadRows<string, string, string, decimal, string, decimal>(
                    (inventory, material, batch, available, symbol, price) =>
                    {
                        result.Add(new StockReportItemModel
                        {
                            InventoryName = inventory,
                            Amount = available,
                            BatchIdentifier = batch,
                            MaterialName = material,
                            UnitSymbol = symbol,
                            Price = price
                        });
                    });

            return result;
        }

        public IList<BatchPriceComponentItemModel> LoadPriceComponentsReport()
        {
            var result = new List<BatchPriceComponentItemModel>(10000);

            var hs = new HashSet<string>();

            m_database.Sql().Call("GetStockReport")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@culture", m_session.Culture)
                .ReadRows<string, string, string, decimal>((material, batch, text, price) =>
                {
                    var item =  new BatchPriceComponentItemModel
                    {
                        Text = text,
                        Price = price
                    };

                    result.Add(item);

                    if (hs.Add(batch))
                    {
                        item.BatchIdentifier = batch;
                        item.MaterialName = material;
                    }
                });

            return result;
        }
    }
}
