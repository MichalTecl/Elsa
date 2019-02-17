var app = app || {};
app.MaterialLevels = app.MaterialLevels || {};
app.MaterialLevels.ViewModel = app.MaterialLevels.ViewModel || function() {

    const self = this;

    this.currentWarning = null;

    this.levels = [];

    var loadWarning = function() {

        lt.api("/materialLevel/GetCurrentWarning").silent().get(function (warn) {

            if (!warn.HasWarning) {
                self.currentWarning = null;
            } else {
                self.currentWarning = warn.Warning;
            }
            
            setTimeout(loadWarning, (60 * 1000) + Math.floor(Math.random() * 30000));
        });

    };
    
    setTimeout(loadWarning, 150);

    this.loadLevels = function () {

        lt.api("/materialLevel/getLevels").get(function(levels) {

            self.levels = [];

            for (let i = 0; i < levels.length; i++) {
                var level = levels[i];
                level.batchesLink = '/UI/Pages/Inventory/WhEvents.html?#findBatches={"materialName":"' + level.MaterialName + '"}';
                level.actualValue = level.ActualValue + " " + level.Unit;
                level.isWarning = false;
                if (level.HasThreshold) {
                    level.minValue = level.MinValue + " " + level.Unit;
                    level.perc = level.PercentLevel + "%";
                    level.isWarning = level.ActualValue < level.MinValue;
                }
                self.levels.push(level);
            }
        });
    };
};


app.MaterialLevels.vm = app.MaterialLevels.vm || new app.MaterialLevels.ViewModel();