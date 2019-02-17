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

    var materialId = null;

    var loadEventTypes = function() {
        lt.api("/stockEvents/getEventTypes").get(function(types) {
        	self.eventTypes = types;
            if (self.currentEventType == null && self.eventTypes.length > 0) {
                self.currentEventType = self.eventTypes[0];
            }
        });
    };

    setTimeout(loadEventTypes, 0);

    this.changeCurrentEventType = function(typeId) {
        if (self.eventTypes == null) {
            return;
        }

        for (let i = 0; i < self.eventTypes.length; i++) {
        	var etype = self.eventTypes[i];
        	if (etype.Id === typeId) {
	            self.materialUnit = "";
	            self.batchRequired = false;
	            self.currentEventType = etype;
	            self.currentBatchNumber = "";
	            self.suggestedAmounts = [];
	            materialId = null;
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
			self.materialUnit = info.PreferredUnitSymbol;
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
};

app.StockEvents.vm = app.StockEvents.vm || new app.StockEvents.ViewModel();