INSERT INTO Recipe (RecipeName, ValidFrom, CreateUserId, ProducedMaterialId, RecipeProducedAmount, ProducedAmountUnitId, ProjectId)
SELECT m.Name, GETDATE(), 2, m.Id, m.NominalAmount, m.NominalUnitId, m.ProjectId
  FROM Material m
 WHERE EXISTS (SELECT TOP 1 1 FROM MaterialComposition mc WHERE mc.CompositionId = m.Id)
   AND NOT EXISTS(SELECT TOP 1 1 FROM Recipe sr WHERE sr.ProducedMaterialId = m.Id);

INSERT INTO RecipeComponent (RecipeId, MaterialId, Amount, UnitId, IsTransformationInput, SortOrder)  
SELECT DISTINCT r.Id, mc.ComponentId, mc.Amount, mc.UnitId, 0, mc.Id
  FROM Recipe r  
  JOIN MaterialComposition mc ON (r.ProducedMaterialId = mc.CompositionId)
WHERE NOT EXISTS(SELECT TOP 1 1 FROM RecipeComponent src WHERE src.RecipeId = r.Id);    


DECLARE @batchIds TABLE(bid INT);

INSERT INTO @batchIds
SELECT mb.Id
  FROM MaterialBatch mb
 WHERE mb.RecipeId IS NULL
   AND mb.MaterialId IN (SELECT ProducedMaterialId FROM Recipe);

