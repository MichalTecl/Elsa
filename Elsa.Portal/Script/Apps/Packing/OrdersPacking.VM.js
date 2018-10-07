var app = app || {};
app.ordersPacking = app.ordersPacking || {};
app.ordersPacking.ViewModel = app.ordersPacking.ViewModel || function() {

    var self = this;

    self.currentOrder = null;

    var adjustServerOrderObject = function (order) {
        order.hasCustomerNote = (!!order.CustomerNote) && (order.CustomerNote.length > 0);
        order.hasInternalNote = (!!order.InternalNote) && (order.InternalNote.length > 0);
    };

    self.searchOrder = function(qry) {
        
        if ((!qry) || (qry.length < 3)) {
            throw new Error("Musí být alespoň tři čísla");
        }

        lt.api("/ordersPacking/findOrder").query({ "number": qry }).get(function(order) {
            adjustServerOrderObject(order);
            self.currentOrder = order;
        });

    };

    self.cancelCurrentOrder = function() {
        self.currentOrder = null;
        lt.notify();
    };
};

app.ordersPacking.vm = app.ordersPacking.vm || new app.ordersPacking.ViewModel();

