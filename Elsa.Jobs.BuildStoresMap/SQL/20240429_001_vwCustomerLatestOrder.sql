IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERE name = 'vwCustomerLatestOrder')
	DROP VIEW vwCustomerLatestOrder;

GO

CREATE VIEW vwCustomerLatestOrder
AS
SELECT c.ProjectId,
       c.Id CustomerId, 
       lastSuccPo.Id LatestSuccessOrderId,
	   lastSuccPo.PurchaseDate LatestSuccessOrderDt,
	   lastPo.Id LatestAnyOrderId,
	   lastPo.PurchaseDate LatestAnyOrderDt
  FROM Customer c
  LEFT JOIN (SELECT po.CustomerErpUid, MAX(po.PurchaseDate) Dt
          FROM PurchaseOrder po
		 WHERE po.OrderStatusId = 5
		 GROUP BY po.CustomerErpUid) lastSuccPoDt ON (c.ErpUid = lastSuccPoDt.CustomerErpUid) 
  LEFT JOIN (SELECT po.CustomerErpUid, MAX(po.PurchaseDate) Dt
          FROM PurchaseOrder po
		  GROUP BY po.CustomerErpUid) lastPoDt ON (c.ErpUid = lastPoDt.CustomerErpUid) 
  LEFT JOIN PurchaseOrder lastSuccPo ON (lastSuccPo.CustomerErpUid = lastSuccPoDt.CustomerErpUid AND lastSuccPo.PurchaseDate = lastSuccPoDt.Dt) 
  LEFT JOIN PurchaseOrder lastPo ON (lastPo.CustomerErpUid = lastPoDt.CustomerErpUid AND lastPo.PurchaseDate = lastPoDt.Dt)
WHERE c.ProjectId = ISNULL(lastSuccPo.ProjectId, c.ProjectId)
  AND c.ProjectId = ISNULL(lastPo.ProjectId, c.ProjectId)