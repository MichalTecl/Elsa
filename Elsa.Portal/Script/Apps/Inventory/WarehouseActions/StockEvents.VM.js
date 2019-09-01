var app = app || {};
app.StockEvents = app.StockEvents || {};
app.StockEvents.ViewModel = app.StockEvents.ViewModel || function() {
	var self = this;

	this.eventTypes = null;
	this.currentEventType = null;
	this.materialUnit = null;
	this.batchRequired = false;
	this.currentBatchNumber = "";
    this.suggestedAmounts = [];
    this.materialUnitOverride = {};
    
    var materialId = null;

    var loadEventTypes = function () {
        
        lt.api("/stockEvents/getEventTypes").get(function(types) {
        	self.eventTypes = types;
            if (self.currentEventType == null && self.eventTypes.length > 0) {
                self.currentEventType = self.eventTypes[0];
            }
            
            var model = app.urlBus.get("setStockEvent");
            if (model != null) {
                self.changeCurrentEventType(model.EventTypeId);
            }
        });
    };

    setTimeout(loadEventTypes, 0);

    this.cancelEdit = function() {
    	self.materialUnit = "";
    	self.batchRequired = false;
    	self.currentBatchNumber = "";
    	self.suggestedAmounts = [];
    	materialId = null;

    	lt.notify();
    };

    this.changeCurrentEventType = function(typeId) {
        if (self.eventTypes == null) {
            return;
        }

        for (let i = 0; i < self.eventTypes.length; i++) {
        	var etype = self.eventTypes[i];
        	if (etype.Id === typeId) {
        		self.currentEventType = etype;
	            self.cancelEdit();
            	break;
            }
        }

        lt.notify();
    };

    this.setMaterialId = function(id) {
    	var info = app.materialHelper.getMaterialInfoById(id);
		
		if (info == null) {
		    self.batchRequired = false;
		} else {
			self.batchRequired = !info.AutomaticBatches;
			self.materialUnit = self.materialUnitOverride[id] || info.PreferredUnitSymbol;
		    materialId = info.MaterialId;
		}

        lt.notify();
    };

    this.findBatch = function(qry) {
        lt.api("/stockEvents/findBatch").query({ "materialId": materialId, "query": qry }).get(function(batch) {
            if (batch == null) {
                self.currentBatchNumber = "";
            } else {
            	self.currentBatchNumber = batch.BatchNumber;
                lt.api("/stockEvents/getSuggestedAmounts")
                    .query({ "eventTypeId": self.currentEventType.Id, "batchId": batch.Id })
					.silent()
                    .get(function(sugs) {
                    	//check if still valid!
                        self.suggestedAmounts = sugs.Suggestions;
                    });
            }
        });
    };

    this.save = function(matId, batchNumber, quantity, reason, successCallback) {
        lt.api("/stockEvents/saveEvent")
            .query({
                "eventTypeId": self.currentEventType.Id,
                "materialId": matId,
                "batchNumber": batchNumber,
                "quantity": quantity,
                "reason": reason,
                "unitSymbol": self.materialUnit
            })
            .get(successCallback);
    };

    app.urlBus.watch("setStockEvent", function (model) {
        self.materialUnitOverride[model.MaterialId] = model.UnitSymbol;
        self.materialUnit = model.UnitSymbol;
        self.changeCurrentEventType(model.EventTypeId);
        lt.notify();
    });
};

app.StockEvents.vm = app.StockEvents.vm || new app.StockEvents.ViewModel();