var app = app || {};
app.paymentPairing = app.paymentPairing || {};
app.paymentPairing.ViewModel = app.paymentPairing.ViewModel || function() {

    var self = this;

    this.suggestedPairs = null;

    var receive = function (pairs) {

        if (!!pairs) {
            for (var i = 0; i < pairs.length; i++) {
                pairs[i].noPayment = (!pairs[i].Payment);

                pairs[i].priceNotMatch = !((!pairs[i].Payment) || pairs[i].Order.Price === pairs[i].Payment.Amount);
                pairs[i].symbolNotMatch = !((!pairs[i].Payment) || pairs[i].Order.VariableSymbol === pairs[i].Payment.VariableSymbol);
            }
        }

        self.suggestedPairs = pairs;
    };

    this.update = function() {
        lt.api("/paymentPairing/getUnpaidOrders").useCache().get(receive);
    };

    this.setOrderPaid = function(orderId, paymentId) {

        var i = self.suggestedPairs.length;
        while (i--) {
            var pair = self.suggestedPairs[i];
            if (pair.Order.OrderId === orderId || ((!!pair.Payment) && (pair.Payment.PaymentId === paymentId))) {
                self.suggestedPairs.splice(i, 1);
            }
        }

        lt.notify();

        lt.api("/paymentPairing/pair").query({ "orderId": orderId, "paymentId": paymentId }).get(receive);

    };

    self.update();


};

app.paymentPairing.vm = app.paymentPairing.vm || new app.paymentPairing.ViewModel();