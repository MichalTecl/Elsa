IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'unassignEshopItemMaterial')
	DROP PROCEDURE unassignEshopItemMaterial;

GO

CREATE PROCEDURE unassignEshopItemMaterial (@projectId INT, @erpId INT, @erpProductName NVARCHAR(255), @materialName NVARCHAR(255))
AS
BEGIN
	DECLARE @mappingId INT;
	DECLARE @vpId INT;

	SELECT @mappingId = vpim.Id, @vpId = vp.Id
	  FROM VirtualProductOrderItemMapping vpim
	  JOIN VirtualProduct vp ON (vpim.VirtualProductId = vp.Id)
	  JOIN VirtualProductMaterial vpm ON (vp.Id = vpm.VirtualProductId)
	  JOIN Material m ON (vpm.ComponentId = m.Id)
	 WHERE vpim.ProjectId = @projectId
	   AND vp.ProjectId = @projectId
	   AND ISNULL(vpim.ErpId, @erpId) = @erpId
	   AND vpim.ItemName = @erpProductName
	   AND m.Name = @materialName;

	IF (@mappingId IS NULL)
	BEGIN
		RETURN;
	END

	DELETE FROM VirtualProductOrderItemMapping WHERE Id = @mappingId;

	IF NOT EXISTS(SELECT TOP 1 1 FROM VirtualProductOrderItemMapping WHERE VirtualProductId = @vpId)
	BEGIN
		DELETE FROM VirtualProductMaterial WHERE VirtualProductId = @vpId;
		DELETE FROM VirtualProduct WHERE Id = @vpId;
	END
END