var app = app || {};
app.CrmReporting = app.CrmReporting || {};
app.CrmReporting.VM = app.CrmReporting.VM ||

function () {

    const allSalesReps = "Všichni OZ";

    var self = this;

    var allDistributors = null;
    var salesRepresentatives = [];
    var selectedSalesRepName = null;

    self.selectedDistributorName = null;

    self.dateFrom = null;
    self.dateTo = null;

    self.reportLinks = [];

    var buildLinks = function () {
        buildPresets();
        var distributorId = null;
        if (!!allDistributors) {

            var di = allDistributors.reverse().find((d) => d.Name === self.selectedDistributorName);
            if (!!di)
                distributorId = di.Id;
        }

        var salesRepId = null;
        if (!!salesRepresentatives) {
            var sr = salesRepresentatives.find((sr) => sr.Name === selectedSalesRepName);

            if (!!sr)
                salesRepId = sr.Id;
        }

        var dtFrom = self.dateFrom.toISOString();
        var dtTo = self.dateTo.toISOString();


        var distributorReportLink = {
            "id":1,
            "title": "Přehled spolupráce s VO " + (self.selectedDistributorName || ""),
            "url": "/crmreporting/getDistributorReport?distributorId=" + distributorId,
            "hasError": !distributorId,
            "error": (!!distributorId) ? "" : "Není vybrán žádný VO partner"
        };

        var smtitle = "všech OZ";
        var smparam = "";

        if ((!!selectedSalesRepName) && (selectedSalesRepName !== allSalesReps)) {
            smtitle = "OZ " + selectedSalesRepName;
            smparam = "&salesRepId=" + salesRepId;
        }

        var salesRepReportLink = {
            "id":2,
            "title": "Přehled všech VO " + smtitle,
            "url": "/crmreporting/getSalesRepReport?dtFrom=" + dtFrom + '&dtTo=' + dtTo + smparam,           
            "hasError": false,
            "error": ""
        };

       self.reportLinks = [distributorReportLink, salesRepReportLink];       
    };

    self.getSalesReps = function (query, callback) {

        var srNames = (salesRepresentatives || []).map(i => i.Name);
        srNames.push(allSalesReps);
        callback(srNames);
    };

    self.getDistributors = function (query, callback) {

        var srep = (salesRepresentatives.filter(sr => sr.Name == selectedSalesRepName));
        var srId = null;

        if (!!srep && srep.length > 0)
            srId = srep[0].Id;

        var distNames = (allDistributors || []).filter(d => (!srId) || srId == d.SalesRepId).map(d => d.Name).filter((value, index, self) => {
            return self.indexOf(value) === index;
        });
        callback(distNames);
    };

    self.selectSalesRepByName = function (name) {

        if (selectedSalesRepName == name)
            return;

        selectedSalesRepName = name;
        self.selectedDistributorName = null;

        buildLinks();
        lt.notify();
    };

    self.selectDistributorByName = function (name) {
        self.selectedDistributorName = name;

        buildLinks();
        lt.notify();
    };

    self.selectPeriod = function (from, to) {
        self.dateFrom = from;
        self.dateTo = to;

        buildLinks();
        lt.notify();
    };

    lt.api("/CrmReporting/GetSalesReps").get(function (salesReps) {
        salesRepresentatives = salesReps;
        buildLinks();
    });

    lt.api("/CrmReporting/GetDistributors").get(function (distributors) {
        allDistributors = distributors;
        buildLinks();
    });

    var now = new Date();
    now.setMonth(now.getMonth() - 1);
    self.dateFrom = now;
    self.dateTo = new Date();

    self.periodPresets = [];

    var compareDates = function (a, b) {
        var da = (a || new Date()).toDateString();
        var db = (b || new Date()).toDateString();

        return da === db;
    };

    var preset = function (text, from, to) {

        if (from > new Date())
            return;

        var classesList = ["periodPresetButton"];

        const monthsDiff = (to.getFullYear() - from.getFullYear()) * 12 + (to.getMonth() - from.getMonth());
        classesList.push("periodMonths" + monthsDiff);
        
        if (to > new Date())
            classesList.push("periodPartiallyInFuture");

        if (self.periodPresets.length > 0 && self.periodPresets[self.periodPresets.length - 1].mlen !== monthsDiff)
            classesList.push('firstOfItsKind');

        if (compareDates(from, self.dateFrom) && compareDates(to, self.dateTo))
            classesList.push('selectedPerPreset');

        self.periodPresets.push({ "text": text, "from": from, "to": to, "mlen": monthsDiff, "cssClass": classesList.join(" ") });
    };
        
    var buildPresets = function () {

        self.periodPresets = [];

        const currentYear = new Date().getUTCFullYear();
        var rim = ['0', 'I', 'II', 'III', 'IV'];

        // Cely letosni rok
        const fromThisYear = new Date(Date.UTC(currentYear, 0, 1));
        const toThisYear = new Date(Date.UTC(currentYear, 11, 31));
        preset(currentYear.toString(), fromThisYear, toThisYear);

        // Cely minuly rok
        const fromLastYear = new Date(Date.UTC(currentYear - 1, 0, 1));
        const toLastYear = new Date(Date.UTC(currentYear - 1, 11, 31));
        preset((currentYear - 1).toString(), fromLastYear, toLastYear);

        // Vsechna ctvrtleti tohoto roku
        for (let quarter = 1; quarter <= 4; quarter++) {
            const fromQuarter = new Date(Date.UTC(currentYear, (quarter - 1) * 3, 1));
            const toQuarter = new Date(Date.UTC(currentYear, quarter * 3, 0));
            preset("Q " + rim[quarter] + '/' + currentYear.toString(), fromQuarter, toQuarter);
        }

        // Pololetí tohoto a minulého roku
        for (let year = currentYear - 1; year <= currentYear; year++) {
            for (let half = 1; half <= 2; half++) {
                const fromHalf = new Date(Date.UTC(year, (half - 1) * 6, 1));
                const toHalf = new Date(Date.UTC(year, half * 6, 0));
                preset(`${half}. pololetí ${year}`, fromHalf, toHalf);
            }
        }
    };

    buildPresets();
};

app.CrmReporting.vm = app.CrmReporting.vm || new app.CrmReporting.VM();