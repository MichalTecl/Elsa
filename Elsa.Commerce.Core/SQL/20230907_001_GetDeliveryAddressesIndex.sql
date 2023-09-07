IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'GetDeliveryAddressesIndex')
	DROP PROCEDURE GetDeliveryAddressesIndex;

GO

CREATE PROCEDURE GetDeliveryAddressesIndex(@projectId INT)
AS
BEGIN
	select c.Id CustomerId, dadr.*
  from Customer c
  join (select po.CustomerErpUid, MAX(po.DeliveryAddressId) addressID
          from PurchaseOrder po
         group by po.CustomerErpUid) abridge ON (c.ErpUid = abridge.CustomerErpUid)
  join [Address] dadr on (abridge.addressID = dadr.Id) 
  where c.IsDistributor = 1
    and c.ProjectId = @projectId;	
END