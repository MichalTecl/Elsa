var app = app || {};
app.scheduledJobs = app.scheduledJobs || {};
app.scheduledJobs.ViewModel = app.scheduledJobs.ViewModel || function() {

    var self = this;

    self.jobs = null;
    self.jobsHeartBeat = true;

    var receive = function(jobs) {
        self.jobs = jobs;

        setTimeout(update, 5000);
    };

    var update = function() {
        lt.api("/scheduledJobs/getStatus").silent().get(receive);
    };
    
    update();

    var receiveJobsHb = function (val) {
        self.jobsHeartBeat = val;
        console.log("JobsHeartBeat=" + val);
        var interval = 5 * 60 * 1000;

        if (!val) {
            interval = 5000;
        }

        setTimeout(checkJobsHb, interval);
    };

    var checkJobsHb = function () {        
        lt.api("/scheduledJobs/checkJobsHeartBeat").silent().get(receiveJobsHb);
    };

    checkJobsHb();

    self.launchJob = function(scheduleId) {

        lt.api("/scheduledJobs/launch").query({ "scheduleId" : scheduleId }).get(receive);

        update();        
    };
};

app.scheduledJobs.vm = app.scheduledJobs.vm || new app.scheduledJobs.ViewModel();
