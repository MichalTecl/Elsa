var app = app || {};
app.batchesOverview = app.batchesOverview || {};
app.batchesOverview.ViewModel = app.batchesOverview.ViewModel || function() {

    var self = this;
    
    var copyProps = function(from, into) {
        for (var prop in from) {
            if (from.hasOwnProperty(prop)) {

                if (Array.isArray(from[prop]) && prop === "Orders") {

                    into.Orders = into.Orders || [];

                    for (var i = 0; i < from.Orders.length; i++) {
                        into.Orders.push(from.Orders[i]);
                    }

                    continue;
                }

                into[prop] = from[prop];
            }
        }
    };
    
    var receiveReport = function (report, session) {

        if (!!report.Query) {
            report.Query.PageNumber = (report.Query.PageNumber || 0) +1;
        }

        if (!report.IsUpdate) {
            session.query = report.Query;
            session.canLoadMore = report.CanLoadMore;
        }

        session.showCustomField1 = session.showCustomField1 || (report.CustomField1Name != null && report.CustomField1Name.length > 0);
        if (session.showCustomField1 && (report.CustomField1Name != null && report.CustomField1Name.length > 0)) {
            session.customField1Name = report.CustomField1Name;
        }

        session.showCustomField2 = session.showCustomField2 || (report.CustomField2Name != null && report.CustomField2Name.length > 0);
        if (session.showCustomField2 && (report.CustomField2Name != null && report.CustomField2Name.length > 0)) {
            session.customField2Name = report.CustomField2Name;
        }

        session.showCustomField3 = session.showCustomField3 || (report.CustomField3Name != null && report.CustomField3Name.length > 0);
        if (session.showCustomField3 && (report.CustomField3Name != null && report.CustomField3Name.length > 0)) {
            session.customField3Name = report.CustomField3Name;
        }
        for (var i = 0; i < report.Report.length; i++) {

            var toExtend = null;

            var receivedBatch = report.Report[i];

            if (receivedBatch.IsDeleted) {
                for (var x = session.list.length - 1; x >= 0; x--) {
                    if (session.list[x].BatchId === receivedBatch.BatchId) {
                        session.list.splice(x, 1);
                    }
                }

                return;
            }

            receivedBatch.stockEvents = [];
            var eventsSource = receivedBatch.StockEventCounts || {};
            
            for (var eventType in eventsSource) {
                if (eventsSource.hasOwnProperty(eventType)) {
                    receivedBatch.stockEvents.push({
                        "type": eventType,
                        "count": eventsSource[eventType],
                        "expanded": false,
                        "items": [],
                        "batchId": receivedBatch.BatchId
                    });
                }
            }

            var found = false;
            
            for (var j = 0; j < session.list.length; j++) {
                
                if (session.list[j].BatchId === receivedBatch.BatchId) {
                    found = true;
                    copyProps(receivedBatch, session.list[j]);
                    toExtend = session.list[j];
                    break;
                }
            }
            
            if (!found) {
                session.list.push(receivedBatch);
                toExtend = receivedBatch;
            }

            if (!!toExtend) {
                toExtend.itemKey = toExtend.BatchId.toString() + ":" + (toExtend.ParentId || "").toString();

                toExtend.hasComponents = toExtend.hasComponents || toExtend.NumberOfComponents > 0;
                toExtend.hasCompositions = toExtend.hasCompositions || toExtend.NumberOfCompositions > 0;
                toExtend.hasOrders = toExtend.hasOrders || toExtend.NumberOfOrders > 0;
                toExtend.hasSteps = toExtend.hasSteps || toExtend.NumberOfRequiredSteps > 0;
                toExtend.expanded = toExtend.expanded || false;

                toExtend.componentsExpanded = toExtend.componentsExpanded || false;
                toExtend.composExpanded = toExtend.composExpanded || false;
                toExtend.stepsExpanded = toExtend.stepsExpanded || false;
                toExtend.ordersExpanded = toExtend.ordersExpanded || false;

                toExtend.showCustomField1 = toExtend.showCustomField1 || session.showCustomField1 || false;
                toExtend.showCustomField2 = toExtend.showCustomField2 || session.showCustomField2 || false;
                toExtend.showCustomField3 = toExtend.showCustomField3 || session.showCustomField3 || false;
                
                toExtend.canExpand = toExtend.hasComponents ||
                    toExtend.hasCompositions ||
                    toExtend.hasOrders ||
                    toExtend.hasSteps ||
                    toExtend.stockEvents.length > 0;
            }
        }
    };

    self.load = function (session, callback, query) {
        
        lt.api("/batchReporting/get")
            .body(query || session.query)
            .post(function(report) {
                receiveReport(report, session);
                callback(session);
            });
    };

    self.deleteBatch = function(batchId, callback) {
        lt.api("/materialBatches/deleteBatch").query({ "id": batchId }).get(callback);
    };

    self.deleteProductionStep = function(stepId, callback) {
        lt.api("/production/deleteProductionStep").query({ "stepId": stepId }).get(callback);
    };

    self.loadSingleBatch = function (batchModel, query, callback) {
        query = query || {};
        callback = callback || function () { };

        query.BatchId = batchModel.BatchId;

        var fakeSession = { "list": [], "query": query };
        fakeSession.list.push(batchModel);

        self.load(fakeSession, callback);
    };

    self.loadStockEvents = function(model) {
        lt.api("/stockEvents/getBatchEvents")
            .query({ "batchId": model.batchId, "eventTypeName": model.type })
            .get(function(events) {
                model.items = events;
                model.expanded = true;
            });
    };

    self.deleteStockEvent = function(eventId, callback) {
        lt.api("/stockEvents/deleteStockEvent").query({ "eventId": eventId }).get(callback);
    };

    self.cutOrderAllocation = function(handle, callback) {
        lt.api("/materialBatches/cutOrderAllocation").query({ "handle": handle }).get(callback);
    };
};

app.batchesOverview.vm = app.batchesOverview.vm || new app.batchesOverview.ViewModel();