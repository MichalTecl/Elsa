IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_SentOrdersWithoutAssignedItems')
BEGIN
	DROP PROCEDURE insp_SentOrdersWithoutAssignedItems;
END

GO

CREATE PROCEDURE insp_SentOrdersWithoutAssignedItems
(
	@projectId INT
)
AS
BEGIN
	
	DECLARE @issues TABLE (id INT IDENTITY(1,1), OrderNumber NVARCHAR(50), ItemName NVARCHAR(500), OrderQty DECIMAL(19, 5), AssignedQty DECIMAL(19, 5));
		
	DECLARE @startDt DATETIME = (SELECT MIN(AssignmentDt) FROM OrderItemMaterialBatch);
	
	INSERT INTO @issues (OrderNumber, ItemName, OrderQty, AssignedQty)
	SELECT po.OrderNumber, ISNULL(ki.PlacedName, oi.PlacedName) as ItemName, ISNULL(ki.Quantity, oi.Quantity) Objednane_Mnozstvi, ISNULL(assignment.qty, 0) as Prirazene_Mnozstvi
	  FROM PurchaseOrder po
	  JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
	  LEFT JOIN OrderItem ki ON (ki.KitParentId = oi.Id)
	  LEFT JOIN (
		SELECT oimb.OrderItemId, SUM(oimb.Quantity) qty
		  FROM OrderItemMaterialBatch oimb
		 GROUP BY oimb.OrderItemId
	  ) as assignment ON (assignment.OrderItemId = ISNULL(ki.Id, oi.Id))
	WHERE po.ProjectId = @projectId 
	AND po.OrderStatusId = 5
	AND ABS(ISNULL(ki.Quantity, oi.Quantity) - ISNULL(assignment.qty, 0)) > 0
	AND po.PurchaseDate > @startDt
	ORDER BY po.BuyDate, po.OrderNumber;

	WHILE EXISTS(SELECT TOP 1 1 FROM @issues)
	BEGIN
		DECLARE @code NVARCHAR(500);
		DECLARE @text NVARCHAR(1000);

		SELECT TOP 1 @code = (OrderNumber + ItemName + STR(AssignedQty)),
		             @text = (N'Objednávka ' + OrderNumber + N' byla odeslána s nesprávným množstvím položky ' + ItemName + N'. Objednáno ' + TRIM(STR(OrderQty)) + N' ks, Přiřazeno ' + TRIM(STR(AssignedQty)) + ' ks') 
		 FROM @issues ORDER BY Id;
		
		DECLARE @issueId UNIQUEIDENTIFIER = NEWID();

		EXEC inspfw_addIssue @issueId, @code, @text;

		DELETE FROM @issues WHERE Id = (SELECT TOP 1 Id FROM @issues ORDER BY Id);
	END

END


