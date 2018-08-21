var app = app || {};
app.paymentPairing = app.paymentPairing || {};
app.paymentPairing.ViewModel = app.paymentPairing.ViewModel || function() {

    var self = this;

    this.suggestedPairs = null;

    var receive = function(pairs) {
        self.suggestedPairs = pairs;
    };

    this.update = function() {
        lt.api("/paymentPairing/getUnpaidOrders").useCache().get(receive);
    };

    self.update();
};

app.paymentPairing.vm = app.paymentPairing.vm || new app.paymentPairing.ViewModel();