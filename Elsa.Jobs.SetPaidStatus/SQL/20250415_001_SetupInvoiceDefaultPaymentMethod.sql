UPDATE CustomerGroupType
   SET DefaultPaymentMethod = N'Faktura - 14denní splatnost'
 WHERE DefaultPaymentMethod IS NULL
   AND ErpGroupName = N'Platba na fakturu'

