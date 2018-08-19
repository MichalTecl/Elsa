var app = app || {};
app.orders = app.orders || {};
app.orders.ViewModel = app.orders.ViewModel || function() {

    var self = this;

    this.ordersOverview = null;
    this.missingPaymentsOverview = null;

    var update = function() {
        lt.api("/commerceOverviews/GetOrdersOverview").get(function (orders) {
            self.ordersOverview = orders;
        });

        lt.api("/commerceOverviews/GetMissingPaymentsCount").get(function(missingPayment) {
                self.missingPaymentsOverview = missingPayment;
        });
    };

    update();

    setInterval(update, 30000);
};

app.orders.vm = app.orders.vm || new app.orders.ViewModel();