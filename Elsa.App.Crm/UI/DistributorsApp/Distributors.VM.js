var app = app || {};
app.Distributors = app.Distributors || {};
app.Distributors.VM = app.Distributors.VM || function(){
    const self = this;

    let metadataConsumersQueue = [];
    let __metadata = null;

    self.data = [];

    self.page = 1;
    self.pageSize = 20;
    self.canReadMore = false;

    self.sortBy = "Name";
    self.sortAscending = true;

    self.filter = {
        TextFilter: null,
        Tags: [],
        SalesRepresentativeId: null,
        CustomerGroupTypeId: null,
        IncludeDisabled: false
    };

    self.load = () => {
        load(self.page + 1);
    };

    const withMetadata = (consumer) => {
        if (!!__metadata) {
            consumer(__metadata);
            return;
        }

        metadataConsumersQueue.push(consumer);

        if (metadataConsumersQueue.length === 1) {
            lt.api("/CrmMetadata/Get").get((md) => {
                __metadata = md;

                let c;
                while ((c = metadataConsumersQueue.pop()) !== undefined)
                    c(__metadata);
            });
        }
    };

    const receiveData = (data) => {
        
        const mapMd = (source, ids) => (ids || []).map(id => (source || []).find(s => s.Id === id));   

        withMetadata((metadata) => {

            data.forEach(d => {

                d.tags = mapMd(metadata.CustomerTagTypes, d.TagTypeIds).sort((a, b) => b.Priority - a.Priority);
                d.customerGroups = mapMd(metadata.CustomerGroupTypes, d.CustomerGroupTypeIds);
                d.salesRep = mapMd(metadata.SalesRepresentatives, d.SalesRepIds).find(x => true);

                const monthSymbols = getMonthSymbols(d.TrendModel.length);
                d.TrendModel.forEach((tm, inx) => {
                    tm.height = tm.Percent + '%';
                    tm.id = inx;
                    tm.symbol = monthSymbols[inx];
                });

                for (var dIndex = 0; dIndex < self.data.length; dIndex++) {
                    if (self.data[dIndex].Id === d.Id) {
                        self.data[dIndex] = d;
                        return;
                    }
                }

                self.data.push(d);
            });           
        });
    };

    const load = (page) => {

        lt.api("/CrmDistributors/getDistributors")
            .query({                
                "pageSize": self.pageSize || 10,
                "page": page || 1,
                "sortBy": self.sortBy,
                "ascending": self.sortAscending
            })
            .body(self.filter)
            .post((data) => {
                receiveData(data);

                self.page = page;
                self.canReadMore = data.length == self.pageSize;
            });
    };

    // just to initiate metadata loading
    withMetadata((m) => console.log("metadata recevied"));

    let monSymbCache = {};
    const getMonthSymbols = (n) => {

        if (!!monSymbCache[n])
            return monSymbCache[n];

        let result = [];
        let date = new Date();

        for (let i = n - 1; i >= 0; i--) {
            let d = new Date(date.getFullYear(), date.getMonth() - i, 1);
            let month = String(d.getMonth() + 1).padStart(2, '0'); // Zajištění dvoumístného formátu
            let year = d.getFullYear();
            result.push(`${month}/${year}`);
        }

        monSymbCache[n] = result;
        return result;
    }

    load(1);
};

app.Distributors.vm = app.Distributors.vm || new app.Distributors.VM();