IF OBJECT_ID('xrep_b2c_orders_mail_price') IS NOT NULL
BEGIN
	DROP PROCEDURE xrep_b2c_orders_mail_price;
END

GO

CREATE PROCEDURE xrep_b2c_orders_mail_price (@projectId INT)
AS
BEGIN
/*Title: Maloobchodní objednávky s cenou a e-mailem*/

/*Note:
Seznam všech vyřízených maloobchodních objednávek od začátku minulého roku, pro každou objednávku řádek s e-mailem zákazníka, datem objednání a cenou bez dph. Cena obsahuje dopravu.
*/

	DECLARE @ords TABLE (orderId BIGINT, customerId INT);

	INSERT INTO @ords
	SELECT po.Id OrderId, oc.CustomerId CustomerId
	  FROM PurchaseOrder po -- 15573
	  JOIN vwOrderCustomer oc ON (oc.OrderId = po.Id)  
	 WHERE YEAR(po.PurchaseDate) >= (YEAR(GETDATE()) - 1)  
	   AND po.OrderStatusId = 5
	   ANd po.ProjectId = @projectId;
   
	SELECT po.CustomerEmail "Email", po.PriceWithVat / 1.21 "Cena bez DPH", po.PurchaseDate "Datum"   
	  FROM @ords o
	  JOIN PurchaseOrder po ON (o.orderId = po.Id)
	  JOIN Customer c ON (o.customerId = c.Id)
	 WHERE c.IsDistributor = 0
	ORDER BY po.PurchaseDate;

END