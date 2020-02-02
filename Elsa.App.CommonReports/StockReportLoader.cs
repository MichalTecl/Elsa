using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Elsa.App.CommonReports.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
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

        public StockReportModel LoadStockReport(DateTime forDateTime)
        {
            var report = new List<StockReportItemModel>(1000);
            var summary = new List<InventorySummaryValueItemModel>();

            m_database.Sql().Call("GetStockReport")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@culture", m_session.Culture)
                .WithParam("@reportDate", forDateTime)
                .ReadRows<string, string, string, decimal, string, decimal>(
                    (inventory, material, batch, available, symbol, price) =>
                    {
                        report.Add(new StockReportItemModel
                        {
                            InventoryName = inventory,
                            Amount = available,
                            BatchIdentifier = batch,
                            MaterialName = material,
                            UnitSymbol = symbol,
                            Price = price
                        });

                        var sum = summary.FirstOrDefault(s =>
                            s.InventoryName.Equals(inventory, StringComparison.InvariantCultureIgnoreCase));
                        if (sum == null)
                        {
                            sum = new InventorySummaryValueItemModel()
                            {
                                InventoryName = inventory,
                                Value = 0
                            };

                            summary.Add(sum);
                        }

                        sum.Value += price;
                    });

            return new StockReportModel()
            {
                Details = report,
                Summary = summary,
                Prices = LoadPriceComponentsReport(),
                FixedCosts = LoadFixedCostReport()
            };
        }

        public List<BatchPriceComponentItemModel> LoadPriceComponentsReport()
        {
            var result = new List<BatchPriceComponentItemModel>(10000);
            
            m_database.Sql().Call("GetBatchPricesReport")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@culture", m_session.Culture)
                .ReadRows<string, string, string, decimal, string>((material, batch, text, price, month) =>
                {
                    var item =  new BatchPriceComponentItemModel
                    {
                        Text = text,
                        Price = price
                    };

                    result.Add(item);
                    
                    item.BatchIdentifier = batch;
                    item.MaterialName = material;
                    item.Month = month;

                });

            return result;
        }

        private List<FixedCostReportItemModel> LoadFixedCostReport()
        {
            var result = new List<FixedCostReportItemModel>();

            m_database.Sql().Call("GetFixedCostReport")
                .WithParam("@projectId", m_session.Project.Id)
                .ReadRows<string, string, decimal, int>((month, text, value, percent) =>
                {
                    result.Add(new FixedCostReportItemModel()
                    {
                        Month = month,
                        TypeName = text,
                        Value = value,
                        Percent = ((decimal)percent) / 100m
                    });
                });

            return result;
        }
    }
}
