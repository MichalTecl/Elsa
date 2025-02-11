IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'LoadAllDistributors')
	DROP PROCEDURE LoadAllDistributors;

GO

CREATE PROCEDURE LoadAllDistributors(@projectId INT, @userId INT)
AS
BEGIN

	-- select * from Customer c where c.iscompany = 1 and c.isdistributor = 1

	DECLARE @previousThreeMonthsStart DATETIME = DATEADD(month, -3, GETDATE());
	DECLARE @previousSixMonthsStart DATETIME = DATEADD(month, -6, GETDATE());



		SELECT c.Id, 
			   ISNULL(c.Name, c.Email) Name,
			   c.Email,
               c.SearchTag,
			   ISNULL(tags.TagTypesCsv, '') TagTypesCsv,
			   ISNULL(customerGroups.CustomerGroupTypesCsv, '') CustomerGroupTypesCsv,
			   passedMeeting.meetingDt LastContactDt,
			   futureMeeting.meetingDt FutureContactDt,		   
			   ISNULL(allOrders.TotalOrdersCount, 0) TotalOrdersCount,
			   ISNULL(allOrders.TotalOrdersTaxedPrice, 0) TotalOrdersTaxedPrice,
			   ISNULL(prevQuarterOrders.TotalOrdersTaxedPrice, 0) PrevQuarterOrdersPrice,
			   ISNULL(lastQuarterOrders.TotalOrdersTaxedPrice, 0) LastQuarterOrdersPrice,
			   ISNULL((ISNULL(lastQuarterOrders.TotalOrdersTaxedPrice, 0) - ISNULL(prevQuarterOrders.TotalOrdersTaxedPrice, 0)) /  
						NULLIF(
							CASE 
								WHEN ISNULL(lastQuarterOrders.TotalOrdersTaxedPrice, 0) >= ISNULL(prevQuarterOrders.TotalOrdersTaxedPrice, 0) 
								THEN ISNULL(lastQuarterOrders.TotalOrdersTaxedPrice, 0) 
								ELSE ISNULL(prevQuarterOrders.TotalOrdersTaxedPrice, 0) 
							END, 
							0
						) * 100, 0)  CrossQuarterTrend

		  FROM Customer c

		  LEFT JOIN (SELECT mtg.CustomerId, MAX(mtg.StartDt) meetingDt
					   FROM Meeting mtg 
					   JOIN MeetingStatusType ms ON (ms.Id = mtg.MeetingStatusId)
					   WHERE ms.ActionExpected = 0
					  GROUP BY mtg.CustomerId) passedMeeting ON (passedMeeting.CustomerId = c.Id)

		  LEFT JOIN (SELECT mtg.CustomerId, MIN(mtg.StartDt) meetingDt
					   FROM Meeting mtg 
					   JOIN MeetingStatusType ms ON (ms.Id = mtg.MeetingStatusId)
					   WHERE ms.ActionExpected = 1
					  GROUP BY mtg.CustomerId) futureMeeting ON (futureMeeting.CustomerId = c.Id)

		   LEFT JOIN (SELECT po.CustomerErpUid, 
						   SUM(po.PriceWithVat) TotalOrdersTaxedPrice, 
						   COUNT(po.Id) TotalOrdersCount
					  FROM PurchaseOrder po
					 WHERE po.OrderStatusId = 5
					GROUP BY po.CustomerErpUid) allOrders ON (allOrders.CustomerErpUid = c.ErpUid)

			LEFT JOIN (SELECT po.CustomerErpUid, 
						   SUM(po.PriceWithVat) TotalOrdersTaxedPrice
					  FROM PurchaseOrder po
					 WHERE po.OrderStatusId = 5
					   AND po.BuyDate BETWEEN @previousThreeMonthsStart AND GETDATE()
					GROUP BY po.CustomerErpUid) lastQuarterOrders ON (lastQuarterOrders.CustomerErpUid = c.ErpUid)

			LEFT JOIN (SELECT po.CustomerErpUid, 
						   SUM(po.PriceWithVat) TotalOrdersTaxedPrice
					  FROM PurchaseOrder po
					 WHERE po.OrderStatusId = 5
					   AND po.BuyDate BETWEEN @previousSixMonthsStart AND @previousThreeMonthsStart
					GROUP BY po.CustomerErpUid) prevQuarterOrders ON (prevQuarterOrders.CustomerErpUid = c.ErpUid)

			LEFT JOIN (SELECT 
						cta.CustomerId,
						STRING_AGG(cta.TagTypeId, ',') AS TagTypesCsv
					FROM CustomerTagAssignment cta
					JOIN CustomerTagType ctt ON (cta.TagTypeId = ctt.Id)
					WHERE ((ctt.ForAuthorOnly = 0) OR (ctt.AuthorId = @userId))
					GROUP BY cta.CustomerId) tags ON (tags.CustomerId = c.Id)

           LEFT JOIN(SELECT cg.CustomerId, STRING_AGG(cgt.Id, ',') CustomerGroupTypesCsv
					  FROM CustomerGroup cg
					  JOIN CustomerGroupType cgt ON (cgt.ErpGroupName = cg.ErpGroupName)
                     WHERE cgt.ProjectId = @projectId
					GROUP BY cg.CustomerId) customerGroups ON (customerGroups.CustomerId = c.Id)

		 WHERE c.ProjectId = @projectId
		   AND c.IsCompany = 1
		   AND c.IsDistributor = 1
	   
END
