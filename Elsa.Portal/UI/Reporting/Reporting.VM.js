var app = app || {};
app.Reporting = app.Reporting || {};
app.Reporting.VM = app.Reporting.VM ||
    function () {

        var self = this;
        this.reports = [];

        lt.api("/reporting/getReportTypes")
            .get(function (reps) {
                self.reports = [];

                for (var i = 0; i < reps.length; i++) {
                    var r = reps[i];
                    r.link = '/reporting/getreport?code=' + r.Code;
                    self.reports.push(r);

                    r.expanded = false;
                    r.canExpand = !!r.Note;
                }
            });
    };

app.Reporting.vm = app.Reporting.vm || new app.Reporting.VM();