var app = app || {};
app.orders = app.orders || {};
app.orders.ViewModel = app.orders.ViewModel || function() {

    var self = this;

    this.ordersOverview = null;
    this.missingPaymentsOverview = null;
    this.readyToPackCount = null;
    this.failedOrderImportsCount = 0;
    this.hasFailedOrderImports = false;
    this.failedOrderImportsText = "Nezdařené importy (0)";

    var update = function() {
        lt.api("/commerceOverviews/GetOrdersOverview").silent().get(function (orders) {
            self.ordersOverview = orders;
        });

        lt.api("/commerceOverviews/GetMissingPaymentsCount").silent().get(function (missingPayment) {
            self.missingPaymentsOverview = missingPayment;
        });

        lt.api("/commerceOverviews/GetReadyToPackCount").silent().get(function (readyToPack) {
            self.readyToPackCount = readyToPack;
        });

        lt.api("/ordersInfo/GetOrderImportFailuresCount").silent().get(function (count) {
            self.failedOrderImportsCount = Number(count || 0);
            self.hasFailedOrderImports = self.failedOrderImportsCount > 0;
            self.failedOrderImportsText = "Nezdařené importy (" + self.failedOrderImportsCount + ")";
        });
    };

    update();

    setInterval(update, 30000);
};

app.orders.vm = app.orders.vm || new app.orders.ViewModel();
