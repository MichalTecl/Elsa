IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_missingWeight')
BEGIN
	DROP PROCEDURE insp_missingWeight;
END

GO

CREATE PROCEDURE [insp_missingWeight] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN

	DECLARE @items TABLE (ItemName NVARCHAR(255));
	INSERT INTO @items 
	SELECT DISTINCT PlacedName
	  FROM OrderItem oi
	  JOIN PurchaseOrder po ON (oi.PurchaseOrderId = po.Id)
	 WHERE po.PurchaseDate > (GETDATE() - 31)
	   AND ISNULL(oi.Weight, 0) = 0
	   AND po.OrderStatusId < 4;
	   	   		    	
     WHILE(EXISTS(SELECT TOP 1 1 FROM @items))
	 BEGIN
		DECLARE @code NVARCHAR(200);
		DECLARE @message NVARCHAR(2000);
		DECLARE @itemName NVARCHAR(200) = (SELECT TOP 1 ItemName FROM @items);
	
		SET @code = 'missingWeight_' + @itemName;
		SET @message = N'Položka "' + @itemName + N'" nemá hmotnost';
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Chybějící hmotnost produktu', @code, @message;

		DELETE FROM @items WHERE ItemName = @itemName;
	 END
END
GO


