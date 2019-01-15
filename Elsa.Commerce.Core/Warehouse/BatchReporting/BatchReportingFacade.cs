using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model.BatchReporting;
using Elsa.Common;
using Elsa.Common.Utils;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse.BatchReporting
{
    public class BatchReportingFacade : IBatchReportingFacade
    {
        private const int c_pageSize = 10;

        private readonly ISession m_session;
        private readonly IDatabase m_database;
        private readonly IMaterialBatchFacade m_batchFacade;

        public BatchReportingFacade(ISession session, IDatabase database, IMaterialBatchFacade batchFacade)
        {
            m_session = session;
            m_database = database;
            m_batchFacade = batchFacade;
        }

        public BatchReportModel QueryBatches(BatchReportQuery query)
        {
            var sql = m_database.Sql().Call("LoadBatchesReport")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@pageSize", c_pageSize)
                .WithParam("@pageNumber", query.PageNumber)
                .WithParam("@materialId", query.MaterialId)
                .WithParam("@orderNumber", query.OrderNumber)
                .WithParam("@batchNumber", query.BatchNumberQuery?.Replace("*", "%"))
                .WithParam("@dtFrom", query.From)
                .WithParam("@dtTo", query.To)
                .WithParam("@closed", query.ClosedBatches)
                .WithParam("@locked", query.LockedBatches)
                .WithParam("@inventoryTypeId", query.InventoryTypeId);

            var result = new BatchReportModel { Query = query };
            result.Report.AddRange(sql.MapRows(MapEntry));
            result.CanLoadMore = (result.Report.Count == c_pageSize);

            foreach (var b in result.Report)
            {
                if (b.IsClosed)
                {
                    b.AvailableAmount = "0";
                }
                else
                {
                    var available = m_batchFacade.GetAvailableAmount(b.BatchId);
                    b.AvailableAmount = $"{StringUtil.FormatDecimal(available.Value)} {available.Unit.Symbol}";
                }
            }

            return result;
        }

        private BatchReportEntry MapEntry(DbDataReader row)
        {
            #region Column ordinals
            const int batchId = 0;
            const int inventoryName = 1;
            const int batchNumber = 2;
            const int materialName = 3;
            const int materialId = 4;
            const int batchVolume = 5;
            const int unit = 6;
            const int batchCreateDt = 7;
            const int batchCloseDt = 8;
            const int batchLockDt = 9;
            const int batchAvailable = 10;
            const int batchProductionDt = 11;
            const int batchStepsDone = 12;
            const int numberOfComponents = 13;
            const int numberOfCompositions = 14;
            const int numberOfRequiredSteps = 15;
            const int numberOfOrders = 16;
            #endregion


            var entry = new BatchReportEntry
            {
                BatchId = row.GetInt32(batchId),
                InventoryName = row.GetString(inventoryName),
                BatchNumber = row.GetString(batchNumber),
                MaterialName = row.GetString(materialName),
                MaterialId = row.GetInt32(materialId),
                BatchVolume = $"{StringUtil.FormatDecimal(row.GetDecimal(batchVolume))} {row.GetString(unit)}",
                CreateDt = StringUtil.FormatDateTime(row.GetDateTime(batchCreateDt)),
                IsClosed = !row.IsDBNull(batchCloseDt),
                IsLocked = !row.IsDBNull(batchLockDt),
                IsAvailable = row.GetBoolean(batchAvailable),
                AllStepsDone = (!row.IsDBNull(batchStepsDone)) && row.GetBoolean(batchStepsDone),
                NumberOfComponents = row.GetInt32(numberOfComponents),
                NumberOfCompositions = row.GetInt32(numberOfCompositions),
                NumberOfRequiredSteps = row.GetInt32(numberOfRequiredSteps),
                NumberOfOrders = row.GetInt32(numberOfOrders)
            };
            
            return entry;
        }
    }
}
