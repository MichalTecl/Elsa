ALTER TABLE MaterialBatch ADD CalculatedKey AS BatchNumber + ':' + CAST(MaterialId AS NVARCHAR) PERSISTED


