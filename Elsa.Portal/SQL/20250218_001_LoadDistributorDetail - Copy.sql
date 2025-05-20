IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'LoadDistributorDetail')
	DROP PROCEDURE LoadDistributorDetail;

GO

CREATE PROCEDURE LoadDistributorDetail (@projectId INT,  @customerId INT)
AS
BEGIN
	SELECT 
		c.Id,
		c.Name,
		c.Email,
		c.Phone,
		c.HasEshop,
		c.HasStore,
		c.VatId,
		c.CompanyRegistrationId RegistrationId,
		srep.SalesRepIdsCsv,
		tags.TagTypesCsv,
		customerGroups.CustomerGroupTypesCsv,
		ISNULL(customerNotes.NotesCount, 0) NotesCount
	FROM 
		Customer c
	
		LEFT JOIN (SELECT cta.CustomerId,
			STRING_AGG(cta.TagTypeId, ',') AS TagTypesCsv
		FROM CustomerTagAssignment cta
		JOIN CustomerTagType ctt ON (cta.TagTypeId = ctt.Id)
		GROUP BY cta.CustomerId) tags ON (tags.CustomerId = c.Id)

		LEFT JOIN(SELECT cg.CustomerId, STRING_AGG(cgt.Id, ',') CustomerGroupTypesCsv
		  FROM CustomerGroup cg
		  JOIN CustomerGroupType cgt ON (cgt.ErpGroupName = cg.ErpGroupName)
				  WHERE cgt.ProjectId = @projectId
		GROUP BY cg.CustomerId) customerGroups ON (customerGroups.CustomerId = c.Id)

		LEFT JOIN (SELECT crn.CustomerId, COUNT(crn.Id) NotesCount
					 FROM CustomerRelatedNote crn
					GROUP BY crn.CustomerId) customerNotes ON (customerNotes.CustomerId = c.Id)

		LEFT JOIN (SELECT src.CustomerId, STRING_AGG(src.SalesRepId, ',') SalesRepIdsCsv
				 FROM SalesRepCustomer src
				 WHERE (src.ValidFrom < GETDATE())
				 AND ((src.ValidTo IS NULL) OR (src.ValidTo > GETDATE()))
				  GROUP BY src.CustomerId) srep ON (srep.CustomerId = c.Id)

	

	WHERE 1=1
	  AND c.ProjectId = @projectId
	  AND c.Id = @customerId
	
END
