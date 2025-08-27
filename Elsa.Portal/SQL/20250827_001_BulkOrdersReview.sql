CREATE OR ALTER PROCEDURE BulkReviewSentOrders (@authorId INT)
AS
BEGIN
	INSERT INTO OrderReviewResult (OrderId, ReviewDt, Note, AuthorId)
	SELECT po.Id, GETDATE(), N'Hromadné označení', @authorId
	  FROM PurchaseOrder po
	 WHERE DATEDIFF(day, po.PurchaseDate, GETDATE()) < 10
	   AND po.OrderStatusId = 5
	   AND NOT EXISTS(SELECT TOP 1 1 FROM OrderReviewResult orr WHERE orr.OrderId = po.Id);
       
    SELECT CAST(@@ROWCOUNT AS INT) AS InsertedCount;
END