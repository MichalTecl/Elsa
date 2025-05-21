IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERE name='vwCustomerEvents')
	DROP VIEW vwCustomerEvents;

GO

CREATE VIEW vwCustomerEvents
AS
WITH cte
AS
(
SELECT cta.CustomerId,
		   cta.AssignDt   EventDt,
		   cta.AuthorId   AuthorId,
		   ctt.Name       Title,
		   cta.Note       Text,
		   ctt.CssClass   TitleCssClass,
		   null           IconCssClass
	   

	  FROM CustomerTagAssignment cta
	  JOIN CustomerTagType ctt ON (cta.TagTypeId = ctt.Id)
	UNION
	SELECT crn.CustomerId,
		   crn.CreateDt         EventDt,
		   crn.AuthorId         AuthorId,
		   'Poznámka'           Title,
		   crn.Body             Text,
		   null                 TitleCssClass,
		   'fas fa-comment-alt' IconCssClass	   
	  FROM CustomerRelatedNote crn
	UNION
	SELECT m.CustomerId,
		   m.StartDt   EventDt,
		   m.AuthorId,
		   m.Title,
		   m.Text,
		   'crmhi-meeting_status-' + LTRIM(STR(ms.Id)) TitleCssClass,
		   mc.IconClass IconCssClass
	  FROM Meeting m
	  JOIN MeetingCategory mc ON (m.MeetingCategoryId = mc.Id)
	  JOIN MeetingStatus   ms ON (m.StatusId = ms.Id)
	UNION
	SELECT c.Id         CustomerId,
	   po.PurchaseDate  EventDt,
       null             AuthorId,
	   po.OrderNumber + ' - ' + po.ErpStatusName Title,
	   orderItems.items  Text,
	   null              TitleCssClass,
	   'fas fa-shopping-basket' IconCssClass
  FROM PurchaseOrder po
  JOIN (SELECT i.PurchaseOrderId, STRING_AGG(i.PlacedName, '; ') items
          FROM OrderItem i
	     GROUP BY i.PurchaseOrderId) orderItems ON (orderItems.PurchaseOrderId = po.Id)
  JOIN Customer      c  ON (po.CustomerErpUid = c.ErpUid)
 WHERE c.IsCompany = 1 AND c.IsDistributor = 1
)

SELECT x.*,
       u.EMail Author,
	   CASE WHEN x.EventDt = lastEvent.DT THEN 1 ELSE 0 END IsLatestEvent,
	   CONVERT(varchar, x.EventDt, 104) EventDtF
  FROM cte x  
   JOIN [User] u ON (u.Id = x.AuthorId)
   JOIN (SELECT c.CustomerId, MAX(c.EventDt) Dt
           FROM cte c
		 GROUP BY c.CustomerId) lastEvent ON (x.CustomerId = lastEvent.CustomerId);




