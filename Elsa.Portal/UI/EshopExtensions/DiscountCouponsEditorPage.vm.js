var app = app || {};
app.DiscountCouponsEditor = app.DiscountCouponsEditor || {};

app.DiscountCouponsEditor.VM = app.DiscountCouponsEditor.VM || function () {
    const self = this;
    const activeCheckboxId = "couponEditorActiveCheckbox";
    const conditionTypes = {
        productsInCart: "productsInCart",
        forbiddenProductsInCart: "forbiddenProductsInCart",
        minimumOrderPrice: "minimumOrderPrice"
    };

    self.ruleList = [];
    self.activeRule = null;
    self.hasActiveRule = false;
    self.productHelpVisible = false;

    const newKey = () => crypto.randomUUID();

    const normalizeProductPath = (value) => {
        if (!value) {
            return "";
        }

        let normalized = value.trim();

        try {
            normalized = decodeURIComponent(normalized);
        } catch (e) {
        }

        const productPathIndex = normalized.toLowerCase().indexOf("/p/");
        if (productPathIndex >= 0) {
            normalized = normalized.substring(productPathIndex);
        }

        normalized = normalized.split("#")[0];

        return normalized.trim();
    };

    const isValidProductPath = (value) => {
        const normalized = normalizeProductPath(value);
        return /^\/p\/\d+(\/[^?#]+)?(\?variant\[\d+\]=[^&#]+(&variant\[\d+\]=[^&#]+)*)?$/i.test(normalized);
    };

    const encodeProductPathForTransport = (value) => encodeURIComponent(normalizeProductPath(value));

    const extractProductLinksFromText = (value) => {
        if (!value) {
            return [];
        }

        return value
            .split(/\r?\n/)
            .map(line => line.trim())
            .filter(line => line.length > 0)
            .map(line => {
                const markdownMatch = /^\[[^\]]*\]\(([^)]+)\)$/.exec(line);
                return markdownMatch ? markdownMatch[1] : line;
            })
            .map(normalizeProductPath)
            .filter(line => line.length > 0);
    };

    const createCouponCodeUi = (value) => ({
        Key: newKey(),
        Value: value || ""
    });

    const createProductLinkUi = (value, groupKey, ruleItemKey) => ({
        Key: newKey(),
        GroupKey: groupKey,
        RuleItemKey: ruleItemKey,
        Value: value || ""
    });

    const normalizeCouponCodesUi = () => {
        if (!self.activeRule) {
            return;
        }

        const nonEmptyCodes = (self.activeRule.CouponCodesUi || [])
            .map(c => ({
                Key: c.Key || newKey(),
                Value: (c.Value || "").trim()
            }))
            .filter(c => c.Value.length > 0);

        nonEmptyCodes.push(createCouponCodeUi(""));
        self.activeRule.CouponCodesUi = nonEmptyCodes;
    };

    const getRuleProductLinks = (ruleData) => {
        if (ruleData && Array.isArray(ruleData.RuleProducts) && ruleData.RuleProducts.length > 0) {
            return ruleData.RuleProducts;
        }

        if (ruleData && Array.isArray(ruleData.MustHaveProductsInCart) && ruleData.MustHaveProductsInCart.length > 0) {
            return ruleData.MustHaveProductsInCart;
        }

        return [];
    };

    const normalizeProductLinksUi = (item) => {
        if (!item) {
            return;
        }

        const nonEmptyLinks = (item.ProductLinksUi || [])
            .map(link => ({
                Key: link.Key || newKey(),
                GroupKey: item.GroupKey,
                RuleItemKey: item.Key,
                Value: normalizeProductPath(link.Value)
            }))
            .filter(link => link.Value.length > 0);

        nonEmptyLinks.push(createProductLinkUi("", item.GroupKey, item.Key));
        item.ProductLinksUi = nonEmptyLinks;
    };

    const createRuleItemUi = (ruleData, groupKey, showAndAlsoLabel) => {
        let conditionType = conditionTypes.productsInCart;

        if (ruleData && ruleData.ConditionType === conditionTypes.minimumOrderPrice) {
            conditionType = conditionTypes.minimumOrderPrice;
        } else if (ruleData && ruleData.ConditionType === conditionTypes.forbiddenProductsInCart) {
            conditionType = conditionTypes.forbiddenProductsInCart;
        }

        const item = {
            Key: newKey(),
            GroupKey: groupKey,
            ShowAndAlsoLabel: !!showAndAlsoLabel,
            ConditionType: conditionType,
            IsProductsInCartCondition: conditionType === conditionTypes.productsInCart,
            IsForbiddenProductsInCartCondition: conditionType === conditionTypes.forbiddenProductsInCart,
            IsMinimumOrderPriceCondition: conditionType === conditionTypes.minimumOrderPrice,
            ProductLinksUi: [],
            MinQuantity: ruleData && ruleData.MinQuantity > 0 ? ruleData.MinQuantity : 1,
            MaxQuantity: ruleData && ruleData.MaxQuantity > 0 ? ruleData.MaxQuantity : 9999,
            MinOrderPrice: ruleData && ruleData.MinOrderPrice > 0 ? ruleData.MinOrderPrice : null,
            ViolationMessage: ruleData && ruleData.ViolationMessage ? ruleData.ViolationMessage : ""
        };

        item.ProductLinksUi = getRuleProductLinks(ruleData)
            .map(value => createProductLinkUi(normalizeProductPath(value), groupKey, item.Key));

        normalizeProductLinksUi(item);
        return item;
    };

    const refreshRuleItemState = (item) => {
        if (!item) {
            return;
        }

        item.IsProductsInCartCondition = item.ConditionType === conditionTypes.productsInCart;
        item.IsForbiddenProductsInCartCondition = item.ConditionType === conditionTypes.forbiddenProductsInCart;
        item.IsMinimumOrderPriceCondition = item.ConditionType === conditionTypes.minimumOrderPrice;
    };

    const createRuleGroupUi = (ruleData) => {
        const groupKey = newKey();
        const chainItems = [];

        let current = ruleData;
        let first = true;

        while (current) {
            chainItems.push(createRuleItemUi(current, groupKey, !first));
            current = current.AndAlso;
            first = false;
        }

        if (chainItems.length === 0) {
            chainItems.push(createRuleItemUi(null, groupKey, false));
        }

        return {
            Key: groupKey,
            ShowOrLabel: false,
            CanMoveUp: false,
            CanMoveDown: false,
            ChainItems: chainItems
        };
    };

    const createEmptyDetail = () => ({
        Id: null,
        RuleName: "",
        IsActive: true,
        ValidationMessage: "",
        CanDelete: false,
        CouponCodesUi: [createCouponCodeUi("")],
        RuleGroups: [createRuleGroupUi(null)]
    });

    const refreshGroupState = () => {
        if (!self.activeRule) {
            return;
        }

        self.activeRule.RuleGroups.forEach((group, index) => {
            group.ShowOrLabel = self.activeRule.RuleGroups.length > 1 && index > 0;
            group.CanMoveUp = index > 0;
            group.CanMoveDown = index < self.activeRule.RuleGroups.length - 1;

            group.ChainItems.forEach((item, chainIndex) => {
                item.ShowAndAlsoLabel = chainIndex > 0;
                item.GroupKey = group.Key;
                refreshRuleItemState(item);
                normalizeProductLinksUi(item);
            });
        });
    };

    const syncActiveCheckbox = () => {
        const checkbox = document.getElementById(activeCheckboxId);
        if (checkbox) {
            checkbox.checked = !!(self.activeRule && self.activeRule.IsActive);
        }
    };

    const mapDetailToUi = (detail) => {
        const ui = createEmptyDetail();

        ui.Id = detail.Id || null;
        ui.RuleName = detail.RuleName || "";
        ui.IsActive = detail.IsActive !== false;
        ui.ValidationMessage = detail.ValidationMessage || "";
        ui.CanDelete = !!detail.Id;
        ui.CouponCodesUi = (detail.CouponCodes && detail.CouponCodes.length ? detail.CouponCodes : [""]).map(createCouponCodeUi);
        ui.RuleGroups = (detail.Rules && detail.Rules.length ? detail.Rules : [null]).map(createRuleGroupUi);

        self.activeRule = ui;
        self.hasActiveRule = true;

        normalizeCouponCodesUi();
        refreshGroupState();
        syncActiveCheckbox();
    };

    const buildRuleChain = (items, index) => {
        if (index >= items.length) {
            return null;
        }

        return {
            ConditionType: items[index].ConditionType || conditionTypes.productsInCart,
            RuleProducts: items[index].ProductLinksUi
                .map(link => normalizeProductPath(link.Value))
                .filter(link => !!link),
            MinQuantity: parseInt(items[index].MinQuantity, 10) || 1,
            MaxQuantity: parseInt(items[index].MaxQuantity, 10) || 9999,
            MinOrderPrice: parseFloat(items[index].MinOrderPrice) || null,
            ViolationMessage: (items[index].ViolationMessage || "").trim(),
            AndAlso: buildRuleChain(items, index + 1)
        };
    };

    const buildPayload = () => ({
        Id: self.activeRule.Id,
        RuleName: (self.activeRule.RuleName || "").trim(),
        IsActive: !!self.activeRule.IsActive,
        CouponCodes: self.activeRule.CouponCodesUi
            .map(c => (c.Value || "").trim())
            .filter(c => !!c),
        Rules: self.activeRule.RuleGroups.map(g => buildRuleChain(g.ChainItems, 0))
    });

    const mapRuleForTransport = (rule) => {
        if (!rule) {
            return null;
        }

        return {
            ConditionType: rule.ConditionType || conditionTypes.productsInCart,
            RuleProducts: ((rule.RuleProducts || rule.MustHaveProductsInCart || [])).map(encodeProductPathForTransport),
            MinQuantity: rule.MinQuantity,
            MaxQuantity: rule.MaxQuantity,
            MinOrderPrice: rule.MinOrderPrice,
            ViolationMessage: rule.ViolationMessage,
            AndAlso: mapRuleForTransport(rule.AndAlso)
        };
    };

    const createTransportPayload = (payload) => ({
        Id: payload.Id,
        RuleName: payload.RuleName,
        IsActive: payload.IsActive,
        CouponCodes: payload.CouponCodes.slice(),
        Rules: payload.Rules.map(mapRuleForTransport)
    });

    const hasInvalidRule = (rule) => {
        if (!rule) {
            return true;
        }

        const conditionType = rule.ConditionType || conditionTypes.productsInCart;

        if (conditionType === conditionTypes.productsInCart || conditionType === conditionTypes.forbiddenProductsInCart) {
            if (!Array.isArray(rule.RuleProducts) || rule.RuleProducts.length === 0) {
                return true;
            }

            if (rule.RuleProducts.some(link => !isValidProductPath(link))) {
                return true;
            }
        } else if (conditionType === conditionTypes.minimumOrderPrice) {
            if (!(parseFloat(rule.MinOrderPrice) > 0)) {
                return true;
            }
        } else {
            return true;
        }

        if (!rule.ViolationMessage || !rule.ViolationMessage.trim()) {
            return true;
        }

        return rule.AndAlso ? hasInvalidRule(rule.AndAlso) : false;
    };

    const validateForActivation = (payload) => {
        if (!payload.RuleName) {
            return "Pravidlo musí mít název.";
        }

        const duplicateExists = (self.ruleList || []).some(rule =>
            rule.Id !== payload.Id &&
            (rule.RuleName || "").trim().toLowerCase() === payload.RuleName.toLowerCase());

        if (duplicateExists) {
            return "Název pravidla musí být jedinečný.";
        }

        if (!payload.CouponCodes || payload.CouponCodes.length === 0) {
            return "Pravidlo musí mít alespoň jeden kód.";
        }

        if (!payload.Rules || payload.Rules.length === 0) {
            return "Pravidlo musí mít alespoň jednu podmínku.";
        }

        if (payload.Rules.some(hasInvalidRule)) {
            return "Každá podmínka musí mít vyplněný jeden typ podmínky a hlášku při nesplnění.";
        }

        return null;
    };

    const updateRuleList = (rules, selectedId) => {
        self.ruleList = (rules || []).map((rule) => ({
            Id: rule.Id,
            RuleName: rule.RuleName || "(bez názvu)",
            StatusText: rule.StatusText || (rule.IsActive ? "Aktivní" : "Neaktivní"),
            IsActive: !!rule.IsActive,
            CouponCodesText: rule.CouponCodesCount > 0
                ? "Kódy: " + (rule.CouponCodesPreview || "") + " (" + rule.CouponCodesCount + ")"
                : "Bez kódů",
            IsSelected: selectedId === rule.Id
        }));
    };

    const refreshList = (selectedId) => {
        lt.api("/discountCoupons/getCouponRules").get((rules) => {
            updateRuleList(rules, selectedId);
            lt.notify();
        });
    };

    const closeDetail = (selectedId) => {
        self.activeRule = null;
        self.hasActiveRule = false;
        self.productHelpVisible = false;
        syncActiveCheckbox();
        refreshList(selectedId || null);
        lt.notify();
    };

    const openRuleDetail = (ruleId) => {
        let request = lt.api("/discountCoupons/getCouponRule");
        if (ruleId) {
            request = request.query({ ruleId: ruleId });
        }

        request.get((detail) => {
            mapDetailToUi(detail || {});
            refreshList(self.activeRule.Id);
            syncActiveCheckbox();
            lt.notify();
        });
    };

    self.createRule = () => openRuleDetail(null);
    self.openRule = (ruleId) => openRuleDetail(ruleId);

    self.saveRule = () => {
        if (!self.activeRule) {
            return;
        }

        const payload = buildPayload();
        const transportPayload = createTransportPayload(payload);
        const requestedActive = !!payload.IsActive;
        const clientValidationMessage = validateForActivation(payload);

        if (requestedActive && clientValidationMessage) {
            payload.IsActive = false;
            transportPayload.IsActive = false;
            self.activeRule.IsActive = false;
            self.activeRule.ValidationMessage = clientValidationMessage;
            syncActiveCheckbox();
            alert("Pravidlo bylo uloženo jako neaktivní: " + clientValidationMessage);
        }

        lt.api("/discountCoupons/saveCouponRule")
            .body(transportPayload)
            .post((saved) => {
                if (requestedActive && saved && saved.ValidationMessage && !clientValidationMessage) {
                    alert("Pravidlo bylo uloženo jako neaktivní: " + saved.ValidationMessage);
                }

                closeDetail(null);
            });
    };

    self.deleteRule = () => {
        if (!self.activeRule || !self.activeRule.Id) {
            return;
        }

        if (!confirm("Opravdu chceš pravidlo smazat?")) {
            return;
        }

        lt.api("/discountCoupons/deleteCouponRule")
            .query({ ruleId: self.activeRule.Id })
            .post((rules) => {
                self.activeRule = null;
                self.hasActiveRule = false;
                updateRuleList(rules, null);
                lt.notify();
            });
    };

    self.closeDetail = () => closeDetail(null);
    self.toggleProductHelp = () => {
        self.productHelpVisible = !self.productHelpVisible;
        lt.notify();
    };
    self.closeProductHelp = () => {
        self.productHelpVisible = false;
        lt.notify();
    };

    self.updateRuleName = (value) => {
        self.activeRule.RuleName = value;
    };

    self.updateIsActive = (value) => {
        self.activeRule.IsActive = !!value;
        syncActiveCheckbox();
    };

    self.updateCouponCode = (key, value) => {
        const code = self.activeRule.CouponCodesUi.find(c => c.Key === key);
        if (code) {
            code.Value = value;
        }

        normalizeCouponCodesUi();
        lt.notify();
    };

    self.addRuleGroup = () => {
        self.activeRule.RuleGroups.push(createRuleGroupUi(null));
        refreshGroupState();
        lt.notify();
    };

    self.moveRuleGroup = (groupKey, direction) => {
        const currentIndex = self.activeRule.RuleGroups.findIndex(g => g.Key === groupKey);
        const targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= self.activeRule.RuleGroups.length) {
            return;
        }

        const moved = self.activeRule.RuleGroups.splice(currentIndex, 1)[0];
        self.activeRule.RuleGroups.splice(targetIndex, 0, moved);
        refreshGroupState();
        lt.notify();
    };

    self.removeRuleGroup = (groupKey) => {
        self.activeRule.RuleGroups = self.activeRule.RuleGroups.filter(g => g.Key !== groupKey);
        if (self.activeRule.RuleGroups.length === 0) {
            self.activeRule.RuleGroups.push(createRuleGroupUi(null));
        }
        refreshGroupState();
        lt.notify();
    };

    self.addAndAlso = (groupKey) => {
        const group = self.activeRule.RuleGroups.find(g => g.Key === groupKey);
        if (!group) {
            return;
        }

        group.ChainItems.push(createRuleItemUi(null, groupKey, true));
        refreshGroupState();
        lt.notify();
    };

    self.addAndAlsoToLastGroup = () => {
        if (!self.activeRule || !self.activeRule.RuleGroups || self.activeRule.RuleGroups.length === 0) {
            return;
        }

        const lastGroup = self.activeRule.RuleGroups[self.activeRule.RuleGroups.length - 1];
        self.addAndAlso(lastGroup.Key);
    };

    self.removeChainItem = (groupKey, itemKey) => {
        const group = self.activeRule.RuleGroups.find(g => g.Key === groupKey);
        if (!group) {
            return;
        }

        group.ChainItems = group.ChainItems.filter(i => i.Key !== itemKey);

        if (group.ChainItems.length === 0) {
            self.removeRuleGroup(groupKey);
            return;
        }

        refreshGroupState();
        lt.notify();
    };

    const getRuleItem = (groupKey, itemKey) => {
        const group = self.activeRule.RuleGroups.find(g => g.Key === groupKey);
        if (!group) {
            return null;
        }

        return group.ChainItems.find(i => i.Key === itemKey) || null;
    };

    self.updateRuleProductLink = (groupKey, itemKey, productLinkKey, value) => {
        const item = getRuleItem(groupKey, itemKey);
        if (!item) {
            return;
        }

        const productLink = (item.ProductLinksUi || []).find(link => link.Key === productLinkKey);
        if (!productLink) {
            return;
        }

        productLink.Value = normalizeProductPath(value);
        normalizeProductLinksUi(item);
        lt.notify();
    };

    self.setRuleConditionType = (groupKey, itemKey, conditionType) => {
        const item = getRuleItem(groupKey, itemKey);
        if (!item) {
            return;
        }

        if (conditionType === conditionTypes.minimumOrderPrice) {
            item.ConditionType = conditionTypes.minimumOrderPrice;
        } else if (conditionType === conditionTypes.forbiddenProductsInCart) {
            item.ConditionType = conditionTypes.forbiddenProductsInCart;
        } else {
            item.ConditionType = conditionTypes.productsInCart;
        }
        refreshRuleItemState(item);
        lt.notify();
    };

    self.handleRuleProductLinksPaste = (groupKey, itemKey, productLinkKey, event) => {
        const clipboardText = event && event.clipboardData
            ? event.clipboardData.getData("text")
            : "";

        const pastedLinks = extractProductLinksFromText(clipboardText);
        if (pastedLinks.length <= 1) {
            return;
        }

        const item = getRuleItem(groupKey, itemKey);
        if (!item) {
            return;
        }

        event.preventDefault();

        const targetIndex = (item.ProductLinksUi || []).findIndex(link => link.Key === productLinkKey);
        if (targetIndex < 0) {
            return;
        }

        const existingLinks = (item.ProductLinksUi || [])
            .map(link => ({
                Key: link.Key,
                GroupKey: link.GroupKey,
                RuleItemKey: link.RuleItemKey,
                Value: normalizeProductPath(link.Value)
            }))
            .filter(link => link.Value.length > 0);

        const before = existingLinks.slice(0, targetIndex);
        const after = existingLinks.slice(targetIndex + 1);

        item.ProductLinksUi = before
            .concat(pastedLinks.map(link => createProductLinkUi(link, item.GroupKey, item.Key)))
            .concat(after);

        normalizeProductLinksUi(item);
        lt.notify();
    };

    self.updateRuleMin = (groupKey, itemKey, value) => {
        const item = getRuleItem(groupKey, itemKey);
        if (item) {
            item.MinQuantity = parseInt(value, 10) || 1;
        }
    };

    self.updateRuleMax = (groupKey, itemKey, value) => {
        const item = getRuleItem(groupKey, itemKey);
        if (item) {
            item.MaxQuantity = parseInt(value, 10) || 9999;
        }
    };

    self.updateRuleMinOrderPrice = (groupKey, itemKey, value) => {
        const item = getRuleItem(groupKey, itemKey);
        if (item) {
            item.MinOrderPrice = value;
        }
    };

    self.updateRuleMessage = (groupKey, itemKey, value) => {
        const item = getRuleItem(groupKey, itemKey);
        if (item) {
            item.ViolationMessage = value;
        }
    };

    refreshList(null);
};

app.DiscountCouponsEditor.vm = app.DiscountCouponsEditor.vm || new app.DiscountCouponsEditor.VM();
