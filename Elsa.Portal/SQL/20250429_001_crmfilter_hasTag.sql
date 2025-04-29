IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'crmfilter_hasTag')
	DROP PROCEDURE crmfilter_hasTag;

GO

CREATE PROCEDURE crmfilter_hasTag(@tagNameLike varchar(100))
AS
BEGIN
    /* Title:Mají štítek */
	/* Note:Vybere zákazníky, kteří mají určitý štítek. Je možné použít hvězdičky '*' pro proměnnou část názvu štítku. */

	/* @tagNameLike.control: /UI/DistributorsApp/FilterControls/TextBox.html */
	/* @tagNameLike.label: Název štítku */
        
	-- todo escape "%" in original string
	SET @tagNameLike = REPLACE(@tagNameLike, '*', '%');

	SELECT c.Id
	  FROM Customer c
	  JOIN CustomerTagAssignment cta ON (cta.CustomerId = c.Id)
	  JOIN CustomerTagType tt ON (tt.Id = cta.TagTypeId)
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
	  AND tt.Name LIKE @tagNameLike;


END
GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'crmfilter_doesntHaveTag')
	DROP PROCEDURE crmfilter_doesntHaveTag;

GO

CREATE PROCEDURE crmfilter_doesntHaveTag(@tagNameLike varchar(100))
AS
BEGIN
    /* Title:Nemají štítek */
	/* Note:Vybere zákazníky, kteří nemají určitý štítek. Je možné použít hvězdičky '*' pro proměnnou část názvu štítku. */

	/* @tagNameLike.control: /UI/DistributorsApp/FilterControls/TextBox.html */
	/* @tagNameLike.label: Název štítku */

	-- todo escape "%" in original string
	SET @tagNameLike = REPLACE(@tagNameLike, '*', '%');

	SELECT c.Id
	  FROM Customer c	
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
	  AND NOT EXISTS(SELECT TOP 1 1 
	                   FROM CustomerTagAssignment cta 
					   JOIN CustomerTagType tt ON (tt.Id = cta.TagTypeId)
					   WHERE cta.CustomerId = c.Id
					     AND tt.Name like @tagNameLike);
END
GO
