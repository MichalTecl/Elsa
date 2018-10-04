var app = app || {};
app.ordersPacking = app.ordersPacking || {};
app.ordersPacking.ViewModel = app.ordersPacking.ViewModel || function() {

    var self = this;

    self.currentOrder = null;

    self.searchOrder = function(qry) {
        
        if ((!qry) || (qry.length < 3)) {
            throw new Error("Musí být alespoň tři čísla");
        }



    };

};

app.ordersPacking.vm = app.ordersPacking.vm || new app.ordersPacking.ViewModel();

