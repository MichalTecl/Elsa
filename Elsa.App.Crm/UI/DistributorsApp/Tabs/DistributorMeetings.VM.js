app.DistributorMeetings = app.DistributorMeetings || {
    VM: function() {
        const self = this;
        const crm = app.Distributors.vm;

        let customerId = null;

        self.meetings = [];

        self.meetingStatusTypes = [];        
        self.meetingCategories = [];

        self.currentMeeting = null;
        self.editingMeeting = false;

        self.addingParticipant = false;

        const receiveMeetings = (meetings) => {

            const now = new Date();
            const fdt = (datetimeValue) => {
                const datetime = new Date(datetimeValue);

                // Pokud je datum v minulosti, vrátí jen datum ve formátu dd.MM.yyyy
                if (datetime < now) {
                    const day = String(datetime.getDate()).padStart(2, '0');
                    const month = String(datetime.getMonth() + 1).padStart(2, '0');
                    const year = datetime.getFullYear();
                    return `${day}.${month}.${year}`; // Formát: dd.MM.yyyy
                } else {
                    // Pokud je datum v budoucnosti, vrátí datum a čas ve formátu dd.MM.yyyy HH:mm
                    const day = String(datetime.getDate()).padStart(2, '0');
                    const month = String(datetime.getMonth() + 1).padStart(2, '0');
                    const year = datetime.getFullYear();
                    const hours = String(datetime.getHours()).padStart(2, '0');
                    const minutes = String(datetime.getMinutes()).padStart(2, '0');
                    return `${day}.${month}.${year} ${hours}:${minutes}`; // Formát: dd.MM.yyyy HH:mm
                }
            };
            

            meetings.forEach(m => {
                const startDate = new Date(m.StartDt);
                m.previewDt = fdt(startDate);       

                m.isOpen = false;
                m.Actions.forEach(a => a.meetingId = m.Id);
                m.textDirty = false;
            });
            
            self.meetings = meetings;

        }

        self.newMeeting = (categoryId) => {

            lt.api("/CrmMeetings/GetMeetingTemplate")
                .query({ "customerId": customerId, "meetingCategoryId": categoryId })
                .post(m => {
                    self.editingMeeting = true;
                    self.currentMeeting = m;
                });

        };

        self.removeParticipant = (userId) => {
            self.currentMeeting.Participants = self.currentMeeting.Participants.filter(p => p.UserId !== userId);
        }

        self.cancelMeetingEdit = () => {
            self.currentMeeting = null;
            self.editingMeeting = false;
        };

        self.saveMeeting = () => {
            lt.api("/CrmMeetings/SaveMeeting")
                .body(self.currentMeeting)
                .post((meetings) => {
                    self.cancelMeetingEdit();
                    receiveMeetings(meetings);
                });
        };

        self.setParticipantAdd = () => {
            self.addingParticipant = true;
        };

        self.getParticipantSelection = (qry, callback) => {
            lt.api("/crmMeetings/getAllParticipants")
                .get(all => {
                    const toAdd = all.filter(p => {
                        const attached = self.currentMeeting.Participants.find(ap => ap.UserId === p.UserId);
                        return !attached;
                    }).map(p => p.UserName);
                    callback(toAdd);

                });
        };

        self.addParticipant = (userName) => {
            lt.api("/crmMeetings/getAllParticipants")
                .get(all => {

                    const toAdd = all.find(p => p.UserName === userName);

                    if(!!toAdd)
                        self.currentMeeting.Participants.push(toAdd);

                    self.addingParticipant = false;

                });
        };

        self.updateCurrentMeeting = (property, value) => {
            self.currentMeeting[property] = value;

            if (property === "StartDt") {
                const startDate = new Date(value);

                const date = new Date(startDate.getTime() + self.currentMeeting.ExpectedDurationMinutes * 60000);
                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0'); 
                const day = String(date.getDate()).padStart(2, '0');
                const hours = String(date.getHours()).padStart(2, '0');
                const minutes = String(date.getMinutes()).padStart(2, '0');

                self.currentMeeting.EndDt =`${year}-${month}-${day}T${hours}:${minutes}`;
            }

        };

        self.openMeetingDetail = (id) => {

            const nowOpen = self.meetings.find(m => m.isOpen);

            if (!!nowOpen) {
                nowOpen.isOpen = false;

                if (nowOpen.Id === id)
                    return;
            }
            
            self.meetings.forEach(m => m.isOpen = m.Id === id);
        };

        self.setMeetingStatus = (meetingId, statusTypeId, callback) => lt
            .api("/CrmMeetings/setMeetingStatus")
            .query({ "meetingId": meetingId, "statusTypeId": statusTypeId })
            .post(m => {
                receiveMeetings(m);

                if (!!callback)
                    callback();
            });

        self.meetingTextChange = (meetingId, text) => {
            const meeting = self.meetings.find(m => m.Id === meetingId);

            meeting.textDirty = meeting.Text !== text;
        };

        self.saveMeetingText = (meetingId, text) => {
            const meeting = self.meetings.find(m => m.Id === meetingId);
            meeting.Text = text;

            lt.api("/CrmMeetings/saveMeeting")
                .body(meeting)
                .post(receiveMeetings);
        };

        self.editMeeting = (id) => {

            if (!!self.currentMeeting)
                return;

            const meeting = self.meetings.find(m => m.Id === id);

            self.currentMeeting = meeting;
            self.editingMeeting = true;

            self.openMeetingDetail(-1);
        };

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
