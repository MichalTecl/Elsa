INSERT INTO AppWidget (Name, IsAnonymous, ViewOrder, WidgetUrl)
SELECT 'ReportingWidget', 0, 4, '/UI/Widgets/Reporting/ReportingWidget.html'
 WHERE NOT EXISTS(SELECT TOP 1 1 FROM AppWidget WHERE Name = 'ReportingWidget');

INSERT INTO AppWidget (Name, IsAnonymous, ViewOrder, WidgetUrl)
SELECT 'SystemWidget', 0, 5, '/UI/Widgets/Sys/SystemWidget.html'
 WHERE NOT EXISTS(SELECT TOP 1 1 FROM AppWidget WHERE Name = 'SystemWidget');


