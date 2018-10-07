var app = app || {};
app.ordersPacking = app.ordersPacking || {};
app.ordersPacking.ViewModel = app.ordersPacking.ViewModel || function() {

    var self = this;

    self.currentOrder = null;

    var adjustServerKitItemObject = function(kitItem) {
        
    };

    var adjustServerOrderItemObject = function(orderItem) {
        orderItem.highlightQuantity = orderItem.Quantity !== "1";
        orderItem.isKit = (!!orderItem.KitItems) && (orderItem.KitItems.length > 0);

        if (orderItem.isKit) {
            
            for (var i = 0; i < orderItem.KitItems.length; i++) {
                var kitItem = orderItem.KitItems[i];
                kitItem.uid = kitItem.GroupId + ":" + kitItem.KitItemIndex;

                kitItem.hasSelectedItem = (!!kitItem.SelectedItem);

                for (var j = 0; j < kitItem.GroupItems.length; j++) {
                    kitItem.GroupItems[j].refData = {
                        orderItemId: orderItem.ItemId,
                        kitItemIndex: kitItem.KitItemIndex
                    };
                }
            }   
        }
    };

    var adjustServerOrderObject = function (order) {
        order.hasCustomerNote = (!!order.CustomerNote) && (order.CustomerNote.length > 0);
        order.hasInternalNote = (!!order.InternalNote) && (order.InternalNote.length > 0);

        for (var i = 0; i < order.Items.length; i++) {
            adjustServerOrderItemObject(order.Items[i]);
        }
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
            });
    };

    self.undoKitItemSelection = function(selectedItemId) {
        
        for (var i = 0; i < self.currentOrder.Items.length; i++) {
            var orderItem = self.currentOrder.Items[i];

            for (var j = 0; j < orderItem.KitItems.length; j++) {
                var kitItem = orderItem.KitItems[j];

                if ((!!kitItem.SelectedItem) && (kitItem.SelectedItem.Id === selectedItemId)) {
                    kitItem.hasSelectedItem = false;
                    lt.notify();
                    return;
                }
            }
        }
    };
};

app.ordersPacking.vm = app.ordersPacking.vm || new app.ordersPacking.ViewModel();

