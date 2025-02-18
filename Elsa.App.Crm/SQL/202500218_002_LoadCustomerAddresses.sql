IF EXISTS(SELECT TOP  1 1 FROM sys.procedures WHERE name = 'LoadCustomerAddresses')
	DROP PROCEDURE LoadCustomerAddresses;

GO

CREATE PROCEDURE LoadCustomerAddresses (@customerId INT)
AS
BEGIN
	SELECT *
	FROM
	(
		-- Sídlo klienta
		SELECT 
			1 AS Sorter,
			N'Sídlo klienta' AS AddressName,
			c.Street,
			c.DescriptiveNumber,
			c.OrientationNumber,
			c.City,
			c.Zip,
			c.Email,
			c.Phone,
			0 AS IsStore,
			NULL AS StoreName,
			NULL AS Lat,
			NULL AS Lon,
			NULL AS Www
		FROM Customer c
		WHERE c.Id = @customerId

		UNION ALL

		-- Poslední doručovací adresa
		SELECT 
			2 AS Sorter,
			N'Poslední doručovací adresa' AS AddressName,
			a.Street,
			a.DescriptiveNumber,
			a.OrientationNumber,
			c.City,
			c.Zip,
			c.Email,
			c.Phone,
			0 AS IsStore,
			NULL AS StoreName,
			NULL AS Lat,
			NULL AS Lon,
			NULL AS Www
		FROM Customer c	
		JOIN 
		(
			SELECT o.CustomerErpUid, MAX(o.Id) AS Id 
			FROM PurchaseOrder o 
			GROUP BY o.CustomerErpUid
		) lastOrder ON lastOrder.CustomerErpUid = c.ErpUid
		JOIN PurchaseOrder po ON po.Id = lastOrder.Id
		JOIN [Address] a ON a.Id = ISNULL(po.DeliveryAddressId, po.InvoiceAddressId)
		WHERE c.Id = @customerId

		UNION ALL

		-- Obchody
		SELECT 
			2 + s.ID AS Sorter,
			s.SystemRecordName AS AddressName,
			s.Address,
			NULL AS DescriptiveNumber,
			NULL AS OrientationNumber,
			s.City,
			NULL AS Zip,
			NULL AS Email,
			NULL AS Phone,
			1 AS IsStore,
			s.Name AS StoreName,
			s.Lat,
			s.Lon,
			s.Www
		FROM CustomerStore s
	   WHERE s.CustomerId = @customerId
	) x
	ORDER BY x.Sorter;
END