
CREATE OR ALTER PROCEDURE [crmfilter_hasTimeoutedTag](@tagNameLike varchar(100))
AS
BEGIN
    /* Title:Mají štítek po timeoutu */
	/* Note:Vybere zákazníky, kteří mají štítek po nastaveném timeoutu. Je třeba vybrat typ štítku, nebo vložit "*" pro jakýkoliv typ */

	/* @tagNameLike.control: /UI/DistributorsApp/FilterControls/CustomerTagInput.html */
	/* @tagNameLike.label: Název štítku */

	-- todo escape "%" in original string

	IF (@tagNameLike IS NULL OR LEN(TRIM(@tagNameLike)) = 0)
		SET @tagNameLike = '*';

	SET @tagNameLike = REPLACE(@tagNameLike, '*', '%');		

	SELECT c.Id
	  FROM Customer c
	  JOIN CustomerTagAssignment cta ON (cta.CustomerId = c.Id)
	  JOIN CustomerTagType tt ON (tt.Id = cta.TagTypeId)
	WHERE c.IsCompany = 1
      AND c.IsDistributor = 1
      AND (cta.UnassignDt IS NULL OR cta.UnassignDt > GETDATE())
	  AND ISNULL(tt.DaysToWarning, 0) > 0	  
	  AND DATEADD(DAY, tt.DaysToWarning, cta.AssignDt) < GETDATE()
	  AND tt.Name LIKE @tagNameLike;

END
GO


