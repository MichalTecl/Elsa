IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'autoq_stockEventsWeeklyOverview')
	DROP PROCEDURE autoq_stockEventsWeeklyOverview;

GO

CREATE PROCEDURE autoq_stockEventsWeeklyOverview (@projectId INT, @weekNum INT)
AS
BEGIN
	SELECT et.Name Typ, LTRIM(STR(mse.Delta)) + ' ' + mu.Symbol Množství, m.Name Materiál, mb.BatchNumber Šarže, mse.EventDt Dt, u.EMail Autor, mse.Note Poznámka
	  FROM MaterialStockEvent mse
	  JOIN StockEventType et ON (mse.TypeId = et.Id)
	  JOIN MaterialBatch mb ON (mse.BatchId = mb.Id)
	  JOIN MaterialUnit mu ON (mse.UnitId = mu.Id)
	  JOIN Material m ON (mb.MaterialId = m.Id)
	  JOIN [User] u ON (mse.UserId = u.Id)
	  WHERE m.ProjectId = @projectId 
	    AND mse.EventDt >= DATEADD(WEEK, -1, GETDATE())  -- Za poslední týden
	ORDER BY et.Name, mse.EventDt
END

GO

INSERT INTO AutomaticQuery (TitlePattern, ProcedureName, MailRecipientGroup, ResultToAttachment, ProjectId)
SELECT N'Odpady a propagace za minulý týden', N'autoq_stockEventsWeeklyOverview', N'Autom. dotazy', 1, 1
 WHERE NOT EXISTS(SELECT TOP 1 1 FROM AutomaticQuery WHERE TitlePattern = N'Odpady a propagace za minulý týden');

DECLARE @aqId INT = (SELECT TOP 1 Id FROM AutomaticQuery WHERE TitlePattern = N'Odpady a propagace za minulý týden');

INSERT INTO AutoQueryParameter (QueryId, ParameterName, Expression, TriggerOnly)
SELECT @aqId, '@projectId', 'GET_PROJECT_ID', 0
 WHERE NOT EXISTS (SELECT TOP 1 1 FROM AutoQueryParameter WHERE QueryId = @aqId AND ParameterName = '@projectId');

INSERT INTO AutoQueryParameter (QueryId, ParameterName, Expression, TriggerOnly)
SELECT @aqId, '@weekNum', 'GET_WEEK_NUM', 0
 WHERE NOT EXISTS (SELECT TOP 1 1 FROM AutoQueryParameter WHERE QueryId = @aqId AND ParameterName = '@weekNum');

GO
