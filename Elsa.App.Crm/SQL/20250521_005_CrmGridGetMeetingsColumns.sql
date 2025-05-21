IF OBJECT_ID('CrmGridGetMeetingsColumns', 'P') IS NOT NULL
    DROP PROCEDURE CrmGridGetMeetingsColumns;
GO

CREATE PROCEDURE CrmGridGetMeetingsColumns
AS
BEGIN
    WITH activeMeeting AS (
        SELECT m.CustomerId, 
               m.Id AS MeetingId, 
               m.StartDt AS Dt, 
               m.Title + ': ' + m.Text AS Text, 
               mc.IconClass,
               FORMAT(m.StartDt, 'dd.MM.yyyy') AS DtF 
          FROM Meeting m
          JOIN MeetingStatus ms ON (m.StatusId = ms.Id)
          JOIN MeetingCategory mc ON (m.MeetingCategoryId = mc.Id)
          WHERE ISNULL(ms.MeansCancelled, 0) <> 1
    )
    SELECT custid.CustomerId,
           futureMeeting.IconClass AS FutureMeetingIcon,
           futureMeeting.DtF AS FutureMeetingDtF,
           futureMeeting.Text AS FutureMeetingText,
           pastMeeting.IconClass AS PastMeetingIcon,
           pastMeeting.DtF AS PastMeetingDtF,
           pastMeeting.Text AS PastMeetingText,
           futureMeeting.Dt AS FutureMeetingDt,
           pastMeeting.Dt AS PastMeetingDt
      FROM (SELECT DISTINCT m.CustomerId FROM Meeting m) custid
      LEFT JOIN (
          SELECT m.CustomerId, MAX(m.Dt) AS Dt
            FROM activeMeeting m
           WHERE m.Dt < GETDATE()			    
           GROUP BY m.CustomerId
      ) pastMeetingTime ON custid.CustomerId = pastMeetingTime.CustomerId
      LEFT JOIN (
          SELECT m.CustomerId, MIN(m.Dt) AS Dt
            FROM activeMeeting m
           WHERE m.Dt > GETDATE()
           GROUP BY m.CustomerId
      ) futureMeetingTime ON custid.CustomerId = futureMeetingTime.CustomerId
      LEFT JOIN activeMeeting pastMeeting 
             ON pastMeeting.CustomerId = custid.CustomerId AND pastMeeting.Dt = pastMeetingTime.Dt
      LEFT JOIN activeMeeting futureMeeting 
             ON futureMeeting.CustomerId = custid.CustomerId AND futureMeeting.Dt = futureMeetingTime.Dt;
END
GO
