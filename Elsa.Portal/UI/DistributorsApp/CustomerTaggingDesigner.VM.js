app = app || {};
app.CustomerTaggingDesigner = app.CustomerTaggingDesigner || {
    "VM": function () {

        let allCssClasses = [];

        const self = this;
        self.binder = 1;
        self.treeLevel = 0;

        let tagData = [];

        self.tags = [];

        const call = (action) => {
            let rq = lt.api("/CustomerTagDesigner/" + action);

            if (!!self.activeGroup)
                rq = rq.query({ "groupId": self.activeGroup.Id });

            return rq;
        };

        const receiveTags = (tags) => {
            tagData = tags;

            updateTagList(null, self.tags, tagData.filter(t => t.IsRoot).map(t => t.Id));            
        };

        const loadTags = () => call("LoadTags").get(receiveTags);

        const updateTagList = (parentTag, list, ids) => {

            // 1. remove tags not contained in ids
            for (let i = list.length - 1; i >= 0; i--) {
                if (ids.indexOf(list[i].id) === -1) {
                    list.splice(i, 1);
                }
            }

            // 2. add/update tags by new ids
            ids.forEach(id => {
                const tagModel = getTagModel(parentTag, list, id);
                tagModel.update();
            });
        };

        const getTagModel = (parentTag, source, id, placeToBeginning) => {

            let tagModel = (id == null) ? null : source.find(t => t.id === id);

            if (!tagModel) {
                tagModel =
                {
                    "id": id,
                    "isRoot": true,
                    "parentTagInstanceId": parentTag ? parentTag.instanceId : null,
                    "instanceId": crypto.randomUUID(),
                    "tags": [],
                    "tagsIds": [],
                    "isOpen": false,
                    "canOpen": false,   
                    "isEditing": false,                    
                    "firstParentId": null,
                    "childPickerOpen": false,
                    "treeLevel": parentTag ? parentTag.treeLevel + 1 : 1
                };

                tagModel.update = () => {

                    const tagRecord = tagData.find(t => t.Id === tagModel.id);
                    if (!tagRecord) {
                        throw new Error("No source data for tagId=" + tagModel.id);
                    }

                    tagModel.name = tagRecord.Name;
                    tagModel.cssClass = tagRecord.CssClass;
                    tagModel.description = tagRecord.Description;
                    tagModel.isRoot = !tagModel.parentTagInstanceId;
                    tagModel.tagsIds = tagRecord.TransitionsTo;
                    tagModel.daysToWarning = tagRecord.DaysToWarning;
                    tagModel.hasDaysToWarning = tagModel.daysToWarning > 0;

                    if (tagModel.isOpen) {
                        updateTagList(tagModel, tagModel.tags, tagModel.tagsIds);
                    } else {
                        tagModel.tags = [];
                    }

                    tagModel.canOpen = (!!tagModel.id); // !id => means it is not saved, so we cannot add tags
                                        
                    return true;
                };

                tagModel.open = () => {
                    tagModel.isOpen = true;
                    tagModel.update();
                };

                tagModel.close = () => {
                    tagModel.isOpen = false;
                };

                tagModel.edit = () => {
                    tagModel.isEditing = true;
                    tagModel.isOpen = false;
                };

                tagModel.save = () => {
                                       
                    call("saveTag")
                        .query({ "parentTagId": tagModel.firstParentId })
                        .body({
                            "Id": tagModel.id,
                            "Name": tagModel.name,
                            "CssClass": tagModel.cssClass,
                            "Description": tagModel.description
                        })
                        .post((tags) => {
                            tagModel.cancelEdit();
                            receiveTags(tags);
                        });
                };

                tagModel.updateName = (v) => tagModel.name = v;
                tagModel.updateDaysToWarning = (hasDtw, strValue) => {
                    let value = 0;
                    if (hasDtw) {
                        value = parseInt(strValue);
                        if (isNaN(value) || value <= 0) {
                            alert("Neplatná hodnota '" + strValue + "'");
                            return;
                        }
                    }

                    tagModel.daysToWarning = value;
                    tagModel.hasDaysToWarning = tagModel.daysToWarning > 0;
                };

                tagModel.changeCssClass = () => {
                    let cinx = (allCssClasses.indexOf(tagModel.cssClass) + 1) % allCssClasses.length;
                    tagModel.cssClass = allCssClasses[cinx];
                };

                tagModel.openChildPicker = () => tagModel.childPickerOpen = true;
                tagModel.closeChildPicker = () => tagModel.childPickerOpen = false;

                tagModel.createChildTag = () => self.createTag(tagModel);

                tagModel.attachChild = (childTagName) => {

                    const tag = tagData.find(t => t.Name === childTagName);
                    if (!tag)
                        return;

                    call("SetTransition").query({ "sourceId": tagModel.id, "targetId": tag.Id }).post(receiveTags);

                    tagModel.closeChildPicker();
                };

                tagModel.detachFromParent = () => {
                    if (!tagModel.parentTagInstanceId)
                        return;

                    const parentTag = findTagUiModel(t => t.instanceId === tagModel.parentTagInstanceId);

                    call("RemoveTransition").query({ "sourceId": parentTag.id, "targetId": tagModel.id }).post(receiveTags);
                };

                tagModel.cancelEdit = () => {
                    tagModel.isEditing = false;
                    tagModel.childPickerOpen = false;

                    if (!tagModel.id) {
                        // new one - we have to discard it
                                                
                        let index;

                        let parentList = self.tags;

                        if (tagModel.parentTagInstanceId) {
                            const parentTag = findTagUiModel(t => t.instanceId === tagModel.parentTagInstanceId);
                            if (!!parentTag)
                                parentList = parentTag.tags;
                        }

                        if ((index = parentList.indexOf(tagModel)) === -1) {
                            throw new Error("Unexpected state of graph :(");
                        }

                        parentList.splice(index, 1);                        

                        return;
                    }

                    tagModel.update();
                };

                tagModel.delete = () => {
                    if (!confirm("Opravdu chcete smazat štítek \"" + tagModel.name + "\?"))
                        return;

                    call("DeleteTag").query({ "tagId": tagModel.id }).post(receiveTags);
                };

                if (!placeToBeginning) {
                    source.push(tagModel);
                } else {
                    source.unshift(tagModel);
                }
            }

            return tagModel;
        };

        const findTagUiModel = (predicate, sourceList) => {

            sourceList = sourceList || self.tags;

            for (let tag of sourceList) {
                if (predicate(tag))
                    return tag;

                let res = findTagUiModel(predicate, tag.tags);
                if (!!res)
                    return res;
            }

            return null;
        };

        self.createTag = (parentTagUiModel) => {

            let list = self.tags;

            if (!!parentTagUiModel) {
                list = parentTagUiModel.tags;
            }

            let model = getTagModel(parentTagUiModel, list, null, !parentTagUiModel);

            model.name = "";
            model.cssClass = "crmDistributorTag_default";
            model.isEditing = true;
            model.isOpen = false;
            model.isRoot = !parentTagUiModel;

            if (!!parentTagUiModel) {
                model.firstParentId = parentTagUiModel.id;
            }
        };

        const collectCssClasses = () => {
            if (allCssClasses.length > 0)
                return;

            for (const sheet of document.styleSheets) {
                try {
                    for (const rule of sheet.cssRules || []) {
                        if (rule.selectorText && rule.selectorText.startsWith('.crmDistributorTag_')) {
                            allCssClasses.push(rule.selectorText.trim().substring(1));
                        }
                    }

                    if (allCssClasses.length > 0)
                        return;

                } catch (e) {
                    console.warn('Cannot access stylesheet:', sheet.href);
                }
            }
        };

        self.getAllTagNames = (qry, callback) => {
            callback(tagData.map(t => t.Name));
        };

        setTimeout(collectCssClasses, 500);
        

        /**** GROUPS *******************************/

        self.groups = [];
        self.groupsFilter = null;

        self.activeGroup = null;
        self.hasActiveGroup = false;

        self.openGroup = (id) => {
            self.activeGroup = self.groups.find(g => g.Id === id);
            self.hasActiveGroup = !!self.activeGroup;
            self.tags.splice(0, self.tags.length);

            if (!!self.activeGroup) {
                loadTags();
            }            
        };

        self.closeGroup = () => {
            self.activeGroup = null;
            self.hasActiveGroup = !!self.activeGroup;
        }

        self.filterGroups = (filter) => {
            self.groupsFilter = filter;
            updateGroupsView();
        };

        self.createGroup = (name) => call("SaveGroup").query({ "name": name }).post(receiveGroups);
        self.getGroups = () => call("GetGroups").get(receiveGroups);

        const receiveGroups = (groups) => {
            self.closeGroup();
            self.groups = groups;

            self.groups.forEach(g => g.searchTags = g.SearchTag.split('|').slice(1).map(t => { return { "tag": t } }));

            updateGroupsView();
        };
        
        const updateGroupsView = () => {
            const matcher = new TextMatcher(self.groupsFilter);
            self.groups.forEach(g => g.isHidden = !matcher.match(g.SearchTag, true));
        };

        

        self.getGroups();
    }
};

app.CustomerTaggingDesigner.vm = app.CustomerTaggingDesigner.vm || new app.CustomerTaggingDesigner.VM();