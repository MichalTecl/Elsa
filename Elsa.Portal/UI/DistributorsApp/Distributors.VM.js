
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
    self.allExFilters = [];

    self.page = 1;
    self.pageSize = 20;
    self.canReadMore = false;

    self.isDirty = false;

    self.sorterId = null;

    self.filter = {
        TextFilter: null,
        Tags: [],
        SalesRepresentativeId: null,
        CustomerGroupTypeId: null,
        IncludeDisabled: false
    };

    self.isDetailOpen = false;
    self.detail = null;
    self.selectedAddress = null;

    self.tagPickerActive = false;

    self.isDetailPage = false;
    self.isGridPage = false;

    self.exFiltersExpanded = false;
    self.exFilterGroups = [];
    self.editedExFilter = null;
    self.editingExFilter = false;

    self.editFilter = (filterId) => {
                
        for (const g of self.exFilterGroups) {
            const filter = g.filters.find(f => f.id === filterId);
            if (!!filter) {
                self.editedExFilter = filter;
                self.editingExFilter = true;
                return;
            }
        }  

        self.editedExFilter = null;
        self.editingExFilter = false;
    };

    self.addFilter = (groupId) => {
        let group = self.exFilterGroups.find(g => g.id === groupId);

        if (!group) {
            group = {
                "id": (new Date()).getTime(),
                "filters":[]
            };
            self.exFilterGroups.push(group);
        }

        const filter = {
            "id": (new Date()).getTime(),
            "isValid":false
        };

        group.filters.push(filter);
                
        self.editFilter(filter.id);
        self.changeCurrentExFilterType(null);
    };

    self.changeCurrentExFilterType = (typeTitle) => {
        const template = self.allExFilters.find(f => f.Title === typeTitle) || self.allExFilters[0];
        
        Object.assign(self.editedExFilter, template);

        self.editedExFilter.Parameters.forEach(p => p.setValue = (v) => p.Value = v);
    };

    self.detailTabs = [
        { "text": "Objednávky", "control": "DistributorOrders" },   
        { "text": "Schůzky", "control": "DistributorMeetings" },
        { "text": "Poznámky", "control": "DistributorNotes" },
        
    ];

    self.currentTabContentControl = null;

    self.toggleExFiltersExpansion = () => {
        self.exFiltersExpanded = !self.exFiltersExpanded;
    };

    self.closeFilter = () => {
        validateExFilter(self.editedExFilter, (f) => {
            if (!f.isValid) {
                if (!window.confirm("Nastavení filtru je chybné (\"" + f.error + "\"). Opravdu jej chcete zavřít?"))
                    return;
            }

            self.editedExFilter = null;
            self.editingExFilter = false;            
        });
    };

    self.deleteExFilter = (filterId) => {

        if (!!self.editedExFilter && self.editedExFilter.id === filterId) {
            self.editedExFilter = null;
            self.editingExFilter = false;
        }

        for (let i = 0; i < self.exFilterGroups.length; i++) {
            const g = self.exFilterGroups[i];
            const filterIndex = g.filters.findIndex(f => f.id === filterId);

            if (filterIndex !== -1) {                
                g.filters.splice(filterIndex, 1);
                                
                if (g.filters.length === 0) {
                    self.exFilterGroups.splice(i, 1);
                }

                return;
            }
        }
    };

    const validateExFilter = (filter, callback) => {

        lt.api("/CrmDistributors/validateFilter")
            .body(filter)
            .post((r) => {
                filter.isValid = r.IsValid;
                filter.error = r.ErrorMessage;
                filter.recordsCount = r.NumberOfRecords;

                if(!!callback)
                    callback(filter);
            });
    };

    self.activateTab = (text) => {

        self.currentTabContentControl = null;

        self.detailTabs.forEach(t => {
            const thisOne = t.text === text;
            t.isActive = thisOne ? 1 : 0;

            if (thisOne) {

                const controlUrl = t.control.indexOf('/') > -1 ? t.control : "/UI/DistributorsApp/Tabs/" + t.control + '.html';

                self.currentTabContentControl = controlUrl;
            }
        });
    };

    self.activateTab(self.detailTabs[0].text);

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

        self.search();
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
        self.allExFilters = m.DistributorFilters;        
    };

    self.withMetadata = (consumer) => {
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

        self.withMetadata((metadata) => {

            data.forEach(d => {

                d.tags = mapMd(metadata.CustomerTagTypes, d.TagTypeIds).sort((a, b) => b.Priority - a.Priority);
                d.customerGroups = mapMd(metadata.CustomerGroupTypes, d.CustomerGroupTypeIds);
                d.salesRep = mapMd(metadata.SalesRepresentatives, d.SalesRepIds).find(x => true);
                d.detailLink = "?customerId=" + d.Id;

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
        
    self.withMetadata((m) => receiveMetadata(m));

    let monSymbCache = {};
    const getMonthSymbols = (n) => {

        if (!!monSymbCache[n])
            return monSymbCache[n];

        let result = [];
        let date = new Date();

        for (let i = n - 1; i >= 0; i--) {
            let d = new Date(date.getFullYear(), date.getMonth() - i, 1);
            let month = String(d.getMonth() + 1).padStart(2, '0'); 
            let year = d.getFullYear();
            result.push(`${month}/${year}`);
        }

        monSymbCache[n] = result;
        return result;
    }

    /** Detail **********************/

    self.updateDetail = (key, value) => {
        if (self.detail[key] === value)
            return;

        self.detail[key] = value;
        self.detail.isDirty = true;

        checkDirty();
    };

    self.saveDetail = () => {

        const model = {
            "CustomerId": self.detail.Id,
            "HasStore": self.detail.HasStore,
            "HasEshop": self.detail.HasEshop,
            "AddedTags": self.detail.addedTags,
            "RemovedTags": self.detail.removedTags,
            "ChangedAddresses": [...self.detail.addresses.filter(a => a.isDeleted || a.isDirty), ...self.detail.deletedAddresses || []],
        };

        lt.api("/CrmDistributors/save").body(model).post(() => {
            self.isDirty = false;
            self.closeDetail();
        });
    };

    const receiveAddresses = (addresses) => {

        if (!self.detail)
            return;

        addresses.forEach(a => a.IsNotStore = !a.IsStore);

        self.detail.addresses = addresses;
        self.selectedAddress = self.detail.addresses[0];
    };

    const loadAddresses = () => lt.api("/CrmDistributors/getAddresses").query({ "customerId": self.detail.Id }).get(receiveAddresses);

    
    const receiveDetail = (detail) => {
        self.detail = detail;
        self.isDetailOpen = true;

        loadAddresses();

        self.withMetadata((md) => {

            if (self.detail.SalesRepIds.length > 0)
                self.detail.salesRepName = (md.SalesRepresentatives.find(s => s.Id === self.detail.SalesRepIds[0]) || {}).PublicName;

            self.detail.customerGroups = self.detail.CustomerGroupTypeIds.map(i => md.CustomerGroupTypes.find(g => g.Id === i));

            self.detail.deletedAddresses = [];

            const manTags = [];
            const sysTags = [];

            self.detail
                .TagTypeIds.map(i => md.CustomerTagTypes.find(tt => tt.Id === i))
                .forEach(t => {
                    if (t.CanBeAssignedManually) {
                        manTags.push(t);
                    } else {
                        sysTags.push(t);
                    }
                });

            self.detail.manTags = manTags.sort((a,b) => a.Priority - b.Priority);
            self.detail.sysTags = sysTags.sort((a, b) => a.Priority - b.Priority);
            self.detail.addedTags = [];
            self.detail.removedTags = [];
            self.detail.reportUrl = "/crmReporting/GetDistributorReport?distributorId=" + self.detail.Id;

        });
    };

    const loadDetail = (customerId) => {

        if (!customerId) {

            if (self.isDetailOpen) {
                resetDetail();
                lt.notify();
            }

            return;
        }

        lt.api("/CrmDistributors/getDetail").query({ "customerId": customerId }).get(receiveDetail);
    }

    const checkDirty = () => {

        let result = false;
        const search = (source) => {

            if (!!result || (!source))
                return;

            if (!!source.find(i => i.isDirty))
                result = true;
        };

        if (self.detail) {

            if ((self.detail.deletedAddresses.length > 0)
                || (self.detail.addedTags.length > 0)
                || (self.detail.removedTags.length > 0))
                result = true;

            search([self.detail]);
            search(self.detail.addresses);
        }

        self.isDirty = result;
        lt.notify();
    };

    self.selectAddress = (addressName) => {
        self.selectedAddress = self.detail.addresses.find(a => a.AddressName === addressName) || self.detail.addresses.find(a => true);
        lt.notify();
    };

    self.updateAddress = (key, value) => {
        if ((!self.selectedAddress) || (self.selectAddress[key] === value))
            return;

        self.selectedAddress.isDirty = true;       
        self.selectedAddress[key] = value;

        checkDirty();
    };

    self.deleteAddress = () => {

        let addrIndex = self.detail.addresses.indexOf(self.selectedAddress);

        self.detail.addresses.splice(addrIndex, 1);

        if (!self.selectedAddress.isNew) {
            self.selectedAddress.isDeleted = true;
            self.detail.deletedAddresses = self.detail.deletedAddresses || [];
            self.detail.deletedAddresses.push(self.selectedAddress);
        }

        checkDirty();

        self.selectAddress(null);
    };

    self.addAddress = () => {
        
        let name = "";
        do {
            name = prompt("Název prodejny", "Prodejna " + name);

            if (!name)
                return;

            if (name.indexOf("Prodejna ") === -1) {
                alert("Název adresy prodejny musí začínat řetězcem 'Prodejna '");
                continue;
            }

            var existing = self.detail.addresses.find(a => a.AddressName.toLowerCase().trim() === name.toLowerCase().trim());
            if (!!existing) {
                alert("Adresa s tímto názvem již existuje");
                continue;
            }

            break;

        } while (true);

        self.detail.addresses.push({
            "AddressName": name,
            "StoreName": name.replace(/^Prodejna /, ''),
            "IsStore": true,
            "isDirty": true,
            "isNew": true,
            "Address": "",
            "City": "",
            "Lat": "",
            "Lon": "",
            "Gps": "",
            "Phone": "",
            "Email": "",
            "Www": ""            
        });

        self.selectAddress(name);

        checkDirty();
    };

    const setTagIdInArray = (array, tagId, add) => {

        const tInx = array.indexOf(tagId);
        if ((tInx > -1 && add) || (tInx === -1 && !add))
            return;

        if (add)
            array.push(tagId);
        else
            array.splice(tInx, 1);
    };

    self.removeTag = (id) => {
        let tag = self.detail.manTags.find(t => t.Id === id);
        if (!tag)
            throw new Error("Invalid tag id");

        setTagIdInArray(self.detail.removedTags, id, true);
        setTagIdInArray(self.detail.addedTags, id, false);
                
        let tagIndex = self.detail.manTags.indexOf(tag);
        self.detail.manTags.splice(tagIndex, 1);     

        self.detail.attachableTags = null;

        checkDirty();
    };

    self.addTag = (name) => {

        self.getAttachableTags(name, (allowedTagNames) => {
            if (allowedTagNames.indexOf(name) === -1)
                return;

            self.withMetadata((md) => {

                const toAdd = md.CustomerTagTypes.find(t => t.Name === name);
                const id = toAdd.Id;

                setTagIdInArray(self.detail.removedTags, id, false);
                setTagIdInArray(self.detail.addedTags, id, true);

                self.detail.manTags.push(toAdd);

                self.detail.attachableTags = null;
                self.tagPickerActive = false;

                checkDirty();

                lt.notify();
            });
        });                       
    };

    self.getAttachableTags = (qry, callback) => {

        self.withMetadata((md) =>
            callback(self.detail.attachableTags || (self.detail.attachableTags = md.CustomerTagTypes.filter(tag => {

                if (!tag.CanBeAssignedManually)
                    return false;

                const existing = self.detail.manTags.find(et => et.Id === tag.Id);
                if (!!existing)
                    return false;

                return true;
            }).sort((a, b) => b.Priority - a.Priority).map(i => i.Name))));
    };

    self.closeDetail = () => {
        if (self.isDetailPage) {
            window.close(); 
        } else {

            if (self.isDirty) {
                if (!confirm("Máte neuložené změny. Opravdu chcete pokračovat?"))
                    return;
            }
            resetDetail();
        }
    };

    window.addEventListener("beforeunload", (event) => {
        if (self.isDirty) {
            event.preventDefault();
            event.returnValue = "";
        }
    });

    window.addEventListener("popstate", (event) => {
        if (self.isDirty) {
            const userConfirmed = confirm("Máte neuložené změny. Opravdu chcete pokračovat?");
            if (!userConfirmed) {
                history.pushState(null, "", window.location.href); 
            }
        }
    });
        
    history.pushState(null, "", window.location.href);


    /** Initialization **************************************************/

    

    lt.api("/CrmDistributors/getSortingTypes").get(st => self.allSorters = st);

    const resetDetail = () => {
        self.isDetailOpen = false;
        self.detail = null;
        self.selectedAddress = null;

        self.tagPickerActive = false;

        self.isDetailPage = false;

        checkDirty();

        const url = new URL(window.location.href);
        const hashParams = new URLSearchParams(url.hash.substring(1)); // Získání parametrů z hash

        if (hashParams.has("customerId")) {
            hashParams.delete("customerId"); // Odebrání `customerId`
            const newHash = hashParams.toString();

            // Aktualizace URL bez reloadu stránky
            history.replaceState(null, "", newHash ? `#${newHash}` : window.location.pathname);
        }
    };


    self.withCustomerId = (callback) => {
        window.queryWatch.watch("customerId", (strCustomerId) => {
            let customerId = null;

            if (strCustomerId && /^\d+$/.test(strCustomerId)) {
                customerId = parseInt(strCustomerId, 10);
            }

            callback(customerId);
        });
    };

    self.withCustomerId((customerId) => {

        if (customerId) {
            if (!self.isGridPage)
                self.isDetailPage = true;

            loadDetail(customerId);

        }
        else {
            self.isGridPage = true;
            loadDetail(null);
            load(1);
        }

    });    
};

app.Distributors.vm = app.Distributors.vm || new app.Distributors.VM();



