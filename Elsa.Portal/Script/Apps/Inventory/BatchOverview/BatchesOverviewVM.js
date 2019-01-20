var app = app || {};
app.batchesOverview = app.batchesOverview || {};
app.batchesOverview.ViewModel = app.batchesOverview.ViewModel || function() {

    var self = this;
    
    var copyProps = function(from, into) {
        for (var prop in from) {
            if (from.hasOwnProperty(prop)) {
                into[prop] = from[prop];
            }
        }
    };
    
    var receiveReport = function (report, session) {

        if (!!report.Query && !!report.Query.PageNumber) {
            report.Query.PageNumber++;
        }

        if (!report.IsUpdate) {
            session.query = report.Query;
            session.canLoadMore = report.CanLoadMore;
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