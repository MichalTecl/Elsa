app = app || {};
app.DynamicColumns = (self) => {
        
    self.gridControlUrl = null;
    self.sorters = {};

    const receiveColumnList = (columns) => {
        self.filter.gridColumns = columns.map((column, index) => {
            const model = {
                "id": column.Id,
                "title": column.Title,
                "isSelected": index === 0
            };
                        
            return model;
        });

        const preferredColumns = localStorage.getItem("distributorGridSelectedColumns");
        if (!!preferredColumns) {
            const colindex = preferredColumns.split(",");

            self.filter.gridColumns.forEach(c => c.isSelected = colindex.indexOf(c.id) > -1);

            const selectedCol = self.filter.gridColumns.find(c => c.isSelected);
            if (!selectedCol) {
                localStorage.removeItem("distributorGridSelectedColumns");
                return receiveColumnList();
            }
        }

        self.onColumnsSelected();
    };

    const loadColumnList = () => lt.api("/CrmDynamicColumns/GetColumns").get(receiveColumnList);
    loadColumnList();

    self.onColumnsSelected = (doNotInitiateSearch) => {
        const query = self.filter.gridColumns.filter(c => c.isSelected).map(c => c.id).join(","); 
        const gridUrl = "/CrmDynamicColumns/getGridHtml?query=" + encodeURIComponent(query);

        if (gridUrl === self.gridControlUrl)
            return;

        self.gridControlUrl = gridUrl;

        self.filter.gridColumns.map(c => c.id).forEach(col => {
            self.sorters[col] = self.sorters[col] || {
                "isActive": false,
                "descending": false
            };            
        });

        self.filter.gridColumns.forEach(col => {
            col.change = (newValue) => col.isSelected = !!newValue;
        });

        if (!doNotInitiateSearch)
            app.Distributors.vm.search();

        localStorage.setItem("distributorGridSelectedColumns", query);
    };

    self.updateSortingModel = () => {
        for (const columnId in self.sorters) {
            if (columnId === self.filter.SortBy) {
                self.sorters[columnId].isActive = true;
                self.sorters[columnId].descending = self.filter.SortDescending;
            } else {
                self.sorters[columnId].isActive = false;
                self.sorters[columnId].descending = false;
            }
        }
    };

    self.changeGridSorting = (columnId) => {

        if (self.filter.SortBy === columnId) {
            self.filter.SortDescending = !self.filter.SortDescending;
        } else {
            self.filter.SortBy = columnId;
            self.filter.SortDescending = false;
        }

        self.search();

        self.updateSortingModel();
    };
};

const setupDynamicColumnsPlugin = () => {

    if ((!app.Distributors) || (!app.Distributors.vm)) {
        setTimeout(setupDynamicColumnsPlugin, 50);
        return;
    }

    app.DynamicColumns(app.Distributors.vm);
}
setupDynamicColumnsPlugin();