DELETE FROM FixedCostValue 
WHERE FixedCostTypeId IN (
SELECT Id
  FROM FixedCostType 
 WHERE Name = N'Vybavení provozovny (potřebné pro výrobu)');

DELETE FROM FixedCostType WHERE Name = N'Vybavení provozovny (potřebné pro výrobu)';