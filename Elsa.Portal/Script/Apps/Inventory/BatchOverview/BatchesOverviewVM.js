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

        for (var i = 0; i < report.Report.length; i++) {

            var toExtend = null;

            var receivedBatch = report.Report[i];
            
            var found = false;
            
            for (var j = 0; j < session.list.length; j++) {
                
                if (session.list[j].BatchId === receivedBatch.BatchId) {
                    found = true;
                    copyProps(receivedBatch, session.list[i]);
                    toExtend = session.list[i];
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
                
                toExtend.canExpand = toExtend.hasComponents ||
                    toExtend.hasCompositions ||
                    toExtend.hasOrders ||
                    toExtend.hasSteps;

            }
        }
    };

    self.load = function (session, callback) {
        
        lt.api("/batchReporting/get")
            .body(session.query)
            .post(function(report) {
                receiveReport(report, session);
                callback(session);
            });
    };

    self.loadSingleBatch = function(batchModel, query) {
        query.BatchId = batchModel.BatchId;

        var fakeSession = { "list": [], "query": query };
        fakeSession.list.push(batchModel);

        self.load(fakeSession, function() {});
    };
};

app.batchesOverview.vm = app.batchesOverview.vm || new app.batchesOverview.ViewModel();