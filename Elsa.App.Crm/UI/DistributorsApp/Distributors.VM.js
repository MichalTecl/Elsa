
var app = app || {};
app.Distributors = app.Distributors || {};
app.Distributors.VM = app.Distributors.VM || function(){
    const self = this;
        
    let metadataConsumersQueue = [];
    let __metadata = null;

    self.dataLoadBlocked = true;
    self.dataLoadRequested = false;

    self.data = [];

    self.allCustomerGroups = [];
    self.allTags = [];
    self.allSalesReps = [];    
    self.allExFilters = [];
    self.allTagGroups = [];

    self.page = 1;
    self.pageSize = 20;
    self.canReadMore = false;
    self.bulkTaggingOpen = false;

    const bulkTaggingTitleDefault = "Všem odpovídajícím kontaktům";
    const bulkTaggingTitleSelection = "Všem označeným kontaktům";

    self.bulkTaggingDynamicTitle = bulkTaggingTitleDefault;

    self.isDirty = false;

    self.sorterId = null;
        
    self.filter = {
        TextFilter: null,
        Tags: [],
        SalesRepresentativeId: null,
        CustomerGroupTypeId: null,
        IncludeDisabled: false,
        ExFilterGroups: [],
        TagGroups: []
    };

    self.isDetailOpen = false;
    self.detail = null;
    self.selectedAddress = null;

    self.tagPickerActive = false;

    self.isDetailPage = false;
    self.isGridPage = false;

    self.exFiltersExpanded = false;    
    self.editedExFilter = null;
    self.editingExFilter = false;

    self.tagFilterVisible = false;

    self.savedFilters = [];
                      
    const withFiltersUsageData = (callback) => {
        const filtersUsageStoredItemKey = "savedFiltersUsageHistory";
        const usageData = JSON.parse(window.localStorage.getItem(filtersUsageStoredItemKey) || '{}');;

        let toSave = false;

        const getter = (id) => usageData["f" + id];

        const setter = (id, value) => {
            toSave = true;
            usageData["f" + id] = value;
        };

        callback(getter, setter);

        if (toSave) {
            window.localStorage.setItem(filtersUsageStoredItemKey, JSON.stringify(usageData));
            sortSavedFilters();
        }
    }

    const markSavedFilterUsed = (filterId) => {
        withFiltersUsageData((_, setter) => {
            setter(filterId, Date.now())
        }); 

        self.savedFilters.forEach(f => f.isActive = f.Id === filterId);
    }

    const sortSavedFilters = () => {

        withFiltersUsageData((getter, _) => {

            let minUsg = Date.now();

            self.savedFilters.forEach((f, inx) => {
                let lastUse = getter(f.Id);

                if (!!lastUse) {

                    if (minUsg > lastUse) {
                        minUsg = lastUse;
                    }

                } else {
                    lastUse = minUsg - inx;
                }

                f.lastUsed = lastUse;
            });

            self.savedFilters.sort((a, b) => b.lastUsed - a.lastUsed);
        });       
    }

    const receiveSavedFilters = (f) => {
        self.savedFilters = f;

        sortSavedFilters();
    };

    self.loadSavedFilter = (id) => {

        markSavedFilterUsed(id);

        lt.api("/crmCustomFilters/loadFilter")
            .query({ "id": id })
            .get(f => applySavedFilter(f, id));
    };

    self.deleteSavedFilter = (id) => lt.api("/crmCustomFilters/deleteFilter").query({ id }).post(receiveSavedFilters);
        
    const applySavedFilter = (filter, filterId) => {
        self.filter = filter;
        self.updateSortingModel();
        self.onColumnsSelected(true);

        self.editedExFilter = null;
        self.editingExFilter = false;

        checkFiltersExpansion();

        let objId = (new Date()).getTime();

        self.filter.ExFilterGroups.forEach(g => {
            g.id = g.id || (objId++);
            g.Filters.forEach(f => f.id = f.id || (objId++));
        });

        visitAllExFilters(f => f.isValid = false);

        const whenAllFiltersValid = (f) => {

            if (!f.isValid)
                return;

            let invalidFound = false;

            visitAllExFilters(f => { if (!f.isValid) { invalidFound = true; } });

            if (!invalidFound) {
                self.search(filterId);
            }
        };
                
        visitAllExFilters(f => {
            validateExFilter(f, whenAllFiltersValid)
        }, () => {            
            self.search();
        });

        self.allTags.forEach(t => {
            t.isSelected = (self.filter.Tags.indexOf(t.Id) > -1);
        });

        applyTagGroupsSelection();

        self.allTagGroups.forEach(tg => {
            tg.isSelected = self.filter.TagGroups.indexOf(tg.Id) > -1;
        });
    };
    
    const loadSavedFilters = () => lt.api("/crmCustomFilters/getSavedFilters").silent().get(receiveSavedFilters);
    loadSavedFilters();
    
    self.saveCurrentFilter = (name) => {
                
        lt.api("/crmCustomFilters/saveFilter")
            .query({ "name": name })
            .body(self.filter)
            .post(filters => {

                const thisFilter = filters.find(f => f.Name === name);
                if (!!thisFilter) {
                    markSavedFilterUsed(thisFilter.Id);
                }

                receiveSavedFilters(filters);
            });
    };

    const visitAllExFilters = (visitor, noFiltersAction) => {

        let found = false;

        self.filter.ExFilterGroups.forEach(fg => fg.Filters
            .forEach(f => {
                visitor(f);
                found = true;
            }));

        if (!found && !!noFiltersAction)
            noFiltersAction();
    };
        
    self.importSavedFilter = (filter) => {
        applySavedFilter(filter);
    };
        
    self.setTagFilter = (text) => {

        self.tagFilterVisible = text !== null && text.trim().length > 0;

        var matcher = new TextMatcher(text);

        self.allTags.forEach(t => t.isHidden = !matcher.match(t.Name, true));            
    };

    self.changeFilterInverted = (filter, inverted) => {
        filter.Inverted = inverted;
    };

    self.toggleExFilterInverted = (filter) => {
        self.changeFilterInverted(!filter.Inverted);
    };

    self.onDataCheckChanged = () => {
        const hasSelection = !!(self.data.find(r => !!r.isChecked));
        self.bulkTaggingDynamicTitle = hasSelection ? bulkTaggingTitleSelection : bulkTaggingTitleDefault;

        if (hasSelection)
            self.bulkTaggingOpen = true;
    };
        
    self.openBulkTagging = () => {

        self.onDataCheckChanged();

        self.bulkTaggingOpen = true;
    };

    self.closeBulkTagging = () => {
        self.bulkTaggingOpen = false;
    }

    const countFilterResults = (callback) => {
        lt.api("/CrmDistributors/CountFilterResults")
            .body(self.filter)
            .post(callback);
    };

    const confirmBulkTagging = (model, callback) => {

        const userInteraction = (recordsCount) => {

            if (recordsCount === 0) {
                alert("Aktuální filtr vybírá 0 záznamů - akce nebude spuštěna");
                return;
            }

            let msg = "Prosím o kontrolu: " + recordsCount.toString() + " ";

            if (recordsCount === 1)
                msg += "velkoodběrateli bude ";
            else
                msg += "velkoodběratelům bude ";

            if (model.Set)
                msg += "nastaven štítek ";
            else
                msg += "odebrán štítek ";

            msg += model.tagName;

            msg += ". Spustit?";

            if (!window.confirm(msg))
                return;

            callback();
        }

        if (!!model.Filter) {
            countFilterResults(userInteraction);
        } else {
            userInteraction(model.CustomerIds.length);
        }
    };

    const obtainTagAssignmentData = (tagName, model, callback) => {
        self.withMetadata((md) => {
            const tag = md.CustomerTagTypes.find(t => t.Name.toLowerCase() === tagName.toLowerCase());

            if (!tag) {
                alert("Štítek " + tagName + " nenalezen.");
                return;
            }

            model.tagName = tag.Name;
            model.TagTypeId = tag.Id;

            let note = "";

            if (model.Set && tag.RequiresNote) {
                note = window.prompt("Štítek " + tag.Name + " vyžaduje poznámku:");
                if (!note)
                    return;
            }

            model.Note = note;

            callback(model);
        });
    };

    self.startBulkTagging = (tagName, set) => {

        const model = {
            "Set":set
        };

        const selectedContactIds = self.data.filter(d => !!d.isChecked).map(d => d.Id);
        if (selectedContactIds.length > 0) {
            model.CustomerIds = selectedContactIds;

        } else {
            model.Filter = self.filter;
        }

        obtainTagAssignmentData(tagName, model, (model) => confirmBulkTagging(model, () => {
            lt.api("/CrmDistributors/doBulkTagging")
                .body(model)
                .post((count) => {
                    self.search();
                    alert("Hotovo. Bylo změněno " + count + " záznamů velkoodběratelů.");
                });
        }));
    };

    const checkFiltersExpansion = () => {
        self.exFiltersExpanded = self.filter.ExFilterGroups.length > 0;
        lt.notify();
    };

    self.editFilter = (filterId) => {
                
        for (const g of self.filter.ExFilterGroups) {
            const filter = g.Filters.find(f => f.id === filterId);
            if (!!filter) {
                self.editedExFilter = filter;
                self.editingExFilter = true;

                if (self.editedExFilter.Parameters)
                    self.editedExFilter.Parameters.forEach(p => p.setValue = (v) => {
                        p.Value = v;
                    });

                return;
            }
        }  

        self.editedExFilter = null;
        self.editingExFilter = false;
     };

    self.addFilter = (groupId) => {
        let group = self.filter.ExFilterGroups.find(g => g.id === groupId);

        if (!group) {
            group = {
                "id": (new Date()).getTime(),
                "Filters":[]
            };
            self.filter.ExFilterGroups.push(group);
        }

        const filter = {
            "id": (new Date()).getTime(),
            "isValid": false,
            "error": "Nenastaveno",
            "IsInverted":false,
        };

        group.Filters.push(filter);
                
        self.editFilter(filter.id);
        self.changeCurrentExFilterType(null);

        checkFiltersExpansion();
    };

    self.changeCurrentExFilterType = (typeTitle) => {
        const templateFilter = self.allExFilters.find(f => f.Title === typeTitle) || self.allExFilters[0];

        const template = JSON.parse(JSON.stringify(templateFilter));

        Object.assign(self.editedExFilter, template);
        self.editedExFilter.IsInverted = false;

        self.editedExFilter.Parameters.forEach(p => p.setValue = p.setValue || ((v) => {
            p.Value = v;
        }));
    };

    self.detailTabs = [
        { "text": "Osoby", "control": "DistributorContactPersons"},
        { "text": "Historie", "control": "DistributorHistory"},
        { "text": "Poznámky", "control": "DistributorNotes" },
        { "text": "Objednávky", "control": "DistributorOrders" },   
        { "text": "Schůzky", "control": "DistributorMeetings" },       
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

            self.search();
        });
    };

    self.deleteExFilter = (filterId) => {

        if (!!self.editedExFilter && self.editedExFilter.id === filterId) {
            self.editedExFilter = null;
            self.editingExFilter = false;
        }

        for (let i = 0; i < self.filter.ExFilterGroups.length; i++) {
            const g = self.filter.ExFilterGroups[i];
            const filterIndex = g.Filters.findIndex(f => f.id === filterId);

            if (filterIndex !== -1) {                
                g.Filters.splice(filterIndex, 1);
                                
                if (g.Filters.length === 0) {
                    self.filter.ExFilterGroups.splice(i, 1);
                }

                break;
            }
        }

        checkFiltersExpansion();
        self.search();
    };

    const validateExFilter = (filter, callback) => {

        const unsetParam = filter.Parameters.find(p => p.Value === null || p.Value.length === 0);
        if (!!unsetParam) {
            filter.isValid = false;
            filter.error = "Nejsou nastaveny všechny parametry";

            if (!!callback)
                callback(filter);

            return;
        }

        const firstLetterLower = (t) => (!t) ? t : t.substring(0, 1).toLowerCase() + t.substring(1);

        lt.api("/CrmDistributors/validateFilter")
            .body(filter)
            .post((r) => {
                filter.isValid = r.IsValid;
                filter.error = r.ErrorMessage;
                filter.recordsCount = r.NumberOfRecords;
                filter.text = (filter.Inverted ? "Ne" : "") + firstLetterLower(r.FilterText.length < 100 ? r.FilterText : filter.Title);

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
            
    const filterUpdaters = {
        "direct": (k, v) => self.filter[k] = v,
        "text": (k, v) => self.filter.TextFilter = v,
        "salesRep": (k, v) => self.filter.SalesRepresentativeId = v,        
        "customerGroup": (k, v) => self.filter.CustomerGroupTypeId = v,
        "sorter": (k, v) => self.sorterId = v,
    };

    self.updateTagFilter = (tagId, value, postponeSearch) => {

        const indexOfTagId = self.filter.Tags.indexOf(tagId);

        if (!!value) {
            if (indexOfTagId === -1)
                self.filter.Tags.push(tagId);
        } else {
            if (indexOfTagId > -1)
                self.filter.Tags.splice(indexOfTagId, 1);
        }

        const tagModel = self.allTags.find(t => t.Id == tagId);
        tagModel.isSelected = value;

        if (!postponeSearch)
            self.search();
    };

    const applyTagGroupsSelection = () => {

        self.allTags.forEach(tag => {
            const activeGroupIndex = self.filter.TagGroups.indexOf(tag.GroupId);

            tag.isHidden = activeGroupIndex === -1;

            if (tag.isHidden) {
                filterUpdated = true;
                self.updateTagFilter(tag.Id, false, true);
            }
        });

        self.search();
    };

    self.updateTagGroupFilter = (groupId, value) => {
        const indexOfGroupId = self.filter.TagGroups.indexOf(groupId);

        if (!!value) {
            if (indexOfGroupId === -1)
                self.filter.TagGroups.push(groupId);
        } else {
            if (indexOfGroupId > -1)
                self.filter.TagGroups.splice(indexOfGroupId, 1);
        }

        applyTagGroupsSelection();
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

    self.search = (savedFilterId) => {
                
        console.log("Search initiated");

        self.page = 0;
        self.canReadMore = false;
        self.data = [];

        lt.notify();

        self.load();

        self.savedFilters.forEach(f => f.isActive = f.Id === savedFilterId);
    };

    const receiveMetadata = (m) => {
        self.allCustomerGroups = [{ ErpGroupName: "", Id: null }, ... m.CustomerGroupTypes];
        self.allTags = m.CustomerTagTypes;
        self.allSalesReps = [{ PublicName: "", Id: null }, ...m.SalesRepresentatives];
        self.allExFilters = m.DistributorFilters;
        self.allTagGroups = m.CustomerTagTypeGroups;

        self.allTagGroups.forEach(tg => {
            self.updateTagGroupFilter(tg.Id, false);
        });
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

            self.onDataCheckChanged();

        });
    };

    const load = (page) => {

        if (self.dataLoadBlocked) {
            console.log("data loading postponed");

            if (!self.dataLoadRequested) {
                self.dataLoadRequested = true;

                lt.api.usageManager.subscribeIdleHandler((idleMsecs) => {

                    if (idleMsecs > 250) {
                        self.dataLoadBlocked = false;
                        self.search();

                        return true;
                    }
                });
            }
            
            return;
        }

        self.dataLoadRequested = false;

        console.log("Loading - page=" + page);
        
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

        if (result) {
            self.saveDetail();
            self.isDirty = false;
        }
                
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
        self.getAllTags(qry, callback);        
    };

    self.getAllTags = (qry, callback) => {
        self.withMetadata((md) => callback(md.CustomerTagTypes.map(i => i.Name)));
    };

    self.getSavedFilterNames = (qry, callback) => {
        callback(self.savedFilters.map(f => f.Name));
    };

    self.closeDetail = () => {

        const closeToBack = () => {
            if (self.isDirty) {
                if (!confirm("Máte neuložené změny. Opravdu chcete pokračovat?"))
                    return;
            }
            resetDetail();
        };
                               
        if (self.isDetailPage) {
            try {
                window.close();

                setTimeout(() => {
                    
                    if (!window.closed) {
                        closeToBack();
                    }
                }, 200); 

            } catch {
                closeToBack();
            }
        } else {
            closeToBack();
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
        else {
            window.location.href = "/UI/DistributorsApp/DistributorsAppPage.html";
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



