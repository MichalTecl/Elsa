CREATE OR ALTER PROCEDURE [dbo].[LoadAllDistributors](@projectId INT, @userId INT)
AS
BEGIN

	SELECT c.Id, 
	   ISNULL(c.Name, c.Email) Name,
	   c.Email,
       c.SearchTag,
	   ISNULL(tags.TagTypesCsv, '') TagTypesCsv,
	   ISNULL(customerGroups.CustomerGroupTypesCsv, '') CustomerGroupTypesCsv,
	   ISNULL(srep.SalesRepIdsCsv, '') SalesRepIdsCsv,
	   passedMeeting.meetingDt LastContactDt,
	   futureMeeting.meetingDt FutureContactDt,		   
	   ISNULL(allOrders.TotalOrdersCount, 0) TotalOrdersCount,
	   ISNULL(allOrders.TotalOrdersUntaxedPrice, 0) TotalOrdersUntaxedPrice	   

  FROM Customer c

  LEFT JOIN (SELECT mtg.CustomerId, MAX(mtg.StartDt) meetingDt
			   FROM Meeting mtg 
			   JOIN MeetingStatus ms ON (ms.Id = mtg.StatusId)
			   WHERE ms.ActionExpected = 0
			  GROUP BY mtg.CustomerId) passedMeeting ON (passedMeeting.CustomerId = c.Id)

  LEFT JOIN (SELECT mtg.CustomerId, MIN(mtg.StartDt) meetingDt
			   FROM Meeting mtg 
			   JOIN MeetingStatus ms ON (ms.Id = mtg.StatusId)
			   WHERE ms.ActionExpected = 1
			  GROUP BY mtg.CustomerId) futureMeeting ON (futureMeeting.CustomerId = c.Id)

   LEFT JOIN (SELECT po.CustomerErpUid, 
				   SUM(po.Price) TotalOrdersUntaxedPrice, 
				   COUNT(po.Id) TotalOrdersCount
			  FROM PurchaseOrder po
			 WHERE po.OrderStatusId = 5
			GROUP BY po.CustomerErpUid) allOrders ON (allOrders.CustomerErpUid = c.ErpUid)
				
	LEFT JOIN (SELECT 
				cta.CustomerId,
				STRING_AGG(cta.TagTypeId, ',' 
            ) WITHIN GROUP (ORDER BY ISNULL(tg.DisplayOrder, tg.Id)) AS TagTypesCsv
			FROM CustomerTagAssignment cta
			JOIN CustomerTagType ctt ON (cta.TagTypeId = ctt.Id)	
			JOIN CustomerTagTypeGroup tg ON (ctt.GroupId = tg.Id)
            WHERE (cta.UnassignDt IS NULL OR cta.UnassignDt > GETDATE())
			GROUP BY cta.CustomerId			
			) tags ON (tags.CustomerId = c.Id)

   LEFT JOIN(SELECT cg.CustomerId, STRING_AGG(cgt.Id, ',') CustomerGroupTypesCsv
			  FROM CustomerGroup cg
			  JOIN CustomerGroupType cgt ON (cgt.ErpGroupName = cg.ErpGroupName)             
			GROUP BY cg.CustomerId) customerGroups ON (customerGroups.CustomerId = c.Id)

   LEFT JOIN (SELECT src.CustomerId, STRING_AGG(src.SalesRepId, ',') SalesRepIdsCsv
                FROM SalesRepCustomer src
			   WHERE (src.ValidFrom < GETDATE())
			     AND ((src.ValidTo IS NULL) OR (src.ValidTo > GETDATE()))
			   GROUP BY src.CustomerId) srep ON (srep.CustomerId = c.Id)

 WHERE c.IsCompany = 1
   AND c.IsDistributor = 1;
	   
	   
END
GO


