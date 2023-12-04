using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Elsa.App.CommonReports.Model;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire.RobOrm.Core;

namespace Elsa.App.CommonReports
{
    public class StockReportLoader : IStockReportLoader
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IUnitRepository m_unitRepository;

        public StockReportLoader(IDatabase database, ISession session, IUnitRepository unitRepository)
        {
            m_database = database;
            m_session = session;
            m_unitRepository = unitRepository;
        }

        public StockReportModel LoadStockReport(DateTime forDateTime)
        {
            var report = new List<StockReportItemModel>(1000);
            var summary = new List<InventorySummaryValueItemModel>();

            m_database.Sql().Call("GetStockReport")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@culture", m_session.Culture)
                .WithParam("@reportDate", forDateTime)
                .SetupCommand(c => c.CommandTimeout = 120000)
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
            var unitIndex = m_unitRepository.GetAllUnits().ToList();
            var result = new List<BatchPriceComponentItemModel>(10000);
            
            m_database.Sql().Call("GetBatchPricesReport")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@culture", m_session.Culture)
                .ReadRows<string, string, string, decimal, string, int, decimal>((material, batch, text, price, month, unitId, unitPrice) =>
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

                    AssignUnitAndUnitPrice(item, unitId, unitPrice, unitIndex);
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

        private void AssignUnitAndUnitPrice(BatchPriceComponentItemModel model, int unitId, decimal unitPrice, List<IMaterialUnit> units)
        {
            var unit = units.FirstOrDefault(u => u.Id == unitId) ?? throw new ArgumentException($"Invalid UnitId {unitId}");

            model.UnitText = $"1 {unit.Symbol}";
            model.UnitPrice = unitPrice;
        }
    }
}
