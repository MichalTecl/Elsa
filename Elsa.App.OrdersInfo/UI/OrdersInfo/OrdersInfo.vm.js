var app = app || {};
app.OrdersInfo = app.OrdersInfo || {};

app.OrdersInfo.VM = app.OrdersInfo.VM || function () {
    var self = this;
    var placedItemNames = null;
    var placedItemNamesCallbacks = [];
    var query = null;

    self.orders = [];
    self.isLoading = false;
    self.hasOrders = false;
    self.isEmpty = false;
    self.pageLabel = "Strana 1";
    self.previousDisabled = true;
    self.nextDisabled = true;

    var receivePlacedItemNames = function (names) {
        placedItemNames = names || [];

        placedItemNamesCallbacks.forEach(function (callback) {
            callback(placedItemNames);
        });

        placedItemNamesCallbacks = [];
    };

    var readInt = function (params, name, defaultValue) {
        var value = parseInt(params.get(name), 10);
        return isNaN(value) ? defaultValue : value;
    };

    var readDate = function (params, name, endOfDay) {
        var value = params.get(name);
        if (!value) {
            return null;
        }

        if (/^\d{4}-\d{2}-\d{2}$/.test(value)) {
            return value + (endOfDay ? "T23:59:59" : "T00:00:00");
        }

        return value;
    };

    var readQueryString = function () {
        var params = new URLSearchParams(window.location.search);

        return {
            Page: Math.max(readInt(params, "Page", 0), 0),
            PageSize: Math.min(Math.max(readInt(params, "PageSize", 50), 1), 200),
            OrderNumber: params.get("OrderNumber") || null,
            MinPurchaseDt: readDate(params, "MinPurchaseDt", false),
            MaxPurchaseDt: readDate(params, "MaxPurchaseDt", true),
            ErpStatuses: params.getAll("ErpStatuses"),
            ContainsPlacedItemWildcard: params.get("ContainsPlacedItemWildcard") || null,
            CustomerNameWildcard: params.get("CustomerNameWildcard") || null,
            ShipmentMethodNameWildcard: params.get("ShipmentMethodNameWildcard") || null,
            PaymentMethodNameWildcard: params.get("PaymentMethodNameWildcard") || null
        };
    };

    var formatOrder = function (order) {
        var purchaseDate = new Date(order.PurchaseDate);

        order.PurchaseDateText = isNaN(purchaseDate.getTime())
            ? ""
            : purchaseDate.toLocaleString("cs-CZ");
        order.PriceWithVatText = Number(order.PriceWithVat || 0).toLocaleString("cs-CZ", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
        order.IsExpanded = false;

        return order;
    };

    var updatePageInAddress = function () {
        var params = new URLSearchParams(window.location.search);

        if (query.Page > 0) {
            params.set("Page", query.Page);
        } else {
            params.delete("Page");
        }

        var queryString = params.toString();
        var address = window.location.pathname + (queryString ? "?" + queryString : "") + window.location.hash;
        window.history.replaceState(null, "", address);
    };

    var loadOrders = function () {
        self.isLoading = true;
        self.isEmpty = false;
        self.hasOrders = false;
        lt.notify();

        lt.api("/ordersInfo/query")
            .body(query)
            .post(function (orders) {
                self.orders = (orders || []).map(formatOrder);
                self.hasOrders = self.orders.length > 0;
                self.isEmpty = !self.hasOrders;
                self.pageLabel = "Strana " + (query.Page + 1);
                self.previousDisabled = query.Page === 0;
                self.nextDisabled = self.orders.length < query.PageSize;
                self.isLoading = false;
            });
    };

    self.init = function () {
        query = readQueryString();
        loadOrders();
    };

    self.previousPage = function () {
        if (self.previousDisabled || self.isLoading) {
            return;
        }

        query.Page--;
        updatePageInAddress();
        loadOrders();
    };

    self.nextPage = function () {
        if (self.nextDisabled || self.isLoading) {
            return;
        }

        query.Page++;
        updatePageInAddress();
        loadOrders();
    };

    self.toggleOrderDetail = function (orderId) {
        var order = self.orders.find(function (item) {
            return item.OrderId === orderId;
        });

        if (!order) {
            return;
        }

        order.IsExpanded = !order.IsExpanded;
        lt.notify();
    };

    self.getPlacedItemNames = function (searchText, callback) {
        if (placedItemNames !== null) {
            callback(placedItemNames);
            return;
        }

        placedItemNamesCallbacks.push(callback);

        if (placedItemNamesCallbacks.length === 1) {
            lt.api("/ordersInfo/getPlacedItemNames").get(receivePlacedItemNames);
        }
    };

    self.getErpStatuses = function (callback) {
        lt.api("/ordersInfo/getErpStatuses").get(callback);
    };
};

app.OrdersInfo.vm = app.OrdersInfo.vm || new app.OrdersInfo.VM();
