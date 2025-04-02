app.DistributorMeetings = app.DistributorMeetings || {
    VM: function() {
        const self = this;
        const crm = app.Distributors.vm;

        let customerId = null;

        self.meetings = [];

        self.meetingStatusTypes = [];        
        self.meetingCategories = [];

        const receiveMeetings = (meetings) => self.meetings = meetings;

        
        crm.withMetadata((md) => {
            self.meetingStatusTypes = md.MeetingStatusTypes;
            self.meetingCategories = md.MeetingCategories;      

            crm.withCustomerId((cid) => {
                customerId = cid;

                self.meetings = [];

                if (!customerId)
                    return;

                lt.api("/CrmMeetings/getMeetings")
                    .query({ "customerId": customerId })
                    .get(receiveMeetings);

            });        
        });
    }
};

app.DistributorMeetings.vm = app.DistributorMeetings.vm || new app.DistributorMeetings.VM();
