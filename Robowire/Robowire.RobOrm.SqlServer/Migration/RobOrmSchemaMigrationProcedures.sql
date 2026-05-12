IF EXISTS (SELECT 1 FROM sys.procedures WHERE object_id = OBJECT_ID(N'[dbo].[robormsp_ensuretable]'))
    DROP PROCEDURE [dbo].[robormsp_ensuretable];
GO

CREATE PROCEDURE [dbo].[robormsp_ensuretable]
    @table_name NVARCHAR(128),
    @pk_column_name NVARCHAR(128),
    @pk_sql_type NVARCHAR(128),
    @pk_identity BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @normalized_pk_sql_type NVARCHAR(128) = LOWER(REPLACE(@pk_sql_type, N' ', N''));
    DECLARE @table_object_id INT;
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @pk_identity_int INT = CASE WHEN @pk_identity = 1 THEN 1 ELSE 0 END;

    RAISERROR(N'robormsp_ensuretable: checking table %s, PK %s %s, identity=%d', 10, 1, @table_name, @pk_column_name, @pk_sql_type, @pk_identity_int) WITH NOWAIT;

    IF @normalized_pk_sql_type NOT IN (N'int', N'bigint')
    BEGIN
        RAISERROR(N'robormsp_ensuretable supports only int and bigint primary keys', 16, 1);
        RETURN;
    END;

    SELECT TOP 1
        @table_object_id = t.object_id
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.name = @table_name
      AND s.name = N'dbo';

    IF @table_object_id IS NOT NULL
    BEGIN
        RAISERROR(N'robormsp_ensuretable: table %s already exists, skipping', 10, 1, @table_name) WITH NOWAIT;
        RETURN;
    END;

    RAISERROR(N'robormsp_ensuretable: creating table %s', 10, 1, @table_name) WITH NOWAIT;
    SET @sql = N'CREATE TABLE [dbo].' + QUOTENAME(@table_name) + N' (' +
        QUOTENAME(@pk_column_name) + N' ' + @normalized_pk_sql_type +
        CASE WHEN @pk_identity = 1 THEN N' IDENTITY(1,1)' ELSE N'' END +
        N' NOT NULL, CONSTRAINT ' + QUOTENAME(N'PK_' + @table_name) +
        N' PRIMARY KEY CLUSTERED (' + QUOTENAME(@pk_column_name) + N'))';

    EXEC(@sql);
    RAISERROR(N'robormsp_ensuretable: table %s created', 10, 1, @table_name) WITH NOWAIT;
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE object_id = OBJECT_ID(N'[dbo].[robormsp_ensurecolumn]'))
    DROP PROCEDURE [dbo].[robormsp_ensurecolumn];
GO

CREATE PROCEDURE [dbo].[robormsp_ensurecolumn]
    @table_name NVARCHAR(128),
    @column_name NVARCHAR(128),
    @column_sql_type NVARCHAR(128),
    @allows_nulls BIT,
    @unique BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @table_object_id INT;
    DECLARE @table_schema_name SYSNAME;
    DECLARE @column_id INT;
    DECLARE @actual_column_sql_type NVARCHAR(128);
    DECLARE @actual_allows_nulls BIT;
    DECLARE @actual_unique BIT;
    DECLARE @unique_constraint_name SYSNAME = N'UQ_' + @table_name + N'_' + @column_name;
    DECLARE @normalized_column_sql_type NVARCHAR(128) = LOWER(REPLACE(@column_sql_type, N' ', N''));
    DECLARE @needs_column_alter BIT = 0;
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @allows_nulls_int INT = CASE WHEN @allows_nulls = 1 THEN 1 ELSE 0 END;
    DECLARE @unique_int INT = CASE WHEN @unique = 1 THEN 1 ELSE 0 END;
    DECLARE @actual_allows_nulls_int INT = 0;

    RAISERROR(N'robormsp_ensurecolumn: checking %s.%s => %s, nullable=%d, unique=%d', 10, 1, @table_name, @column_name, @column_sql_type, @allows_nulls_int, @unique_int) WITH NOWAIT;

    SELECT TOP 1
        @table_object_id = t.object_id,
        @table_schema_name = s.name
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.name = @table_name
      AND s.name = N'dbo';

    IF @table_object_id IS NULL
    BEGIN
        RAISERROR(N'robormsp_ensurecolumn expected an existing table', 16, 1);
        RETURN;
    END;

    SELECT
        @column_id = c.column_id,
        @actual_column_sql_type = LOWER(
            CASE
                WHEN ty.name IN (N'decimal', N'numeric') THEN ty.name + N'(' + CAST(c.precision AS NVARCHAR(10)) + N',' + CAST(c.scale AS NVARCHAR(10)) + N')'
                WHEN ty.name IN (N'nvarchar', N'nchar') THEN ty.name + N'(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length / 2 AS NVARCHAR(10)) END + N')'
                WHEN ty.name IN (N'varchar', N'char', N'varbinary', N'binary') THEN ty.name + N'(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length AS NVARCHAR(10)) END + N')'
                ELSE ty.name
            END),
        @actual_allows_nulls = CASE WHEN c.is_nullable = 1 THEN 1 ELSE 0 END
    FROM sys.columns c
    JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = @table_object_id
      AND c.name = @column_name;

    IF @column_id IS NULL
    BEGIN
        RAISERROR(N'robormsp_ensurecolumn: adding missing column %s.%s as nullable bootstrap', 10, 1, @table_name, @column_name) WITH NOWAIT;
        SET @sql = N'ALTER TABLE ' + QUOTENAME(@table_schema_name) + N'.' + QUOTENAME(@table_name) +
            N' ADD ' + QUOTENAME(@column_name) + N' ' + @normalized_column_sql_type + N' NULL;';
        EXEC(@sql);

        SELECT
            @column_id = c.column_id,
            @actual_column_sql_type = LOWER(
                CASE
                    WHEN ty.name IN (N'decimal', N'numeric') THEN ty.name + N'(' + CAST(c.precision AS NVARCHAR(10)) + N',' + CAST(c.scale AS NVARCHAR(10)) + N')'
                    WHEN ty.name IN (N'nvarchar', N'nchar') THEN ty.name + N'(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length / 2 AS NVARCHAR(10)) END + N')'
                    WHEN ty.name IN (N'varchar', N'char', N'varbinary', N'binary') THEN ty.name + N'(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length AS NVARCHAR(10)) END + N')'
                    ELSE ty.name
                END),
            @actual_allows_nulls = CASE WHEN c.is_nullable = 1 THEN 1 ELSE 0 END
        FROM sys.columns c
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = @table_object_id
          AND c.name = @column_name;
    END;

    SELECT
        @actual_unique = CASE WHEN EXISTS(
            SELECT 1
            FROM sys.key_constraints kc
            WHERE kc.parent_object_id = @table_object_id
              AND kc.type = 'UQ'
              AND kc.name = @unique_constraint_name) THEN 1 ELSE 0 END;

    IF @actual_column_sql_type <> @normalized_column_sql_type
       OR ISNULL(@actual_allows_nulls, 0) <> CASE WHEN @allows_nulls = 1 THEN 1 ELSE 0 END
    BEGIN
        SET @needs_column_alter = 1;
    END;

    SET @actual_allows_nulls_int = ISNULL(@actual_allows_nulls, 0);

    IF @needs_column_alter = 0
       AND ISNULL(@actual_unique, 0) = CASE WHEN @unique = 1 THEN 1 ELSE 0 END
    BEGIN
        RAISERROR(N'robormsp_ensurecolumn: column %s.%s already matches requested definition', 10, 1, @table_name, @column_name) WITH NOWAIT;
        RETURN;
    END;

    DECLARE @foreignKeys TABLE
    (
        ConstraintName SYSNAME NOT NULL,
        DropSql NVARCHAR(MAX) NOT NULL,
        CreateSql NVARCHAR(MAX) NOT NULL
    );

    IF @needs_column_alter = 1
    BEGIN
        RAISERROR(N'robormsp_ensurecolumn: altering column %s.%s from %s nullable=%d to %s nullable=%d', 10, 1, @table_name, @column_name, @actual_column_sql_type, @actual_allows_nulls_int, @normalized_column_sql_type, @allows_nulls_int) WITH NOWAIT;

        INSERT INTO @foreignKeys(ConstraintName, DropSql, CreateSql)
        SELECT
            fk.name,
            N'ALTER TABLE ' + QUOTENAME(ps.name) + N'.' + QUOTENAME(pt.name) + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';',
            N'ALTER TABLE ' + QUOTENAME(ps.name) + N'.' + QUOTENAME(pt.name) +
                N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(fk.name) +
                N' FOREIGN KEY (' + pc.ParentColumns + N') REFERENCES ' +
                QUOTENAME(rs.name) + N'.' + QUOTENAME(rt.name) +
                N' (' + rc.ReferencedColumns + N'); ALTER TABLE ' +
                QUOTENAME(ps.name) + N'.' + QUOTENAME(pt.name) +
                N' CHECK CONSTRAINT ' + QUOTENAME(fk.name) + N';'
        FROM sys.foreign_keys fk
        JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
        JOIN sys.schemas ps ON ps.schema_id = pt.schema_id
        JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
        JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
        CROSS APPLY
        (
            SELECT STUFF((
                SELECT N', ' + QUOTENAME(pc2.name)
                FROM sys.foreign_key_columns fkc2
                JOIN sys.columns pc2
                    ON pc2.object_id = fkc2.parent_object_id
                   AND pc2.column_id = fkc2.parent_column_id
                WHERE fkc2.constraint_object_id = fk.object_id
                ORDER BY fkc2.constraint_column_id
                FOR XML PATH(N''), TYPE).value(N'.', N'nvarchar(max)'), 1, 2, N'') AS ParentColumns
        ) pc
        CROSS APPLY
        (
            SELECT STUFF((
                SELECT N', ' + QUOTENAME(rc2.name)
                FROM sys.foreign_key_columns fkc2
                JOIN sys.columns rc2
                    ON rc2.object_id = fkc2.referenced_object_id
                   AND rc2.column_id = fkc2.referenced_column_id
                WHERE fkc2.constraint_object_id = fk.object_id
                ORDER BY fkc2.constraint_column_id
                FOR XML PATH(N''), TYPE).value(N'.', N'nvarchar(max)'), 1, 2, N'') AS ReferencedColumns
        ) rc
        WHERE EXISTS(
            SELECT 1
            FROM sys.foreign_key_columns fkc
            WHERE fkc.constraint_object_id = fk.object_id
              AND (
                    (fkc.parent_object_id = @table_object_id AND fkc.parent_column_id = @column_id)
                 OR (fkc.referenced_object_id = @table_object_id AND fkc.referenced_column_id = @column_id)
              ));

        DECLARE @dropSql NVARCHAR(MAX);
        DECLARE @dropped_fk_count INT = 0;
        DECLARE drop_fk_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT DropSql FROM @foreignKeys ORDER BY ConstraintName;

        OPEN drop_fk_cursor;
        FETCH NEXT FROM drop_fk_cursor INTO @dropSql;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC(@dropSql);
            SET @dropped_fk_count = @dropped_fk_count + 1;
            FETCH NEXT FROM drop_fk_cursor INTO @dropSql;
        END;
        CLOSE drop_fk_cursor;
        DEALLOCATE drop_fk_cursor;

        IF ISNULL(@actual_unique, 0) = 1
        BEGIN
            RAISERROR(N'robormsp_ensurecolumn: dropping unique constraint %s before altering %s.%s', 10, 1, @unique_constraint_name, @table_name, @column_name) WITH NOWAIT;
            SET @sql = N'ALTER TABLE ' + QUOTENAME(@table_schema_name) + N'.' + QUOTENAME(@table_name) +
                N' DROP CONSTRAINT ' + QUOTENAME(@unique_constraint_name) + N';';
            EXEC(@sql);

            SET @actual_unique = 0;
        END;

        SET @sql = N'ALTER TABLE ' + QUOTENAME(@table_schema_name) + N'.' + QUOTENAME(@table_name) +
            N' ALTER COLUMN ' + QUOTENAME(@column_name) + N' ' + @normalized_column_sql_type +
            CASE WHEN @allows_nulls = 1 THEN N' NULL;' ELSE N' NOT NULL;' END;
        EXEC(@sql);

        RAISERROR(N'robormsp_ensurecolumn: column %s.%s altered, dropped FK=%d', 10, 1, @table_name, @column_name, @dropped_fk_count) WITH NOWAIT;
    END;

    IF @unique = 1 AND ISNULL(@actual_unique, 0) = 0
    BEGIN
        RAISERROR(N'robormsp_ensurecolumn: creating unique constraint %s on %s.%s', 10, 1, @unique_constraint_name, @table_name, @column_name) WITH NOWAIT;
        SET @sql = N'ALTER TABLE ' + QUOTENAME(@table_schema_name) + N'.' + QUOTENAME(@table_name) +
            N' ADD CONSTRAINT ' + QUOTENAME(@unique_constraint_name) +
            N' UNIQUE NONCLUSTERED (' + QUOTENAME(@column_name) + N');';
        EXEC(@sql);
    END;
    ELSE IF @unique = 0 AND ISNULL(@actual_unique, 0) = 1
    BEGIN
        RAISERROR(N'robormsp_ensurecolumn: dropping unique constraint %s on %s.%s', 10, 1, @unique_constraint_name, @table_name, @column_name) WITH NOWAIT;
        SET @sql = N'ALTER TABLE ' + QUOTENAME(@table_schema_name) + N'.' + QUOTENAME(@table_name) +
            N' DROP CONSTRAINT ' + QUOTENAME(@unique_constraint_name) + N';';
        EXEC(@sql);
    END;

    IF @needs_column_alter = 1
    BEGIN
        DECLARE @createSql NVARCHAR(MAX);
        DECLARE @recreated_fk_count INT = 0;
        DECLARE create_fk_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT CreateSql FROM @foreignKeys ORDER BY ConstraintName;

        OPEN create_fk_cursor;
        FETCH NEXT FROM create_fk_cursor INTO @createSql;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC(@createSql);
            SET @recreated_fk_count = @recreated_fk_count + 1;
            FETCH NEXT FROM create_fk_cursor INTO @createSql;
        END;
        CLOSE create_fk_cursor;
        DEALLOCATE create_fk_cursor;

        RAISERROR(N'robormsp_ensurecolumn: column %s.%s finished, recreated FK=%d', 10, 1, @table_name, @column_name, @recreated_fk_count) WITH NOWAIT;
    END;
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE object_id = OBJECT_ID(N'[dbo].[robormsp_createfk]'))
    DROP PROCEDURE [dbo].[robormsp_createfk];
GO

CREATE PROCEDURE [dbo].[robormsp_createfk]
    @source_table NVARCHAR(128),
    @source_column NVARCHAR(128),
    @target_table NVARCHAR(128),
    @target_column NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    RAISERROR(N'robormsp_createfk: checking FK %s.%s -> %s.%s', 10, 1, @source_table, @source_column, @target_table, @target_column) WITH NOWAIT;

    DECLARE @source_table_object_id INT;
    DECLARE @source_schema_name SYSNAME;
    DECLARE @target_table_object_id INT;
    DECLARE @target_schema_name SYSNAME;
    DECLARE @source_column_id INT;
    DECLARE @target_column_id INT;
    DECLARE @constraint_name SYSNAME = N'FK_' + @source_table + N'_' + @source_column + N'__' + @target_table + N'_' + @target_column;
    DECLARE @existing_constraint_parent_schema SYSNAME;
    DECLARE @existing_constraint_parent_table SYSNAME;
    DECLARE @sql NVARCHAR(MAX);

    SELECT TOP 1
        @source_table_object_id = t.object_id,
        @source_schema_name = s.name
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.name = @source_table
      AND s.name = N'dbo';

    SELECT TOP 1
        @target_table_object_id = t.object_id,
        @target_schema_name = s.name
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.name = @target_table
      AND s.name = N'dbo';

    SELECT @source_column_id = c.column_id
    FROM sys.columns c
    WHERE c.object_id = @source_table_object_id
      AND c.name = @source_column;

    SELECT @target_column_id = c.column_id
    FROM sys.columns c
    WHERE c.object_id = @target_table_object_id
      AND c.name = @target_column;

    IF @source_table_object_id IS NULL
       OR @target_table_object_id IS NULL
       OR @source_column_id IS NULL
       OR @target_column_id IS NULL
    BEGIN
        RAISERROR(N'robormsp_createfk expected existing source and target tables and columns', 16, 1);
        RETURN;
    END;

    IF EXISTS(
        SELECT 1
        FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = @source_table_object_id
          AND fk.referenced_object_id = @target_table_object_id
          AND EXISTS(
                SELECT 1
                FROM sys.foreign_key_columns fkc
                WHERE fkc.constraint_object_id = fk.object_id
                GROUP BY fkc.constraint_object_id
                HAVING COUNT(*) = 1
                   AND MAX(CASE WHEN fkc.parent_column_id = @source_column_id AND fkc.referenced_column_id = @target_column_id THEN 1 ELSE 0 END) = 1))
    BEGIN
        RAISERROR(N'robormsp_createfk: FK %s.%s -> %s.%s already exists', 10, 1, @source_table, @source_column, @target_table, @target_column) WITH NOWAIT;
        RETURN;
    END;

    SELECT TOP 1
        @existing_constraint_parent_schema = s.name,
        @existing_constraint_parent_table = t.name
    FROM sys.foreign_keys fk
    JOIN sys.tables t ON t.object_id = fk.parent_object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE fk.name = @constraint_name;

    IF @existing_constraint_parent_table IS NOT NULL
    BEGIN
        RAISERROR(N'robormsp_createfk: dropping conflicting FK constraint %s', 10, 1, @constraint_name) WITH NOWAIT;
        SET @sql = N'ALTER TABLE ' + QUOTENAME(@existing_constraint_parent_schema) + N'.' + QUOTENAME(@existing_constraint_parent_table) +
            N' DROP CONSTRAINT ' + QUOTENAME(@constraint_name) + N';';
        EXEC(@sql);
    END;

    RAISERROR(N'robormsp_createfk: creating FK constraint %s', 10, 1, @constraint_name) WITH NOWAIT;
    SET @sql = N'ALTER TABLE ' + QUOTENAME(@source_schema_name) + N'.' + QUOTENAME(@source_table) +
        N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@constraint_name) +
        N' FOREIGN KEY (' + QUOTENAME(@source_column) + N') REFERENCES ' +
        QUOTENAME(@target_schema_name) + N'.' + QUOTENAME(@target_table) +
        N' (' + QUOTENAME(@target_column) + N'); ' +
        N'ALTER TABLE ' + QUOTENAME(@source_schema_name) + N'.' + QUOTENAME(@source_table) +
        N' CHECK CONSTRAINT ' + QUOTENAME(@constraint_name) + N';';
    EXEC(@sql);

    RAISERROR(N'robormsp_createfk: FK %s created', 10, 1, @constraint_name) WITH NOWAIT;
END
GO
