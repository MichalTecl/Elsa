var app = app || {};
app.Distributors = app.Distributors || {};
app.Distributors.VM = app.Distributors.VM || function(){
    const self = this;

    let metadataConsumersQueue = [];
    let __metadata = null;

    self.data = [];

    self.allCustomerGroups = [];
    self.allTags = [];
    self.allSalesReps = [];
    self.allSorters = [];

    self.page = 1;
    self.pageSize = 20;
    self.canReadMore = false;

    self.sorterId = null;

    self.filter = {
        TextFilter: null,
        Tags: [],
        SalesRepresentativeId: null,
        CustomerGroupTypeId: null,
        IncludeDisabled: false
    };

    const updateFilterArray = (array, value, shouldBeIncluded) => {

            var inx = array.indexOf(value);

            if ((inx > -1) == shouldBeIncluded)
                return;

        if (shouldBeIncluded)
                array.push(value);
            else
                array.splice(inx, 1);
    };

    const filterUpdaters = {
        "direct": (k, v) => self.filter[k] = v,
        "text": (k, v) => self.filter.TextFilter = v,
        "salesRep": (k, v) => self.filter.SalesRepresentativeId = v,
        "tag": (k, v) => updateFilterArray(self.filter.Tags, k, v),
        "customerGroup": (k, v) => self.filter.CustomerGroupTypeId = v,
        "sorter": (k, v) => self.sorterId = v,
    };

    self.load = () => {
        load(self.page + 1);
    };

    self.updateFilter = (type, key, value) => {

        updater = filterUpdaters[type];
        if (!updater)
            throw new Error("Undefined filter type " + type);

        updater(key, value);

        console.log(self.filter);
    };

    self.search = () => {
        self.page = 0;
        self.canReadMore = false;
        self.data = [];

        lt.notify();

        self.load();
    };

    const receiveMetadata = (m) => {
        self.allCustomerGroups = [{ ErpGroupName: "", Id: null }, ... m.CustomerGroupTypes];
        self.allTags = m.CustomerTagTypes;
        self.allSalesReps = [{ PublicName: "", Id: null }, ...m.SalesRepresentatives];
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
                "sorterId": self.sorterId
            })
            .body(self.filter)
            .post((data) => {
                receiveData(data);

                self.page = page;
                self.canReadMore = data.length == self.pageSize;
            });
    };
        
    withMetadata((m) => receiveMetadata(m));

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

    lt.api("/CrmDistributors/getSortingTypes").get(st => self.allSorters = st);
};

app.Distributors.vm = app.Distributors.vm || new app.Distributors.VM();