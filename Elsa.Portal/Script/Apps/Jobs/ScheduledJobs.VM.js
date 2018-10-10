var app = app || {};
app.scheduledJobs = app.scheduledJobs || {};
app.scheduledJobs.ViewModel = app.scheduledJobs.ViewModel || function() {

    var self = this;

    self.jobs = null;

    var receive = function(jobs) {
        self.jobs = jobs;

        setTimeout(update, 5000);
    };

    var update = function() {
        lt.api("/scheduledJobs/getStatus").silent().get(receive);
    };
    
    update();

    self.launchJob = function(scheduleId) {

        lt.api("/scheduledJobs/launch").query({ "scheduleId" : scheduleId }).get(receive);

        update();
    };
};

app.scheduledJobs.vm = app.scheduledJobs.vm || new app.scheduledJobs.ViewModel();
