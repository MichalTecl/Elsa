INSERT INTO SysConfig ([Key], ValueJson, ValidFrom, InsertUserId)
SELECT x.K, x.V, GETDATE(), 2
FROM (
SELECT 'RayNet.UserName' K, '"emailHere@xxx"' V UNION
SELECT 'RayNet.InstanceName', '"instanceName"' UNION
SELECT 'RayNet.ApiKey', '"psssst"'
) x
WHERE NOT EXISTS(SELECT NULL FROM SysConfig e WHERE e.[Key] = x.K)