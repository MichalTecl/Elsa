IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'ParseKitNote')
BEGIN
	DROP PROCEDURE [ParseKitNote];
END

GO

CREATE PROCEDURE [ParseKitNote](@projectId INT, @orderId INT = NULL)
AS
BEGIN
    DECLARE @cuts TABLE (
        Id INT IDENTITY(1,1), 
        OrderId BIGINT, 
        KitNr INT, 
        KitName NVARCHAR(1000), 
        ItemType NVARCHAR(1000), 
        Item NVARCHAR(1000),
		KitDefinitionId INT,
		SelectionGroupId INT,
		SelectionGroupItemId INT
    );


	DECLARE @orders TABLE(OrderId BIGINT, Note NVARCHAR(4000));
	
	IF (@orderId IS NOT NULL)
	BEGIN
		INSERT INTO @orders 
		SELECT Id, CustomerNote 
		  FROM PurchaseOrder po
		 WHERE po.ProjectId = @projectId
		   AND po.Id = @orderId;
	END
	ELSE
	BEGIN
		INSERT INTO @orders
		SELECT DISTINCT po.Id, po.CustomerNote
		  FROM KitDefinition kd
		  JOIN KitSelectionGroup ksg on ksg.KitDefinitionId = kd.id
		  JOIN KitSelectionGroupItem kgi ON kgi.KitSelectionGroupId = ksg.Id
		  JOIN (SELECT k.KitSelectionGroupId GroupId
				  FROM KitSelectionGroupItem k
				GROUP BY k.KitSelectionGroupId
				HAVING COUNT(k.Id) > 1) selectable ON  (selectable.GroupId = ksg.Id)
		  JOIN OrderItem oi ON (oi.PlacedName = kd.ItemName)
		  JOIN PurchaseOrder po ON (oi.PurchaseOrderId = po.Id)
		 WHERE po.OrderStatusId = 3
		   AND po.ProjectId = @projectId;
	END

	
	WHILE EXISTS(SELECT TOP 1 1 FROM @orders)
	BEGIN
    
		DECLARE @inp NVARCHAR(MAX);

		SELECT TOP 1 @orderId = OrderId, @inp = Note FROM @orders;
		DELETE FROM @orders 
		 WHERE OrderId = @orderId;
    
		SELECT TOP 1 @inp = CustomerNote 
		FROM PurchaseOrder 
		WHERE Id = @orderId;

		DECLARE @dataMarker NVARCHAR(6) = '------';
		DECLARE @n INT;

		SET @n = CHARINDEX(@dataMarker, @inp);
		IF (@n > 0)
		BEGIN    
			SET @n = @n + LEN(@dataMarker);
			SET @inp = SUBSTRING(@inp, @n, LEN(@inp) - @n + 1);
		END

		SET @n = CHARINDEX(@dataMarker, @inp);
		IF (@n > 0)
		BEGIN    
			SET @inp = LEFT(@inp, @n - 1);
		END

		SET @inp = REPLACE(@inp, '  ', '#');
		SET @inp = REPLACE(@inp, CHAR(9), ' ');  -- Tabulátor
		SET @inp = REPLACE(@inp, CHAR(10), ' '); -- LF
		SET @inp = REPLACE(@inp, CHAR(13), ' '); -- CR

		WHILE (CHARINDEX('  ', @inp) > 0)
		BEGIN
			SET @inp = REPLACE(@inp, '  ', ' ');
		END

		WHILE (CHARINDEX(' #', @inp) > 0)
		BEGIN
			SET @inp = REPLACE(@inp, ' #', '#');
		END

		SET @inp = REPLACE(@inp, N'Do sady', N'Do 1. sady'); 
		SET @inp = TRIM(@inp);

		PRINT @inp;

		WHILE (1 = 1)
		BEGIN
			PRINT '------';

			DECLARE @pos INT = PATINDEX('%Do _. sady%', @inp);
			IF (@pos = 0)
				BREAK;

			DECLARE @kitNr INT;
			DECLARE @kitName NVARCHAR(200);
        
			SET @inp = SUBSTRING(@inp, @pos + 3, LEN(@inp) - @pos + 1);
        
			SET @kitNr = CAST(SUBSTRING(@inp, 1, 1) AS INT);

			SET @pos = @pos + 8;
        
			SET @inp = SUBSTRING(@inp, @pos, LEN(@inp) - @pos + 1);
        
			PRINT @inp;

			DECLARE @kitNameEndPos INT = PATINDEX('%si přeji:#%', @inp);
			PRINT @kitNameEndPos;

			SET @kitName = SUBSTRING(@inp, 0, @kitNameEndPos);
			PRINT @kitName;

			SET @pos = @kitNameEndPos + LEN(N'si přeji:#');

			DECLARE @nextPos INT = PATINDEX('%Do _. sady%', @inp);
			IF (@nextPos = 0)
				SET @nextPos = LEN(@inp) + 1;

			DECLARE @kitContent NVARCHAR(1000) = SUBSTRING(@inp, @pos, @nextPos - @pos);

			SET @pos = @nextPos;
			SET @inp = SUBSTRING(@inp, @pos, LEN(@inp) - @pos + 1);
			SET @pos = 0;    

			WITH cte
			AS
			(
				SELECT @orderId OrderId, @kitNr KitNr, TRIM(@kitName) KitName, 
					   TRIM(LEFT(value, CHARINDEX(':', value) - 1)) ItemTypeName,
					   TRIM(SUBSTRING(value, CHARINDEX(':', value) + 1, LEN(value)))  Item   
				FROM STRING_SPLIT(@kitContent, '#')
			)
			INSERT INTO @cuts (OrderId, KitNr, KitName, ItemType, Item, KitDefinitionId,  SelectionGroupId,	SelectionGroupItemId)
			SELECT c.*, kd.Id, kg.Id, ki.id
			  FROM cte c
			  LEFT JOIN KitDefinition kd ON (kd.ItemName = @kitName)
			  LEFT JOIN KitSelectionGroup kg ON (kg.KitDefinitionId = kd.Id AND kg.InTextMarker = c.ItemTypeName)
			  LEFT JOIN KitSelectionGroupItem ki ON (ki.KitSelectionGroupId = kg.Id AND ki.InTextMarker = c.Item)
			 WHERE kd.ProjectId = @projectId;
		END

		IF NOT EXISTS(SELECT TOP 1 1 FROM @cuts WHERe OrderId = @orderId)
		INSERT INTO @cuts (OrderId) 
		SELECT @orderId;
	END

	SELECT * FROM @cuts;
END;





