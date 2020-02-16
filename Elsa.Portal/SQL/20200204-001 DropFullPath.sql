IF EXISTS(SELECT TOP 1 1 FROM sys.columns c
          JOIN sys.tables t ON (c.object_id = t.object_id)
		  WHERE t.name = 'UserRight'
		    AND c.name = 'FullPath')
BEGIN
	ALTER TABLE Userright DROP COLUMN FullPath;
END