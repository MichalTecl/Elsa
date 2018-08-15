var app = app || {};
app.orders = app.orders || {};
app.orders.ViewModel = app.orders.ViewModel || function() {

    var self = this;

    this.ordersOverview = null;

    var update = function() {

        lt.api("/commerceOverviews/GetOrdersOverview").get(function (orders) {
            self.ordersOverview = orders;
        });

    };

    update();

    setInterval(update, 30000);
};

app.orders.vm = app.orders.vm || new app.orders.ViewModel();