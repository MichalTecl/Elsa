CREATE OR ALTER PROCEDURE PopulateContactPersons
AS
BEGIN

    MERGE INTO dbo.Person AS target
    USING (
        SELECT 
            c.ErpUid AS ExternalId,
            c.Email,
            c.Name,
            c.Phone
        FROM Customer c
        WHERE c.ErpUid IS NOT NULL
          AND c.IsCompany = 0
    ) AS source
    ON target.ExternalId = source.ExternalId

    -- Pokud záznam existuje, proveď UPDATE
    WHEN MATCHED THEN
        UPDATE SET 
            target.Email = source.Email,
            target.Name = source.Name,
            target.Phone = source.Phone

    -- Pokud záznam neexistuje, proveď INSERT
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (ExternalId, Email, Name, Phone)
        VALUES (source.ExternalId, source.Email, source.Name, source.Phone);

    INSERT INTO CustomerContactPerson (CustomerId, PersonId)
    SELECT company.Id, p.Id
      FROM Customer company
      JOIN Person p ON (company.MainUserEmail = p.Email)
     WHERE company.MainUserEmail IS NOT NULL
       ANd NOT EXISTS(SELECT TOP 1 1 
                        FROM CustomerContactPerson ccp
                       WHERE ccp.CustomerId = company.Id
                         AND ccp.PersonId = p.Id)
END