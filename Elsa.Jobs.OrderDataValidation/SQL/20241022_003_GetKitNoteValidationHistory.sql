IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'GetKitNoteValidationHistory')
	DROP PROCEDURE GetKitNoteValidationHistory;

GO

CREATE PROCEDURE GetKitNoteValidationHistory(@projectId INT)
AS
BEGIN
	SELECT kif.*, 
		   knv.ValidationDt LastValidationDt,
		   knv.CustomerNoteHash CustomerNoteHash,
		   knv.IsValid
	  FROM vwOrderKitInfo kif
	  JOIN PurchaseOrder po ON (kif.OrderId = po.Id)
	  LEFT JOIN (
		SELECT r.PurchaseOrderId, MAX(r.Id) ResultId
		 FROM KitNoteValidationResult r
		 GROUP BY r.PurchaseOrderId) latestResult ON (latestResult.PurchaseOrderId = po.Id)
	  LEFT JOIN KitNoteValidationResult knv ON (latestResult.ResultId = knv.Id)
	 WHERE po.ProjectId = @projectId
	   AND po.OrderStatusId < 5
	   AND kif.RequiresSelection = 1
	   AND ((knv.Id IS NULL) OR (knv.OrderHash <> po.OrderHash));
END

