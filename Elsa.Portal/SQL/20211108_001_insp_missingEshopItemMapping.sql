IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_missingEshopItemMapping')
BEGIN
	DROP PROCEDURE insp_missingEshopItemMapping;
END

GO

CREATE PROCEDURE [dbo].[insp_missingEshopItemMapping] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @items TABLE (ItemName NVARCHAR(255));
	INSERT INTO @items 
	SELECT DISTINCT PlacedName
	  FROM OrderItem oi
	  JOIN PurchaseOrder po ON (oi.PurchaseOrderId = po.Id)
	 WHERE po.PurchaseDate > (GETDATE() - 31)
	   AND PlacedName NOT IN (SELECT ItemName FROM VirtualProductOrderItemMapping)   
	   AND PlacedName NOT IN (SELECt ItemName FROM KitDefinition);
	   		    	
     WHILE(EXISTS(SELECT TOP 1 1 FROM @items))
	 BEGIN
		DECLARE @code NVARCHAR(200);
		DECLARE @message NVARCHAR(2000);
		DECLARE @itemName NVARCHAR(200) = (SELECT TOP 1 ItemName FROM @items);
	
		SET @code = 'missingMapping_' + @itemName;
		SET @message = N'Položka e-shopu "' + @itemName + N'" není propojená s materiálem.';
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Neznámý produkt v e-shopu', @code, @message;

		DELETE FROM @items WHERE ItemName = @itemName;
	 END
END
GO


