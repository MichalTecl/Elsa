app.DistributorNotes = app.DistributorNotes || {
    VM: function () {
        const self = this;

        self.notes = [];

        window.queryWatch.watch("customerId", (customerId) => {
            const cid = (!!customerId) ? parseInt(customerId) : null;

            self.notes = [];

            if (!cid)
                return;

            lt.api("/CrmDistributors/getNotes")
                .query({ "customerId":cid })
                .get((notes) => {
                    self.notes = notes;
                });
        });

    }
};

app.DistributorNotes.vm = app.DistributorNotes.vm || new app.DistributorNotes.VM();

