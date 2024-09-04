IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'getOneClickProductionOptions' AND type = 'P')
	DROP PROCEDURE getOneClickProductionOptions

GO

CREATE PROCEDURE getOneClickProductionOptions(@projectId INT)
AS
BEGIN
	select mb.BatchNumber, 
       mb.MaterialId SourceMaterialId,
	   r.Id RecipeId, 
	   composition.Name ProducedMaterialName,
	   composition.Id ProducedMaterialId,
	   r.VisibleForUserRole, 	
	   mu.Symbol ProducibleAmountUnit,
	   SUM(dbo.ConvertToUnit(@projectId, bam.Available, bam.UnitId, rc.UnitId) / rc.Amount * r.RecipeProducedAmount) ProducibleAmount
	   
  from MaterialBatch mb  
  join RecipeComponent rc ON (rc.MaterialId = mb.MaterialId)
  join Recipe r ON (rc.RecipeId = r.Id)
  join Material composition ON (r.ProducedMaterialId = composition.Id)
  join vwBatchAvailableAmount bam ON (mb.Id = bam.BatchId)
  join MaterialUnit mu ON (r.ProducedAmountUnitId = mu.Id)
   
where r.DeleteUserId is null
  and rc.IsTransformationInput = 1
  and r.AllowOneClickProduction = 1
  and bam.Available > 0
  and r.ProjectId = @projectId  

GROUP BY mb.BatchNumber, 
       mb.MaterialId,
	   r.Id, 
	   composition.Name,
	   composition.Id,
	   r.VisibleForUserRole,
	   mu.Symbol;
END