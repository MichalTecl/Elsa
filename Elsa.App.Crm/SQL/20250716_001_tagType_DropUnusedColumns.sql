-- Drop ForAuthorOnly pokud existuje
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CustomerTagType'
      AND COLUMN_NAME = 'ForAuthorOnly'
)
BEGIN
    ALTER TABLE CustomerTagType
    DROP COLUMN ForAuthorOnly;
END

-- Drop Priority pokud existuje
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CustomerTagType'
      AND COLUMN_NAME = 'Priority'
)
BEGIN
    ALTER TABLE CustomerTagType
    DROP COLUMN Priority;
END

-- Drop OptionGroup pokud existuje
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CustomerTagType'
      AND COLUMN_NAME = 'OptionGroup'
)
BEGIN
    ALTER TABLE CustomerTagType
    DROP COLUMN OptionGroup;
END

-- Drop CanBeAssignedManually pokud existuje
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CustomerTagType'
      AND COLUMN_NAME = 'CanBeAssignedManually'
)
BEGIN
    ALTER TABLE CustomerTagType
    DROP COLUMN CanBeAssignedManually;
END
