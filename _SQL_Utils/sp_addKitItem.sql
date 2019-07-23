--DROP PROCEDURE sp_addKitItem

CREATE PROCEDURE sp_addKitItem (
    @projectId INT,
	@kitName NVARCHAR(200), 
	@groupName NVARCHAR(200),
	@it1 NVARCHAR(300) = null, 
	@it2 NVARCHAR(300) = NULL, 
	@it3 NVARCHAR(300) = NULL, 
	@it4 NVARCHAR(300) = NULL, 
	@it5 NVARCHAR(300) = NULL, 
	@it6 NVARCHAR(300) = NULL, 
	@it7 NVARCHAR(300) = NULL)
AS
BEGIN
	DECLARE @alts TABLE (name NVARCHAR(300));

	INSERT INTO @alts 
	SELECT DISTINCT x.n
	  FROM (SELECT @it1 as n UNION
			SELECT @it2 as n UNION
			SELECT @it3 as n UNION
			SELECT @it4 as n UNION
			SELECT @it5 as n UNION
			SELECT @it6 as n UNION
			SELECT @it7 as n) as x
	  WHERE x.n IS NOT NULL;
	
	IF NOT EXISTS(SELECT TOP 1 1 FROM OrderItem WHERE PlacedName = @kitName)
	BEGIN
		PRINT 'NEEXISTUJE: ' + @kitName;
		RETURN;
	END

	DECLARE @noex NVARCHAR(300);
	SET @noex = (SELECT TOP 1 name FROM @alts a WHERE NOT EXISTS (SELECT TOP 1 1 FROM OrderItem WHERE PlacedName = a.name));
	IF (@noex IS NOT NULL)
	BEGIN
		PRINT 'NEEXISTUJE: ' + @noex;
		RETURN;
	END

	  
	DECLARE @kitDefinitoinId INT;

	SET @kitDefinitoinId = (SELECT TOP 1 Id FROM KitDefinition k WHERE k.ProjectId = @projectId AND k.ItemName = @kitName);

	IF (@kitDefinitoinId IS NULL)
	BEGIN
		INSERT INTO KitDefinition (ProjectId, ItemName) VALUES (@projectId, @kitName);
		SET @kitDefinitoinId = SCOPE_IDENTITY();
	END

	DECLARE @groupId INT;
	SET @groupId = (SELECT TOP 1 Id FROM KitSelectionGroup g WHERE g.KitDefinitionId = @kitDefinitoinId AND g.Name = @groupName);
	IF (@groupId IS NULL)
	BEGIN
		INSERT INTO KitSelectionGroup (KitDefinitionId, Name) VALUES (@kitDefinitoinId, @groupName);
		SET @groupId = SCOPE_IDENTITY();
	END

	WHILE(1 = 1)
	BEGIN
		DECLARE @alt NVARCHAR(300);
		SET @alt = (SELECT TOP 1 name FROM @alts ORDER BY name);
		IF (@alt IS NULL)
		BEGIN
			RETURN;
		END
		DELETE FROM @alts WHERE name = @alt;

		IF NOT EXISTS(SELECT TOP 1 1 FROM KitSelectionGroupItem i WHERE i.KitSelectionGroupId = @groupId AND i.ItemName = @alt)
		BEGIN
			DECLARE @shortcut NVARCHAR(300) = @alt;
			IF (LEN(@shortcut) > 64)
			BEGIN
				set @shortcut = 'TODO';
			END
			INSERT INTO KitSelectionGroupItem (ItemName, KitSelectionGroupId, Shortcut) VALUES (@alt, @groupId, @shortcut);
		END
	END	
END	 

