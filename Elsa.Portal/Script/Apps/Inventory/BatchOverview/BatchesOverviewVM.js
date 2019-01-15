var app = app || {};
app.batchesOverview = app.batchesOverview || {};
app.batchesOverview.ViewModel = app.batchesOverview.ViewModel || function() {

    var self = this;

    const defaultQuery = { PageNumber: 0 };

    self.batches = [];

    self.currentQuery = {};

    self.canLoadMore = true;

    self.clear = function() {
        self.batches = [];
        self.currentQuery = JSON.parse(JSON.stringify(defaultQuery));
        //self.canLoadMore = false;

        lt.notify();
    };

    var copyProps = function(from, into) {
        for (var prop in from) {
            if (from.hasOwnProperty(prop)) {
                into[prop] = from[prop];
            }
        }
    };

    var receiveReport = function (report) {

        report.Query.PageNumber++;

        if (!report.IsUpdate) {
            self.currentQuery = report.Query;
            self.canLoadMore = report.CanLoadMore;
        }

        for (var i = 0; i < report.Report.length; i++) {
            var receivedBatch = report.Report[i];

            var found = false;
            for (var j = 0; j < self.batches.length; j++) {
                if (self.batches[j].BatchId === receivedBatch.BatchId) {
                    found = true;
                    copyProps(receivedBatch, self.batches[i]);
                    break;
                }
            }

            if (!found) {
                self.batches.push(receivedBatch);
            }
        }
    };

    self.load = function() {

        lt.api("/batchReporting/get").body(self.currentQuery).post(receiveReport);
    };

    self.clear();
};

app.batchesOverview.vm = app.batchesOverview.vm || new app.batchesOverview.ViewModel();