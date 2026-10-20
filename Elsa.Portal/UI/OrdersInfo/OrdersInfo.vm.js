var app = app || {};
app.OrdersInfo = app.OrdersInfo || {};

app.OrdersInfo.AGGREGATION_PARAM_PREFIX = "AggregationParam.";

app.OrdersInfo.VM = app.OrdersInfo.VM || function () {
    var self = this;
    var detailTabDefinitions = [];
    var placedItemNames = null;
    var placedItemNamesCallbacks = [];
    var query = null;
    var aggregationId = null;
    var aggregationParams = {};
    var aggregationParametersRenderer = null;

    self.orders = [];
    self.isLoading = false;
    self.hasOrders = false;
    self.isEmpty = false;
    self.pageLabel = "Strana 1";
    self.previousDisabled = true;
    self.nextDisabled = true;
    self.exportDisabled = true;
    self.totalOrdersText = "0";
    self.totalPriceWithVatText = "0,00";
    self.isAggregated = false;
    self.hasAggregatedResult = false;
    self.isAggregatedEmpty = false;
    self.aggregationMessage = "";
    self.hasAggregationMessage = false;
    self.aggregatedColumns = [];
    self.aggregatedRows = [];
    self.aggregationLegend = "";
    self.hasAggregationLegend = false;
    self.aggregatedExportDisabled = true;
    self.aggregationParameters = [];
    self.isDrilldown = false;
    self.drilldownTitle = "";
    self.drilldownBackUrl = "";
    self.drilldownCancelUrl = "";
    self.batchDialogOpen = false;
    self.batchDialogItem = null;
    self.batchDialogRows = [];
    self.batchDialogRemovedRows = [];
    self.batchDialogDirty = false;
    self.batchDialogSaving = false;
    self.batchDialogError = null;
    self.batchDialogHasError = false;
    self.batchDialogAssignedText = "0";

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
        var paymentMethodNames = params.getAll("PaymentMethodNames");
        var legacyPaymentMethodName = params.get("PaymentMethodName");

        if (paymentMethodNames.length === 0 && legacyPaymentMethodName) {
            paymentMethodNames.push(legacyPaymentMethodName);
        }

        return {
            Page: Math.max(readInt(params, "Page", 0), 0),
            PageSize: Math.min(Math.max(readInt(params, "PageSize", 50), 1), 200),
            OrderNumber: params.get("OrderNumber") || null,
            MinPurchaseDt: readDate(params, "MinPurchaseDt", false),
            MaxPurchaseDt: readDate(params, "MaxPurchaseDt", true),
            MinPriceWithVat: params.get("MinPriceWithVat") || null,
            MaxPriceWithVat: params.get("MaxPriceWithVat") || null,
            ErpStatuses: params.getAll("ErpStatuses"),
            OrderStatusId: readInt(params, "OrderStatusId", null),
            ContainsPlacedItemWildcard: params.get("ContainsPlacedItemWildcard") || null,
            MaterialBatchNumberWildcard: params.get("MaterialBatchNumberWildcard") || null,
            CustomerNameWildcard: params.get("CustomerNameWildcard") || null,
            ShipmentMethodNameWildcard: params.get("ShipmentMethodNameWildcard") || null,
            PaymentMethodNames: paymentMethodNames,
            DiscountTextWildcard: params.get("DiscountTextWildcard") || null,
            DrilldownAggregationId: params.get("DrilldownAggregationId") || null,
            DrilldownCell: params.get("DrilldownCell") || null,
            DrilldownParams: params.get("DrilldownAggregationId") ? readAggregationParams() : null
        };
    };

    var DRILLDOWN_URL_PARAMS = ["DrilldownAggregationId", "DrilldownCell", "DrilldownTitle"];

    var createAddress = function (params) {
        var queryString = params.toString();
        return window.location.pathname + (queryString ? "?" + queryString : "") + window.location.hash;
    };

    var deleteParams = function (params, predicate) {
        var names = [];
        params.forEach(function (value, name) {
            if (predicate(name) && names.indexOf(name) < 0) {
                names.push(name);
            }
        });
        names.forEach(function (name) { params.delete(name); });
    };

    var isAggregationParamName = function (name) {
        return name.indexOf(app.OrdersInfo.AGGREGATION_PARAM_PREFIX) === 0;
    };

    var createDrilldownUrl = function (drilldownKey, title) {
        var params = new URLSearchParams(window.location.search);

        params.delete("AggregationId");
        params.delete("Page");
        deleteParams(params, isAggregationParamName);
        deleteParams(params, function (name) { return DRILLDOWN_URL_PARAMS.indexOf(name) >= 0; });

        Object.keys(aggregationParams).forEach(function (optionId) {
            params.append(app.OrdersInfo.AGGREGATION_PARAM_PREFIX + optionId, aggregationParams[optionId]);
        });

        params.set("DrilldownAggregationId", aggregationId);
        params.set("DrilldownCell", drilldownKey);
        params.set("DrilldownTitle", title);

        return createAddress(params);
    };

    var initDrilldown = function () {
        var params = new URLSearchParams(window.location.search);
        var drilldownAggregationId = params.get("DrilldownAggregationId");

        self.isDrilldown = !!drilldownAggregationId;
        if (!self.isDrilldown) {
            return;
        }

        self.drilldownTitle = params.get("DrilldownTitle") || params.get("DrilldownCell") || "";

        var backParams = new URLSearchParams(window.location.search);
        deleteParams(backParams, function (name) { return DRILLDOWN_URL_PARAMS.indexOf(name) >= 0; });
        backParams.delete("Page");
        backParams.set("AggregationId", drilldownAggregationId);
        self.drilldownBackUrl = createAddress(backParams);

        var cancelParams = new URLSearchParams(window.location.search);
        deleteParams(cancelParams, function (name) { return DRILLDOWN_URL_PARAMS.indexOf(name) >= 0 || isAggregationParamName(name); });
        cancelParams.delete("Page");
        self.drilldownCancelUrl = createAddress(cancelParams);
    };

    var readAggregationId = function () {
        var params = new URLSearchParams(window.location.search);
        return params.get("AggregationId") || null;
    };

    var readAggregationParams = function () {
        var params = new URLSearchParams(window.location.search);
        var result = {};

        params.forEach(function (value, name) {
            if (name.indexOf(app.OrdersInfo.AGGREGATION_PARAM_PREFIX) === 0) {
                result[name.substring(app.OrdersInfo.AGGREGATION_PARAM_PREFIX.length)] = value;
            }
        });

        return result;
    };

    var receiveAggregationParameters = function (parameters, selectedParameters) {
        selectedParameters = selectedParameters || {};

        self.aggregationParameters = (parameters || []).map(function (parameter) {
            var selectedValue = selectedParameters[parameter.OptionId];

            return {
                OptionId: parameter.OptionId,
                Title: parameter.Title || parameter.OptionId,
                Values: parameter.Values || [],
                SelectedValue: selectedValue === null || selectedValue === undefined ? "" : selectedValue
            };
        });

        aggregationParams = {};
        self.aggregationParameters.forEach(function (parameter) {
            aggregationParams[parameter.OptionId] = parameter.SelectedValue;
        });

        if (aggregationParametersRenderer) {
            aggregationParametersRenderer(self.aggregationParameters);
        }
    };

    var receiveAggregatedResult = function (aggregation) {
        aggregation = aggregation || {};

        var columns = aggregation.Columns || [];
        var rows = aggregation.Rows || [];
        var table = aggregation.Table || {};
        var links = aggregation.Links || {};
        var drilldowns = aggregation.Drilldowns || {};
        var rowsTitle = aggregation.RowsTitle || "";

        self.aggregatedColumns = [{ Key: "rowTitle", Title: aggregation.RowsTitle || "" }].concat(columns.map(function (column, index) {
            return { Key: "c" + index, Title: column };
        }));

        self.aggregatedRows = rows.map(function (row) {
            var values = table[row] || {};
            var rowLinks = links[row] || {};
            var rowDrilldowns = drilldowns[row] || {};

            return {
                Key: row,
                Cells: [{ Key: "rowTitle", Value: row, IsRowTitle: true }].concat(columns.map(function (column, index) {
                    var value = values[column];
                    var drilldownKey = rowDrilldowns[column];
                    var href = drilldownKey
                        ? createDrilldownUrl(drilldownKey, (rowsTitle ? rowsTitle + ": " : "") + row + ", " + column)
                        : (rowLinks[column] || "");

                    return {
                        Key: "c" + index,
                        Value: value === null || value === undefined ? "" : value,
                        IsRowTitle: false,
                        HasLink: !!href,
                        Href: href,
                        LinkTitle: drilldownKey ? "Zobrazit objednávky" : ""
                    };
                }))
            };
        });

        self.aggregationLegend = aggregation.Legend || "";
        self.hasAggregationLegend = !!self.aggregationLegend;
        self.aggregationMessage = aggregation.Message || "";
        self.hasAggregationMessage = !!self.aggregationMessage;
        self.hasAggregatedResult = !self.hasAggregationMessage && self.aggregatedRows.length > 0;
        self.isAggregatedEmpty = !self.hasAggregationMessage && !self.hasAggregatedResult;
        self.aggregatedExportDisabled = !self.hasAggregatedResult;
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
            .query({
                "aggregationId": aggregationId,
                "aggregationParams": JSON.stringify(aggregationParams)
            })
            .body(query)
            .post(function (result) {
                result = result || {};

                if (result.IsAggregated) {
                    self.orders = [];
                    self.isAggregated = true;
                    receiveAggregationParameters(result.AggregationParameters, result.SelectedAggregationParameters);
                    receiveAggregatedResult(result.AggregatedResult);
                    self.isLoading = false;
                    return;
                }

                self.orders = (result.Orders || []).map(formatOrder);
                self.hasOrders = self.orders.length > 0;
                self.isEmpty = !self.hasOrders;
                self.pageLabel = "Strana " + (query.Page + 1);
                self.previousDisabled = query.Page === 0;
                self.nextDisabled = (query.Page + 1) * query.PageSize >= (result.TotalCount || 0);
                self.exportDisabled = (result.TotalCount || 0) === 0;
                self.totalOrdersText = Number(result.TotalCount || 0).toLocaleString("cs-CZ");
                self.totalPriceWithVatText = Number(result.TotalPriceWithVat || 0).toLocaleString("cs-CZ", {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2
                });
                self.isLoading = false;
            });
    };

    self.init = function () {
        query = readQueryString();
        aggregationId = readAggregationId();
        aggregationParams = readAggregationParams();
        initDrilldown();
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

    var downloadXls = function (url, fileName, errorMessage) {
        lt.api.usageManager.notifyOperationStart();

        fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "*/*; charset=utf-8" },
            body: JSON.stringify(query)
        }).then(function (response) {
            if (!response.ok) {
                throw new Error(errorMessage);
            }

            return response.blob();
        }).then(function (blob) {
            var link = document.createElement("a");
            var objectUrl = URL.createObjectURL(blob);
            link.href = objectUrl;
            link.download = fileName;
            link.hidden = true;
            document.body.appendChild(link);
            link.click();
            link.remove();
            URL.revokeObjectURL(objectUrl);
        }).catch(function (error) {
            lanta.Extensions.defaultErrorHandler(error);
        }).then(function () {
            lt.api.usageManager.notifyOperationEnd();
        });
    };

    self.exportXls = function () {
        if (self.exportDisabled || !query) {
            return;
        }

        downloadXls("/ordersInfo/export", "Objednavky.xlsx", "Export objednávek se nepodařilo stáhnout.");
    };

    self.exportAggregatedXls = function () {
        if (self.aggregatedExportDisabled || !query || !aggregationId) {
            return;
        }

        downloadXls("/ordersInfo/exportAggregated?aggregationId=" + encodeURIComponent(aggregationId)
            + "&aggregationParams=" + encodeURIComponent(JSON.stringify(aggregationParams)),
            "Objednavky_agregace.xlsx",
            "Export agregace objednávek se nepodařilo stáhnout.");
    };

    self.getAggregationTypes = function (callback) {
        lt.api("/ordersInfo/getAggregationTypes").get(callback);
    };

    self.setAggregationParametersRenderer = function (renderer) {
        aggregationParametersRenderer = renderer;

        if (renderer) {
            renderer(self.aggregationParameters);
        }
    };

    var updateBatchDialogState = function () {
        var assigned = self.batchDialogRows.reduce(function (sum, row) {
            return sum + Number(row.Quantity || 0);
        }, 0);
        self.batchDialogAssignedText = assigned.toLocaleString("cs-CZ", { maximumFractionDigits: 6 });
        self.batchDialogDirty = self.batchDialogRemovedRows.length > 0
            || self.batchDialogRows.some(function (row) { return row.IsNew; });
    };

    var setBatchDialogError = function (error) {
        self.batchDialogError = error && error.message ? error.message : (error || null);
        self.batchDialogHasError = !!self.batchDialogError;
        lt.notify();
    };

    self.openBatchAssignmentDialog = function (item) {
        if (!item || item.BatchAssignmentEditDisabled) {
            return;
        }

        self.batchDialogItem = item;
        self.batchDialogRows = (item.BatchAssignments || []).map(function (assignment) {
            return Object.assign({}, assignment, {
                IsNew: false,
                RowKey: "assignment-" + assignment.AssignmentId
            });
        });
        self.batchDialogRemovedRows = [];
        self.batchDialogSaving = false;
        self.batchDialogError = null;
        self.batchDialogHasError = false;
        self.batchDialogOpen = true;
        updateBatchDialogState();
        lt.notify();
    };

    self.closeBatchAssignmentDialog = function () {
        if (self.batchDialogSaving) {
            return;
        }

        self.batchDialogOpen = false;
        self.batchDialogItem = null;
        self.batchDialogRows = [];
        self.batchDialogRemovedRows = [];
        self.batchDialogDirty = false;
        self.batchDialogError = null;
        self.batchDialogHasError = false;
        lt.notify();
    };

    self.removeBatchAssignmentRow = function (row) {
        if (!row || self.batchDialogSaving) {
            return;
        }

        if (!row.CanDelete) {
            setBatchDialogError(row.DeleteBlockedReason
                || "Objednávka již byla zahrnuta do účetnictví a přiřazení šarže nelze smazat.");
            return;
        }

        self.batchDialogRows = self.batchDialogRows.filter(function (candidate) {
            return candidate !== row;
        });

        if (!row.IsNew) {
            self.batchDialogRemovedRows.push(row);
        }

        setBatchDialogError(null);
        updateBatchDialogState();
        lt.notify();
    };

    self.addBatchAssignmentRow = function (quantityValue, batchNumberValue) {
        if (self.batchDialogSaving || !self.batchDialogItem) {
            return false;
        }

        var quantity = Number((quantityValue || "").toString().replace(",", "."));
        var batchNumber = (batchNumberValue || "").trim();

        if (!isFinite(quantity) || quantity <= 0) {
            setBatchDialogError("Množství musí být kladné číslo.");
            return false;
        }

        if (!batchNumber) {
            setBatchDialogError("Číslo šarže musí být vyplněno.");
            return false;
        }

        var currentlyAssigned = self.batchDialogRows.reduce(function (sum, row) {
            return sum + Number(row.Quantity || 0);
        }, 0);
        if (currentlyAssigned + quantity > Number(self.batchDialogItem.Quantity || 0)) {
            setBatchDialogError("Celkové přiřazené množství nesmí překročit množství položky.");
            return false;
        }

        self.batchDialogRows.push({
            AssignmentId: null,
            BatchNumber: batchNumber,
            Quantity: quantity,
            QuantityText: quantity.toLocaleString("cs-CZ", { maximumFractionDigits: 6 }),
            AssignedBy: "Nové přiřazení",
            AssignedByText: "Nové přiřazení",
            AssignmentDtText: "",
            CanDelete: true,
            DeleteDisabled: false,
            DeleteBlockedReason: null,
            IsNew: true,
            RowKey: "new-" + Date.now() + "-" + self.batchDialogRows.length
        });
        setBatchDialogError(null);
        updateBatchDialogState();
        lt.notify();
        return true;
    };

    self.saveBatchAssignmentChanges = function () {
        if (!self.batchDialogDirty || self.batchDialogSaving || !self.batchDialogItem) {
            return;
        }

        var changes = self.batchDialogRemovedRows.map(function (row) {
            return { BatchNumber: row.BatchNumber, Delta: -Number(row.Quantity || 0) };
        }).concat(self.batchDialogRows.filter(function (row) {
            return row.IsNew;
        }).map(function (row) {
            return { BatchNumber: row.BatchNumber, Delta: Number(row.Quantity || 0) };
        }));

        self.batchDialogSaving = true;
        setBatchDialogError(null);

        lt.api("/ordersInfo/saveOrderItemBatchAssignments")
            .body({
                OrderId: self.batchDialogItem.OrderId,
                OrderItemId: self.batchDialogItem.ItemId,
                Changes: changes
            })
            .onerror(function (error) {
                self.batchDialogSaving = false;
                setBatchDialogError(error);
            })
            .post(function (items) {
                var order = self.orders.find(function (candidate) {
                    return candidate.OrderId === self.batchDialogItem.OrderId;
                });
                var detail = order && order.details.find(function (candidate) {
                    return candidate.id === "orderItems";
                });

                if (detail) {
                    detail.data = app.OrdersInfo.prepareOrderItems(items);
                    detail.isEmpty = detail.data.length === 0;
                }

                self.batchDialogSaving = false;
                self.closeBatchAssignmentDialog();
            });
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

app.OrdersInfo.prepareOrderItems = function (items) {
    var formatItems = function (sourceItems) {
        (sourceItems || []).forEach(function (item) {
            item.QuantityText = Number(item.Quantity || 0).toLocaleString("cs-CZ", {
                maximumFractionDigits: 6
            });
            item.PriceWithVatText = Number(item.PriceWithVat || 0).toLocaleString("cs-CZ", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            });
            item.CanEditBatchAssignments = !!(window.can && window.can.EditOrderItemBatchAssignments);
            item.BatchAssignmentEditDisabled = !item.CanEditBatchAssignments || item.BatchAssignmentsLocked;
            item.BatchAssignmentButtonTitle = item.BatchAssignmentsLocked
                ? item.BatchAssignmentsLockedReason
                : (item.CanEditBatchAssignments
                    ? "Změnit přiřazení šarží"
                    : "Pro změnu přiřazení šarží nemáte oprávnění");
            item.HasBatchAssignmentDateNotice = !!item.BatchAssignmentDateNotice;

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
                assignment.DeleteDisabled = !assignment.CanDelete;
                assignment.AssignmentDtText = isNaN(assignmentDate.getTime())
                    ? ""
                    : assignmentDate.toLocaleDateString("cs-CZ");
                assignment.AssignedByText = assignedBy;
                assignment.SummaryText = assignment.QuantityText
                    + "× " + assignment.BatchNumber
                    + " (" + assignedBy
                    + (assignment.AssignmentDtText ? " " + assignment.AssignmentDtText : "")
                    + ")";
            });
            item.BatchAssignmentsText = (item.BatchAssignments || []).length > 0
                ? item.BatchAssignments.map(function (assignment) { return assignment.SummaryText; }).join(", ")
                : "Šarže nepřiřazena";
            formatItems(item.Children);
        });

        return sourceItems || [];
    };

    return formatItems(items);
};

app.OrdersInfo.vm.registerDetailTab({
    id: "orderItems",
    tabTitle: "Položky",
    action: "getOrderItems",
    control: "/UI/OrdersInfo/DetailTabs/OrderItems.html",
    prepareData: app.OrdersInfo.prepareOrderItems
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
