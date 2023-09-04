INSERT INTO CustomerGroupType (ProjectId, ErpGroupName, IsDistributor, IsDisabled)
SELECT *
  FROM 
  (
    SELECT 1 pid, N'Přátelé a parťáci' GN, 1 distributor, 0 dis UNION
    SELECT 1, N'NOVÍ velkoodběratelé (od 16.11.2018)', 1, 0 UNION
    SELECT 1, N'Poslední obj. 2021 a dříve', 0, 1	
  ) x
WHERE NOT EXISTS(SELECT TOP 1 1 FROM CustomerGroupType WHERE ErpGroupName = x.GN);  
 