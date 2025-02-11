IF NOT EXISTS (SELECT 1 FROM sys.types WHERE name = 'IntTable' AND is_table_type = 1)
BEGIN
    CREATE TYPE IntTable AS TABLE
    (
        Id INT PRIMARY KEY
    );
END;
