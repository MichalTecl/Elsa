IF OBJECT_ID('vwCustomerGroupsCsv', 'V') IS NOT NULL
    DROP VIEW vwCustomerGroupsCsv;
GO

-- Create the view
CREATE VIEW vwCustomerGroupsCsv AS
    SELECT x.CustomerId, STRING_AGG(x.ReportingName, ', ') AS Groups
    FROM 
    (
        SELECT DISTINCT cg.CustomerId, cgm.ReportingName
        FROM CustomerGroup cg
        JOIN CustomerGroupMapping cgm ON cg.ErpGroupName = cgm.GroupErpName
    ) x
    GROUP BY x.CustomerId;
