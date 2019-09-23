var app = app || {};
app.SaleEvents = app.SaleEvents || {};
app.SaleEvents.ViewModel = app.SaleEvents.ViewModel || function() {

    var self = this;

    var nextPage = 0;

    self.events = [];
    self.canLoadMore = false;

    var receiveEvent = function(e) {

        for (var i = 0; i < self.events.length; i++) {
            var existing = self.events[i];

            if (existing.Id === e.Id) {
                self.events[i] = e;
                return;
            }
        }

        self.events.push(e);
    };

    self.loadEvents = function() {
        lt.api("/saleEvents/getEvents").query({ "pageNumber": nextPage }).get(function(collection) {

            nextPage = collection.NextPageNumber || 0;
            self.canLoadMore = collection.HasNextPage;

            for (var i = 0; i < collection.Events.length; i++) {
                receiveEvent(collection.Events[i]);
            }
        });
    };

    self.uploadXls = function(file) {
        lt.api("/saleEvents/upload").formData(file.name, file).post(function(evt) {
            receiveEvent(evt);
        });
    }

    self.loadEvents(0);
};

app.SaleEvents.vm = app.SaleEvents.vm || new app.SaleEvents.ViewModel();