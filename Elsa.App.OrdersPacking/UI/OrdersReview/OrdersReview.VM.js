var app = app || {};
app.OrdersReview = app.OrdersReview || {};
app.OrdersReview.VM = app.OrdersReview.VM
||
function () {
    const self = this;

    self.orders = [];


    const receiveOrders = (orders) => {

        orders.forEach(o => {
            o.items = [];
            o.hasItems = false;
            o.hasNote = !!o.CustomerNote;            
            o.mailHref = "mailto:" + o.CustomerEmail + "?subject=" + encodeURIComponent("Vaše objednávka na biorythme.cz");
        });

        self.orders = orders;
    };

    const load = () => lt.api("/OrdersReview/getOrders").get(receiveOrders);

    self.markReviewed = (orderId) => lt.api("/OrdersReview/markReviewed").query({ "orderId": orderId }).post(receiveOrders);

    self.loadItems = (model) => lt.api("/OrdersReview/getOrderItems").query({ "orderId": model.OrderId }).post((items) => {
        model.items = items;
        model.hasItems = true;
    });

    load();   
};

app.OrdersReview.vm = app.OrdersReview.vm || new app.OrdersReview.VM();