var app = app || {};
app.ordersPacking = app.ordersPacking || {};
app.ordersPacking.ViewModel = app.ordersPacking.ViewModel || function() {

    var self = this;

    self.currentOrder = null;
    self.currentQuery = "";
    self.ordersToPack = null;
    self.checklistMode = false;
    self.loadingInternalNote = false;

    var loadedIntNotes = {};

    var loadInternalNote = function (orderId) {

        var setNote = function (text) {
            self.currentOrder.InternalNote = text;
            self.currentOrder.hasInternalNote = (self.currentOrder.InternalNote) && (self.currentOrder.InternalNote.length > 0);
            self.currentOrder.displayInternalNote = self.currentOrder.hasInternalNote;
        };

        if (loadedIntNotes.hasOwnProperty(orderId)) {
            var noteText = loadedIntNotes[orderId];
            setNote(noteText);
            lt.notify();
            return;
        }

        self.loadedIntNotes = {};

        self.loadingInternalNote = true;
        lt.notify();

        lt.api("/ordersPacking/getMostRecentInternalNote").silent().query({ "orderId": orderId }).get(function (note) {

            console.log(note);

            if ((!self.currentOrder) || (self.currentOrder.OrderId !== note.OrderId))
                return; 

            loadedIntNotes[orderId] = note.FieldValue;

            setNote(note.FieldValue);

            self.loadingInternalNote = false;           

        });

    };

    var updateChecklistMode = function (items, mode) {

        for (var i = 0; i < items.length; i++) {
            items[i].showCheckbox = !!mode;
        }
    };

    var adjustServerKitItemObject = function(kitItem) {
        
    };

    var getLocalOrderRecord = function (orderId, createNew, update) {

        if (!orderId) {
            throw new Error("Invalid state");
        }

        var allObjects = [];

        var allObjectsJson = localStorage.getItem("orderRecords");
        if (!!allObjectsJson) {
            allObjects = JSON.parse(allObjectsJson);
        }
                
        var orderRecord = null;
        var orderRecords = allObjects.filter(function (r) { return r.orderId === orderId; });
        if (orderRecords.length > 0) {
            orderRecord = orderRecords[0];
        }

        if ((!orderRecord) && (!createNew))
            return null;

        if (!orderRecord) {
            orderRecord = {
                "orderId": orderId,
                "checkedItems": []
            };

            allObjects.push(orderRecord);
        }

        orderRecord.lastAccess = new Date().getTime();

        if (!!update)
            update(orderRecord);

        localStorage.setItem("orderRecords", JSON.stringify(allObjects));

        return orderRecord;
    };

    var validateBatchAssignment = function(orderItem) {
        var asig = orderItem.BatchAssignment;
        if ((!asig) || (asig.length === 0)) {
            throw new Error("Chybí šarže u '" + orderItem.ProductName + "'");
        }

        for (var i = 0; i < asig.length; i++) {
            var a = asig[i];

            if ((!a.BatchNumber) || (!a.MaterialBatchId) || (a.BatchNumber.trim().length === 0)) {
                throw new Error("Chybí šarže u '" + orderItem.ProductName +"'");
            }
        }
    };

    self.validateCurrentOrder = function() {
        
        if (!self.currentOrder) {
            throw new Error("Neni vybrana objednavka");
        }

        if (!!self.checklistMode) {
            var uncheckedItem = self.currentOrder.Items.filter(i => !i.isChecked);
            if (uncheckedItem.length > 0) {
                // ask the user whether they wants to proceed 
                var result = confirm("Všechny položky nebyly zaškrtnuty. Chcete přesto pokračovat?");
                if (!result)
                    return;
            }
        }

        for (var i = 0; i < self.currentOrder.Items.length; i++) {
            var orderItem = self.currentOrder.Items[i];

            if (orderItem.KitItems && orderItem.KitItems.length > 0) {
                for (var j = 0; j < orderItem.KitItems.length; j++) {
                    var kitGroup = orderItem.KitItems[j];

                    if ((!kitGroup.hasSelectedItem) || (!kitGroup.SelectedItem)) {
                        throw new Error("Není kompletní sada '" + orderItem.ProductName + "'!");
                    } else {
                        validateBatchAssignment(kitGroup.SelectedItem);
                    }
                }
            } else {
                validateBatchAssignment(orderItem);
            }
        }
        
    };

    var adjustServerOrderItemObject = function(orderItem, checkedItems) {
        orderItem.highlightQuantity = orderItem.Quantity !== "1";
        orderItem.isKit = (!!orderItem.KitItems) && (orderItem.KitItems.length > 0);
        orderItem.isChecked = checkedItems.indexOf(orderItem.ItemId) >= 0;

        if (orderItem.isKit) {

            var lastKitIndex = -1;
            if (orderItem.KitItems.length > 0) {
                lastKitIndex = orderItem.KitItems[orderItem.KitItems.length - 1].KitItemIndex;
            }

            for (var i = 0; i < orderItem.KitItems.length; i++) {
                var kitItem = orderItem.KitItems[i];
                kitItem.uid = kitItem.GroupId + ":" + kitItem.KitItemIndex;

                kitItem.hasSelectedItem = (!!kitItem.SelectedItem);

                kitItem.isKitItemIndexHead = (kitItem.KitItemIndex !== lastKitIndex);
                lastKitIndex = kitItem.KitItemIndex;
                kitItem.kitItemIndexText = (lastKitIndex + 1) + ".";

                for (var j = 0; j < kitItem.GroupItems.length; j++) {
                    kitItem.GroupItems[j].refData = {
                        orderItemId: orderItem.ItemId,
                        kitItemIndex: kitItem.KitItemIndex
                    };

                    kitItem.GroupItems[j].Shortcut = kitItem.GroupItems[j].Shortcut || kitItem.GroupItems[j].ItemName;
                }
            }   
        }
    };

    var adjustServerOrderObject = function (order) {
        order.hasCustomerNote = (!!order.CustomerNote) && (order.CustomerNote.length > 0);
        order.hasInternalNote = false;
        order.displayInternalNote = true;
        order.hasDiscount = (!!order.DiscountsText) && (order.DiscountsText) && (order.DiscountsText.length > 0);

        var localOrderRecord = getLocalOrderRecord(order.OrderId, false, null);

        var checkedItems = [];
        if (!!localOrderRecord) {
            self.checklistMode = !!localOrderRecord.checklistMode;
            checkedItems = localOrderRecord.checkedItems || checkedItems;
        } else {
            self.checklistMode = order.Items.length >= 10;
        }
                
        for (var i = 0; i < order.Items.length; i++) {
            adjustServerOrderItemObject(order.Items[i], checkedItems);
        }

        updateChecklistMode(order.Items, self.checklistMode);
    };

    
    self.searchOrder = function(qry) {
        
        if ((!qry) || (qry.length < 3)) {
            throw new Error("Musí být alespoň tři čísla");
        }

        lt.api("/ordersPacking/findOrder").query({ "number": qry }).get(function(order) {
            adjustServerOrderObject(order);
            self.currentOrder = order;
            loadInternalNote(order.OrderId);

            self.currentQuery = qry;
            setTimeout(loadOrdersToPack, 500);
        });

    };

    self.cancelCurrentOrder = function() {
        self.currentOrder = null;
        lt.notify();
    };

    self.selectKitItem = function(orderItemId, kitItemId, kitItemIndex) {

        lt.api("/ordersPacking/selectKitItem")
            .query({
                "orderId": self.currentOrder.OrderId,
                "orderItemId": orderItemId,
                "kitItemId": kitItemId,
                "kitItemIndex": kitItemIndex
            })
            .get(function (updated) {
                adjustServerOrderObject(updated);
                self.currentOrder = updated;
                loadInternalNote(updated.OrderId);
            });
    };

    self.undoKitItemSelection = function(selectedItemId) {
        
        for (var i = 0; i < self.currentOrder.Items.length; i++) {
            var orderItem = self.currentOrder.Items[i];

            for (var j = 0; j < orderItem.KitItems.length; j++) {
                var kitItem = orderItem.KitItems[j];

                if ((!!kitItem.SelectedItem) && (kitItem.SelectedItem.ItemId === selectedItemId)) {
                    kitItem.hasSelectedItem = false;
                    lt.notify();
                    return;
                }
            }
        }
    };

    self.packOrder = function() {
                
        self.validateCurrentOrder();

        var ordid = self.currentOrder.OrderId;

        self.currentOrder = null;
        lt.notify();

        lt.api("/ordersPacking/packOrder").query({ "orderId": ordid }).get(function() {
            self.currentQuery = "";

            if (self.ordersToPack) {
                for (var i = 0; i < self.ordersToPack.length; i++) {
                    if (self.ordersToPack[i].OrderId === ordid) {
                        self.ordersToPack.splice(i, 1);
                        break;
                    }
                }
            }

            setTimeout(loadOrdersToPack, 100);
        });

    };

    self.setBatch = function(assignmentModel, query) {
        var request = {
            OrderId: self.currentOrder.OrderId,
            OrderItemId: assignmentModel.OrderItemId,
            OriginalBatchId: assignmentModel.MaterialBatchId,
            OriginalBatchNumber: assignmentModel.BatchNumber,
            NewBatchSearchQuery: query
        };

        lt.api("/ordersPacking/SetItemBatchAllocation").body(request).post(function(order) {
            adjustServerOrderObject(order);
            self.currentOrder = order;
            loadInternalNote(order.OrderId);
        });
    };

    self.decreaseAmount = function(assignmentModel) {
        var request = {
            OrderId: self.currentOrder.OrderId,
            OrderItemId: assignmentModel.OrderItemId,
            OriginalBatchId: assignmentModel.MaterialBatchId,
            OriginalBatchNumber: assignmentModel.BatchNumber,
            NewAmount: assignmentModel.AssignedQuantity - 1
        };

        lt.api("/ordersPacking/SetItemBatchAllocation").body(request).post(function (order) {
            adjustServerOrderObject(order);
            self.currentOrder = order;
            loadInternalNote(order.OrderId);
        });
    };

    var loadOrdersToPack = function () {
        self.ordersToPack = [];
        lt.notify();
        /*lt.api("/ordersPacking/getOrdersToPack").silent().get(function(toPack) {
            self.ordersToPack = toPack;
        });*/
    };

    setTimeout(loadOrdersToPack, 0);

    self.releaseBatches = function() {
        lt.api("/materialBatches/releaseUnsentOrdersAllocations").get(function() {
            self.cancelCurrentOrder();
        });
    };

    self.toggleChecklistMode = function () {

        if (!self.currentOrder)
            return;

        self.checklistMode = !self.checklistMode;
        getLocalOrderRecord(self.currentOrder.OrderId, true, function (orderRecord) {
            orderRecord.checklistMode = !!self.checklistMode;
        });

        updateChecklistMode(self.currentOrder.Items, self.checklistMode);
    };

    self.setItemChecked = function (itemId, checked) {
        var foundItems = self.currentOrder.Items.filter(function (i) { return i.ItemId === itemId; });
        if (foundItems.length !== 1)
            return;

        foundItems[0].isChecked = !!checked;

        getLocalOrderRecord(self.currentOrder.OrderId, true, function (orderRecord) {
            if (checked) {
                if (orderRecord.checkedItems.indexOf(itemId) < 0)
                    orderRecord.checkedItems.push(itemId);
            }
            else {
                var idx = orderRecord.checkedItems.indexOf(itemId);
                if (idx >= 0)
                    orderRecord.checkedItems.splice(idx, 1);
            }
        });

    };

    var cleanOrderRecords = function () {
        // remove order records where last access is older than 3 days
        var allObjectsJson = localStorage.getItem("orderRecords");
        if(!allObjectsJson)
            return;

        var allObjects = JSON.parse(allObjectsJson);
        var threeDaysAgo = new Date().getTime() - 3 * 24 * 60 * 60 * 1000;

        var newObjects = allObjects.filter(function (r) {
            return r.lastAccess >= threeDaysAgo;
        });

        localStorage.setItem("orderRecords", JSON.stringify(newObjects));
    };

    setTimeout(cleanOrderRecords, 0);
};

app.ordersPacking.vm = app.ordersPacking.vm || new app.ordersPacking.ViewModel();

