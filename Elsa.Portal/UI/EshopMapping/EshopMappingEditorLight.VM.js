var app = app || {};
app.EshopMapping = app.EshopMapping || {};
app.EshopMapping.Light = app.EshopMapping.Light || {};
app.EshopMapping.Light.VM = app.EshopMapping.Light.VM || function() {
    const self = this;

    let allRows = [];
    let materialNameMap = {};
    let productNameMap = {};

    let materialFilter = "";
    let productFilter = "";
    let sortColumn = "material";
    let sortDirection = "asc";
    let selectedPuzzleRowKey = null;
    let localRowSequence = 0;

    self.rows = [];
    self.includeHistoricalProducts = false;

    const normalize = (value) => natcharmap.denat((value || "").trim().toLocaleLowerCase());

    const toLookup = (items) => {
        const lookup = {};

        (items || []).forEach(item => {
            const key = normalize(item);
            if (!!key && !lookup[key]) {
                lookup[key] = item;
            }
        });

        return lookup;
    };

    const hasText = (value) => !!(value || "").trim();
    const isMaterialOnly = (row) => row.hasMaterial && !row.hasProduct;
    const isProductOnly = (row) => !row.hasMaterial && row.hasProduct;

    const getPuzzleSelection = () => allRows.find(r => r.rowKey === selectedPuzzleRowKey) || null;

    const canPuzzlePair = (firstRow, secondRow) => {
        if (!firstRow || !secondRow || firstRow.rowKey === secondRow.rowKey) {
            return false;
        }

        if (isMaterialOnly(firstRow)) {
            return isProductOnly(secondRow);
        }

        if (isProductOnly(firstRow)) {
            return isMaterialOnly(secondRow);
        }

        return false;
    };

    const compareValues = (left, right) => {
        const l = normalize(left);
        const r = normalize(right);

        if (l < r) {
            return -1;
        }

        if (l > r) {
            return 1;
        }

        return 0;
    };

    const applyFilters = () => {
        const materialNeedle = normalize(materialFilter);
        const productNeedle = normalize(productFilter);
        const puzzleSelection = getPuzzleSelection();

        allRows.forEach(row => {
            const materialText = normalize(row.MaterialName || row.materialDraft);
            const productText = normalize(row.ProductName || row.productDraft);

            row.isVisible = (!materialNeedle || materialText.indexOf(materialNeedle) > -1) &&
                (!productNeedle || productText.indexOf(productNeedle) > -1);

            row.selectedAsPuzzleSource = !!puzzleSelection && puzzleSelection.rowKey === row.rowKey;
            row.puzzleTargetCandidate = !!puzzleSelection && canPuzzlePair(puzzleSelection, row);
            row.showPuzzle = row.isIncomplete && (!puzzleSelection || row.selectedAsPuzzleSource || row.puzzleTargetCandidate);
        });

        const rows = allRows.slice();
        rows.sort((a, b) => {
            const primary = sortColumn === "product"
                ? compareValues(a.ProductName, b.ProductName)
                : compareValues(a.MaterialName, b.MaterialName);

            if (primary !== 0) {
                return sortDirection === "asc" ? primary : -primary;
            }

            const fallback = sortColumn === "product"
                ? compareValues(a.MaterialName, b.MaterialName)
                : compareValues(a.ProductName, b.ProductName);

            return sortDirection === "asc" ? fallback : -fallback;
        });

        rows.forEach((row, index) => {
            row.isOddRow = (index % 2) === 1;
        });

        self.rows = rows;
        lt.notify();
    };

    const updateMaterialState = (row) => {
        row.showMaterialActions = hasText(row.materialDraft);
        row.canSaveMaterial = !!materialNameMap[normalize(row.materialDraft)];
    };

    const updateProductState = (row) => {
        row.showProductActions = hasText(row.productDraft);
        row.canSaveProduct = !!productNameMap[normalize(row.productDraft)];
    };

    const createRowKey = (row, index) => {
        return "row_" + index + "_" + (row.MaterialName || "null") + "_" + (row.ProductName || "null");
    };

    const initializeRow = (row) => {
        row.hasMaterial = hasText(row.MaterialName);
        row.hasProduct = hasText(row.ProductName);
        row.canDelete = row.hasMaterial && row.hasProduct;
        row.canDuplicate = row.canDelete;
        row.isIncomplete = row.hasMaterial !== row.hasProduct;

        row.materialDraft = row.materialDraft || "";
        row.productDraft = row.productDraft || "";
        row.showMaterialActions = hasText(row.materialDraft);
        row.showProductActions = hasText(row.productDraft);
        row.canSaveMaterial = !!materialNameMap[normalize(row.materialDraft)];
        row.canSaveProduct = !!productNameMap[normalize(row.productDraft)];
        row.isVisible = true;
        row.showPuzzle = row.isIncomplete;
        row.selectedAsPuzzleSource = false;
        row.puzzleTargetCandidate = false;

        return row;
    };

    const receiveData = (data) => {
        materialNameMap = toLookup((data || []).map(r => r.MaterialName).filter(x => !!x));
        productNameMap = toLookup((data || []).map(r => r.ProductName).filter(x => !!x));

        allRows = (data || []).map((row, index) => {
            row.rowKey = createRowKey(row, index);
            row.materialDraft = "";
            row.productDraft = "";
            return initializeRow(row);
        });

        if (!!selectedPuzzleRowKey && !getPuzzleSelection()) {
            selectedPuzzleRowKey = null;
        }

        applyFilters();
    };

    const showApiError = (errorMessage) => {
        alert(errorMessage);
    };

    const load = () => {
        lt.api("/eshopMapping/getLightMappings")
            .query({ includeHistoricalProducts: self.includeHistoricalProducts })
            .onerror(showApiError)
            .get(receiveData);
    };

    const loadWithRefresh = () => {
        lt.api("/eshopMapping/reloadErpProducts")
            .query({ includeHistoricalProducts: self.includeHistoricalProducts })
            .onerror(showApiError)
            .get(receiveData);
    };

    const getResolvedMaterialName = (row) => row.hasMaterial ? row.MaterialName : materialNameMap[normalize(row.materialDraft)];

    const getResolvedProductName = (row) => row.hasProduct ? row.ProductName : productNameMap[normalize(row.productDraft)];

    self.getMaterialNames = (query, callback) => callback(Object.values(materialNameMap));

    self.getProductNames = (query, callback) => callback(Object.values(productNameMap));

    self.setMaterialFilter = (value) => {
        materialFilter = value || "";
        applyFilters();
    };

    self.setProductFilter = (value) => {
        productFilter = value || "";
        applyFilters();
    };

    self.toggleSort = (column) => {
        if (sortColumn === column) {
            sortDirection = sortDirection === "asc" ? "desc" : "asc";
        } else {
            sortColumn = column;
            sortDirection = "asc";
        }

        applyFilters();
    };

    self.refreshProducts = () => {
        loadWithRefresh();
    };

    self.setIncludeHistoricalProducts = (checked) => {
        self.includeHistoricalProducts = !!checked;
        load();
    };

    self.setMaterialDraft = (row, value) => {
        row.materialDraft = value || "";
        updateMaterialState(row);
        applyFilters();
    };

    self.setProductDraft = (row, value) => {
        row.productDraft = value || "";
        updateProductState(row);
        applyFilters();
    };

    self.cancelMaterialDraft = (row) => {
        row.materialDraft = "";
        updateMaterialState(row);
        applyFilters();
    };

    self.cancelProductDraft = (row) => {
        if (row.isLocalDuplicate) {
            allRows = allRows.filter(r => r.rowKey !== row.rowKey);
            applyFilters();
            return;
        }

        row.productDraft = "";
        updateProductState(row);
        applyFilters();
    };

    self.cancelPuzzleSelection = () => {
        if (!selectedPuzzleRowKey) {
            return;
        }

        selectedPuzzleRowKey = null;
        applyFilters();
    };

    self.togglePuzzleRow = (row) => {
        if (!row.isIncomplete) {
            return;
        }

        const puzzleSelection = getPuzzleSelection();
        if (!puzzleSelection) {
            selectedPuzzleRowKey = row.rowKey;
            applyFilters();
            return;
        }

        if (puzzleSelection.rowKey === row.rowKey) {
            selectedPuzzleRowKey = null;
            applyFilters();
            return;
        }

        if (!canPuzzlePair(puzzleSelection, row)) {
            selectedPuzzleRowKey = null;
            applyFilters();
            return;
        }

        const materialName = puzzleSelection.hasMaterial ? puzzleSelection.MaterialName : row.MaterialName;
        const productName = puzzleSelection.hasProduct ? puzzleSelection.ProductName : row.ProductName;

        selectedPuzzleRowKey = null;

        lt.api("/eshopMapping/mapLight")
            .query({ elsaMaterialName: materialName, eshopProductName: productName, includeHistoricalProducts: self.includeHistoricalProducts })
            .onerror(showApiError)
            .get(receiveData);
    };

    self.saveRow = (row) => {
        const elsaMaterialName = getResolvedMaterialName(row);
        const eshopProductName = getResolvedProductName(row);

        if (!hasText(elsaMaterialName)) {
            alert("Zadaný název materiálu v Else neexistuje.");
            return;
        }

        if (!hasText(eshopProductName)) {
            alert("Zadaný název produktu z e-shopu neexistuje.");
            return;
        }

        lt.api("/eshopMapping/mapLight")
            .query({ elsaMaterialName, eshopProductName, includeHistoricalProducts: self.includeHistoricalProducts })
            .onerror(showApiError)
            .get(receiveData);
    };

    self.deleteMapping = (row) => {
        if (!row.canDelete) {
            return;
        }

        if (!confirm("Opravdu chcete zrušit mapování produktu \"" + row.ProductName + "\" na materiál \"" + row.MaterialName + "\"?")) {
            return;
        }

        lt.api("/eshopMapping/unmapLight")
            .query({ elsaMaterialName: row.MaterialName, eshopProductName: row.ProductName, includeHistoricalProducts: self.includeHistoricalProducts })
            .onerror(showApiError)
            .get(receiveData);
    };

    self.duplicateRow = (row) => {
        if (!row.canDuplicate) {
            return;
        }

        const duplicateRow = initializeRow({
            rowKey: "local_" + (++localRowSequence),
            MaterialId: row.MaterialId,
            MaterialName: row.MaterialName,
            ProductName: null,
            materialDraft: "",
            productDraft: "",
            isLocalDuplicate: true
        });

        const sourceIndex = allRows.findIndex(r => r.rowKey === row.rowKey);
        const insertIndex = sourceIndex < 0 ? allRows.length : sourceIndex + 1;
        allRows.splice(insertIndex, 0, duplicateRow);
        applyFilters();
    };

    load();
};

app.EshopMapping.lightVm = app.EshopMapping.lightVm || new app.EshopMapping.Light.VM();
