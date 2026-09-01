var app = app || {};
app.OrdersInfo = app.OrdersInfo || {};

app.OrdersInfo.VM = app.OrdersInfo.VM || function () {
    var self = this;
    var detailTabDefinitions = [];
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
            OrderStatusId: readInt(params, "OrderStatusId", null),
            ContainsPlacedItemWildcard: params.get("ContainsPlacedItemWildcard") || null,
            MaterialBatchNumberWildcard: params.get("MaterialBatchNumberWildcard") || null,
            CustomerNameWildcard: params.get("CustomerNameWildcard") || null,
            ShipmentMethodNameWildcard: params.get("ShipmentMethodNameWildcard") || null,
            PaymentMethodName: params.get("PaymentMethodName") || null,
            DiscountTextWildcard: params.get("DiscountTextWildcard") || null
        };
    };

    var createDetailTab = function (definition, orderId) {
        return {
            id: definition.id,
            tabTitle: definition.tabTitle,
            action: definition.action,
            control: definition.control,
            prepareData: definition.prepareData,
            orderId: orderId,
            active: 0,
            contentControl: null,
            data: null,
            isLoading: false,
            isLoaded: false,
            isEmpty: false,
            error: null,
            hasError: false
        };
    };

    var addDetailTabToOrder = function (order, definition) {
        if (order.details.some(function (detail) { return detail.id === definition.id; })) {
            return;
        }

        order.details.push(createDetailTab(definition, order.OrderId));
    };

    var formatOrder = function (order) {
        var purchaseDate = new Date(order.PurchaseDate);

        order.PurchaseDateText = isNaN(purchaseDate.getTime())
            ? ""
            : purchaseDate.toLocaleDateString("cs-CZ");
        order.PurchaseDateTitle = isNaN(purchaseDate.getTime())
            ? ""
            : purchaseDate.toLocaleString("cs-CZ");
        order.PriceWithVatText = Number(order.PriceWithVat || 0).toLocaleString("cs-CZ", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
        order.CanOpenCustomerInCrm = !!(window.can && window.can.DistributorsApp)
            && /^C/.test(order.CustomerErpUid || "");
        order.IsExpanded = false;
        order.detailControl = null;
        order.details = [];

        detailTabDefinitions.forEach(function (definition) {
            addDetailTabToOrder(order, definition);
        });

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

        if (order.IsExpanded) {
            order.detailControl = "/UI/OrdersInfo/OrdersInfoDetail.html";

            if ((!order.details.some(function (detail) { return detail.active; })) && (order.details.length > 0)) {
                self.activateDetailTab(order.details[0].id, order);
            }
        }

        lt.notify();
    };

    self.openCustomerInCrm = function (order) {
        if (!order || !order.CanOpenCustomerInCrm) {
            return;
        }

        var targetWindow = window.open("", "_blank");

        lt.api("/ordersInfo/getCrmLink")
            .query({ "orderId": order.OrderId })
            .onerror(function (error) {
                if (targetWindow) {
                    targetWindow.close();
                }

                lanta.Extensions.defaultErrorHandler(error);
            })
            .get(function (url) {
                if (targetWindow) {
                    targetWindow.opener = null;
                    targetWindow.location.href = url;
                } else {
                    window.open(url, "_blank");
                }
            });
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

    self.getPaymentMethods = function (callback) {
        lt.api("/ordersInfo/getPaymentMethods").get(callback);
    };

    self.registerDetailTab = function (definition) {
        if (!definition || !definition.id || !definition.tabTitle || !definition.control) {
            throw new Error("Neplatná definice záložky detailu objednávky");
        }

        if (detailTabDefinitions.some(function (item) { return item.id === definition.id; })) {
            return;
        }

        var normalizedDefinition = {
            id: definition.id,
            tabTitle: definition.tabTitle,
            action: definition.action || null,
            control: definition.control,
            prepareData: definition.prepareData || function (data) { return data; }
        };

        detailTabDefinitions.push(normalizedDefinition);

        self.orders.forEach(function (order) {
            addDetailTabToOrder(order, normalizedDefinition);
        });

        lt.notify();
    };

    self.activateDetailTab = function (tabId, order) {
        if (!order) {
            return;
        }

        order.details.forEach(function (detail) {
            detail.active = detail.id === tabId ? 1 : 0;

            if (!detail.active) {
                return;
            }

            detail.contentControl = detail.control;

            if (detail.isLoaded || detail.isLoading) {
                return;
            }

            if (!detail.action) {
                detail.isLoaded = true;
                return;
            }

            var url = detail.action;
            if (url.indexOf("/") < 0) {
                url = "/ordersInfo/" + url;
            }

            detail.isLoading = true;
            detail.error = null;
            detail.hasError = false;

            lt.api(url)
                .query({ "orderId": order.OrderId })
                .onerror(function (error) {
                    detail.isLoading = false;
                    detail.hasError = true;
                    detail.error = error || "Detail objednávky se nepodařilo načíst.";
                    lt.notify();
                })
                .get(function (data) {
                    detail.data = detail.prepareData(data);
                    detail.isEmpty = Array.isArray(detail.data) && detail.data.length === 0;
                    detail.isLoaded = true;
                    detail.isLoading = false;
                    lt.notify();
                });
        });

        lt.notify();
    };
};

app.OrdersInfo.vm = app.OrdersInfo.vm || new app.OrdersInfo.VM();

app.OrdersInfo.vm.registerDetailTab({
    id: "orderNotes",
    tabTitle: "Poznámky",
    action: "getOrderNotes",
    control: "/UI/OrdersInfo/DetailTabs/OrderNotes.html"
});

app.OrdersInfo.vm.registerDetailTab({
    id: "orderItems",
    tabTitle: "Položky",
    action: "getOrderItems",
    control: "/UI/OrdersInfo/DetailTabs/OrderItems.html",
    prepareData: function (items) {
        var formatItems = function (sourceItems) {
            (sourceItems || []).forEach(function (item) {
                item.QuantityText = Number(item.Quantity || 0).toLocaleString("cs-CZ", {
                    maximumFractionDigits: 6
                });
                item.PriceWithVatText = Number(item.PriceWithVat || 0).toLocaleString("cs-CZ", {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2
                });
                item.HasBatchAssignments = (item.BatchAssignments || []).length > 0;
                (item.BatchAssignments || []).forEach(function (assignment) {
                    var assignmentDate = new Date(assignment.AssignmentDt);
                    var assignedBy = assignment.AssignedBy || "Neznámý uživatel";
                    var emailSeparator = assignedBy.indexOf("@");

                    if (emailSeparator > 0) {
                        assignedBy = assignedBy.substring(0, emailSeparator)
                            .replace(/[._-]+/g, " ")
                            .replace(/(^|\s)\S/g, function (character) { return character.toUpperCase(); });
                    }

                    assignment.QuantityText = Number(assignment.Quantity || 0).toLocaleString("cs-CZ", {
                        maximumFractionDigits: 6
                    });
                    assignment.AssignmentDtText = isNaN(assignmentDate.getTime())
                        ? ""
                        : assignmentDate.toLocaleDateString("cs-CZ");
                    assignment.SummaryText = assignment.QuantityText
                        + "× " + assignment.BatchNumber
                        + " (" + assignedBy
                        + (assignment.AssignmentDtText ? " " + assignment.AssignmentDtText : "")
                        + ")";
                });
                item.BatchAssignmentsText = (item.BatchAssignments || []).map(function (assignment) {
                    return assignment.SummaryText;
                }).join(", ");
                formatItems(item.Children);
            });

            return sourceItems || [];
        };

        return formatItems(items);
    }
});

app.OrdersInfo.vm.registerDetailTab({
    id: "shippingDetail",
    tabTitle: "Doprava",
    action: "getShippingDetail",
    control: "/UI/OrdersInfo/DetailTabs/ShippingDetail.html",
    prepareData: function (detail) {
        detail = detail || {};
        detail.ShippingMethodName = detail.ShippingMethodName || "Neuvedený způsob dopravy";
        detail.HasDpdUrl = !!detail.DpdUrl;
        detail.HasPacketaUrl = !!detail.PacketaUrl;
        detail.HasDeliveryAddress = !!detail.DeliveryAddress;
        detail.ShippingCostText = Number(detail.TaxedShippingCost || 0).toLocaleString("cs-CZ", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }) + (detail.OrderCurrencySymbol ? " " + detail.OrderCurrencySymbol : "");
        detail.ItemWeights = detail.ItemWeights || [];
        detail.TotalItemsWeightText = Number(detail.TotalItemsWeightKg || 0).toLocaleString("cs-CZ", {
            maximumFractionDigits: 6
        }) + " kg";
        detail.ItemWeightsTooltip = detail.ItemWeights.length > 0
            ? detail.ItemWeights.map(function (item) {
                var weightText = item.WeightKg === null || item.WeightKg === undefined
                    ? "neuvedeno"
                    : Number(item.WeightKg).toLocaleString("cs-CZ", { maximumFractionDigits: 6 }) + " kg";

                return (item.ItemName || "Neznámá položka") + ": " + weightText;
            }).join("\n")
            : "Objednávka nemá žádné položky.";

        var packingDate = new Date(detail.PackingDt);
        detail.PackingDtText = detail.PackingDt && !isNaN(packingDate.getTime())
            ? packingDate.toLocaleString("cs-CZ")
            : "";

        if (detail.DeliveryAddress) {
            var address = detail.DeliveryAddress;
            var streetNumbers = [address.DescriptiveNumber, address.OrientationNumber]
                .filter(function (value) { return !!value; })
                .join("/");

            address.RecipientName = [address.FirstName, address.LastName]
                .filter(function (value) { return !!value; })
                .join(" ");
            address.StreetText = [address.Street, streetNumbers]
                .filter(function (value) { return !!value; })
                .join(" ");
            address.CityText = [address.Zip, address.City]
                .filter(function (value) { return !!value; })
                .join(" ");
            address.HasCompanyName = !!address.CompanyName;
            address.HasPhone = !!address.Phone;
            address.HasNote = !!address.Note;
            address.PhoneUrl = address.Phone ? "tel:" + address.Phone : "";
        }

        return detail;
    }
});

app.OrdersInfo.vm.registerDetailTab({
    id: "paymentDetail",
    tabTitle: "Platba",
    action: "getPaymentDetail",
    control: "/UI/OrdersInfo/DetailTabs/PaymentDetail.html",
    prepareData: function (detail) {
        detail = detail || {};
        detail.PaymentMethodName = detail.PaymentMethodName || "Neuvedená platební metoda";
        detail.HasComgateUrl = !!detail.ComgateUrl;
        detail.PaymentCostText = Number(detail.TaxedPaymentCost || 0).toLocaleString("cs-CZ", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }) + (detail.OrderCurrencySymbol ? " " + detail.OrderCurrencySymbol : "");

        var pairingDate = new Date(detail.PaymentPairingDt);
        detail.PaymentPairingDtText = detail.PaymentPairingDt && !isNaN(pairingDate.getTime())
            ? pairingDate.toLocaleString("cs-CZ")
            : "";

        if (detail.Payment) {
            var paymentDate = new Date(detail.Payment.PaymentDt);
            detail.Payment.PaymentDtText = isNaN(paymentDate.getTime())
                ? ""
                : paymentDate.toLocaleString("cs-CZ");
            detail.Payment.AmountText = Number(detail.Payment.Amount || 0).toLocaleString("cs-CZ", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            }) + (detail.Payment.CurrencySymbol ? " " + detail.Payment.CurrencySymbol : "");
        }

        return detail;
    }
});

app.OrdersInfo.vm.registerDetailTab({
    id: "systemDetail",
    tabTitle: "System",
    action: "getOrderSysInfo",
    control: "/UI/OrdersInfo/DetailTabs/SystemDetail.html",
    prepareData: function (detail) {
        detail = detail || {};
        detail.Events = detail.Events || [];
        detail.HasEvents = detail.Events.length > 0;

        detail.Events.forEach(function (orderEvent) {
            var eventDate = new Date(orderEvent.Dt);
            orderEvent.DtText = isNaN(eventDate.getTime())
                ? ""
                : eventDate.toLocaleString("cs-CZ");
            orderEvent.HasUser = !!orderEvent.User;
        });

        return detail;
    }
});
