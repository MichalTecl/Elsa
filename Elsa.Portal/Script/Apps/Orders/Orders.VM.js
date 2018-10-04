var app = app || {};
app.orders = app.orders || {};
app.orders.ViewModel = app.orders.ViewModel || function() {

    var self = this;

    this.ordersOverview = null;
    this.missingPaymentsOverview = null;
    this.readyToPackCount = null;

    var update = function() {
        lt.api("/commerceOverviews/GetOrdersOverview").get(function (orders) {
            self.ordersOverview = orders;
        });

        lt.api("/commerceOverviews/GetMissingPaymentsCount").get(function(missingPayment) {
            self.missingPaymentsOverview = missingPayment;
        });

        lt.api("/commerceOverviews/GetReadyToPackCount").get(function(readyToPack) {
            self.readyToPackCount = readyToPack;
        });
    };

    update();

    setInterval(update, 30000);
};

app.orders.vm = app.orders.vm || new app.orders.ViewModel();