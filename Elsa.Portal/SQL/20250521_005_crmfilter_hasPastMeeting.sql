-- Drop if exists and create crmfilter_hasPastMeeting
IF OBJECT_ID('[crmfilter_hasPastMeeting]', 'P') IS NOT NULL
    DROP PROCEDURE [crmfilter_hasPastMeeting];
GO

CREATE PROCEDURE [crmfilter_hasPastMeeting](@strDays varchar(100), @filterText nvarchar(1000) OUTPUT)
AS
BEGIN
    /* Title:Byli kontaktováni posledních x dnů */
	/* Note:Vybere zákazníky, kteří byli v posledních X dnech kontaktováni */

	/* @strDays.control: /UI/DistributorsApp/FilterControls/NumberInput.html */
	/* @strDays.label: Počet dnů */

	SET @filterText = N'Kontaktováni v posledních ' + @strDays + ' dnech';

	DECLARE @daysInt INT = CONVERT(INT, @strDays);
		
	SELECT CustomerId
	  FROM Meeting m
	  JOIN MeetingStatus ms ON m.StatusId = ms.Id
	WHERE ISNULL(ms.MeansCancelled, 0) <> 1
	  AND m.StartDt >= DATEADD(day, -1 * @daysInt, GETDATE())
	  AND m.StartDt < GETDATE();
END
GO

-- Drop if exists and create crmfilter_hasPlannedMeeting
IF OBJECT_ID('[crmfilter_hasPlannedMeeting]', 'P') IS NOT NULL
    DROP PROCEDURE [crmfilter_hasPlannedMeeting];
GO

CREATE PROCEDURE [crmfilter_hasPlannedMeeting](@strDays varchar(100), @filterText nvarchar(1000) OUTPUT)
AS
BEGIN
    /* Title:Budou kontaktováni v příštích x dnech*/
	/* Note:Vybere zákazníky, kteří mají naplánovanou schůzku v příštích x dnech */

	/* @strDays.control: /UI/DistributorsApp/FilterControls/NumberInput.html */
	/* @strDays.label: Počet dnů */

	SET @filterText = N'Naplánován kontakt v ' + @strDays + ' dnech';

	DECLARE @daysInt INT = CONVERT(INT, @strDays);
		
	SELECT CustomerId
	  FROM Meeting m
	  JOIN MeetingStatus ms ON m.StatusId = ms.Id
	WHERE ISNULL(ms.MeansCancelled, 0) <> 1
	  AND m.StartDt <= DATEADD(day, @daysInt, GETDATE())
	  AND m.StartDt > GETDATE();
END
GO
