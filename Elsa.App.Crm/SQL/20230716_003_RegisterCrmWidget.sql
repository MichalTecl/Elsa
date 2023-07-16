INSERT INTO Appwidget (Name, IsAnonymous, ViewOrder, WidgetUrl)
SELECT 'CrmWidget', 0, 6, '/UI/CrmReporting/CrmWidget.html'
WHERE NOT EXISTS(SELECT TOP 1 1 FROM AppWidget WHERE Name = 'CrmWidget')