app.DistributorMeetings = app.DistributorMeetings || {
    VM: function() {
        const self = this;
        const crm = app.Distributors.vm;

        let customerId = null;
        let externalMeetingId = null;
        let externalChangeCallback = null;
        let externalCloseCallback = null;

        self.meetings = [];

        self.meetingStatusTypes = [];        
        self.meetingCategories = [];

        self.currentMeeting = null;
        self.editingMeeting = false;
        self.mailConversationDialogOpen = false;
        self.mailConversationDetail = [];
        self.mailConversationTitle = null;

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
                m.isMailConversation = !!m.MailConversationId;
                m.isReadOnly = m.isMailConversation;
                m.canOpenConversation = m.isMailConversation && !!(window.can && can.EmailConversationsFull);
                m.mailConversationLinkText = m.isMailConversation
                    ? `E-mailová konverzace (${m.MailConversationMessageCount || 0} zpráv)`
                    : null;
                m.Actions.forEach(a => a.meetingId = m.Id);
                m.textDirty = false;
            });
            
            self.meetings = meetings;

        }

        const receiveMeetingUpdate = (meetings) => {
            if (externalMeetingId === null) {
                receiveMeetings(meetings);
                return;
            }

            const meeting = meetings.find(m => m.Id === externalMeetingId);
            receiveMeetings(!!meeting ? [meeting] : []);

            if (!!meeting)
                self.openMeetingDetail(meeting.Id);

            if (!!externalChangeCallback)
                externalChangeCallback();
        };

        self.openExternalMeeting = (meeting, changeCallback, closeCallback) => {
            externalMeetingId = meeting.Id;
            externalChangeCallback = changeCallback;
            externalCloseCallback = closeCallback;
            customerId = meeting.CustomerId;

            receiveMeetings([JSON.parse(JSON.stringify(meeting))]);
            self.currentMeeting = self.meetings[0];
            self.editingMeeting = true;
        };

        self.closeExternalMeeting = () => {
            self.currentMeeting = null;
            self.editingMeeting = false;
            self.addingParticipant = false;
            self.meetings = [];
            externalMeetingId = null;
            externalChangeCallback = null;
            externalCloseCallback = null;
            customerId = null;
        };

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
            if (externalMeetingId !== null) {
                if (!!externalCloseCallback)
                    externalCloseCallback();

                return;
            }

            self.currentMeeting = null;
            self.editingMeeting = false;
            self.addingParticipant = false;
        };

        self.saveMeeting = () => {
            const meetingToSave = {
                ...self.currentMeeting,
                Participants: self.currentMeeting.Participants.map(participant => ({
                    UserId: participant.UserId,
                    UserName: participant.UserName
                }))
            };

            lt.api("/CrmMeetings/SaveMeeting")
                .body(meetingToSave)
                .post((meetings) => {
                    if (externalMeetingId !== null) {
                        if (!!externalChangeCallback)
                            externalChangeCallback();

                        if (!!externalCloseCallback)
                            externalCloseCallback();

                        return;
                    }

                    self.cancelMeetingEdit();
                    receiveMeetingUpdate(meetings);
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

                    const normalizedUserName = (userName || "").trim().toLocaleLowerCase();
                    const toAdd = all.find(p => p.UserName.toLocaleLowerCase() === normalizedUserName);

                    if (!!toAdd && !self.currentMeeting.Participants.some(p => p.UserId === toAdd.UserId)) {
                        self.currentMeeting.Participants = [
                            ...self.currentMeeting.Participants,
                            {
                                UserId: toAdd.UserId,
                                UserName: toAdd.UserName
                            }
                        ];
                    }

                    self.addingParticipant = false;
                    lt.notify();

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
                receiveMeetingUpdate(m);

                if (!!callback)
                    callback();
            });

        self.meetingTextChange = (meetingId, text) => {
            const meeting = self.meetings.find(m => m.Id === meetingId);

            if (!meeting || meeting.isReadOnly)
                return;

            meeting.textDirty = meeting.Text !== text;
        };

        self.saveMeetingText = (meetingId, text) => {
            const meeting = self.meetings.find(m => m.Id === meetingId);

            if (!meeting || meeting.isReadOnly)
                return;

            meeting.Text = text;

            lt.api("/CrmMeetings/saveMeeting")
                .body(meeting)
                .post(receiveMeetingUpdate);
        };

        self.editMeeting = (id) => {

            if (!!self.currentMeeting)
                return;

            const meeting = self.meetings.find(m => m.Id === id);

            if (!meeting || meeting.isReadOnly)
                return;

            self.currentMeeting = meeting;
            self.editingMeeting = true;

            self.openMeetingDetail(-1);
        };

        self.closeMailConversation = () => {
            self.mailConversationDialogOpen = false;
            self.mailConversationDetail = [];
            self.mailConversationTitle = null;
        };

        self.openMailConversation = (meetingId) => {
            const meeting = self.meetings.find(m => m.Id === meetingId);

            if (!meeting || !meeting.MailConversationId || !meeting.canOpenConversation)
                return;

            lt.api("/CrmMeetings/GetMailConversationDetail")
                .query({ "customerId": customerId, "conversationId": meeting.MailConversationId })
                .get(messages => {
                    self.mailConversationTitle = meeting.Title || "E-mailová konverzace";
                    self.mailConversationDetail = messages.map(m => ({
                        ...m,
                        Content: (m.Content || "").replace(/(\r?\n){3,}/g, "\n\n").trim()
                    }));
                    self.mailConversationDialogOpen = true;
                });
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
