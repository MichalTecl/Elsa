IF OBJECT_ID('dbo.crmfilter_doesntHaveTag', 'P') IS NOT NULL
    DROP PROCEDURE dbo.crmfilter_doesntHaveTag;

IF OBJECT_ID('dbo.crmfilter_neverOrderedAProduct', 'P') IS NOT NULL
    DROP PROCEDURE dbo.crmfilter_neverOrderedAProduct;

IF OBJECT_ID('dbo.crmfilter_didntOrderProductInPeriod', 'P') IS NOT NULL
    DROP PROCEDURE dbo.crmfilter_didntOrderProductInPeriod;
