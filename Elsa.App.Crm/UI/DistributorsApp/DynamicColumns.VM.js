app = app || {};
app.DynamicColumns = (self) => {
        
    self.gridControlUrl = null;

    const receiveColumnList = (columns) => {
        self.filter.gridColumns = columns.map((column, index) => {
            const model = {
                "id": column.Id,
                "title": column.Title,
                "isSelected": index === 0
            };

            model.change = (newValue) => model.isSelected = !!newValue;
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

        if (!doNotInitiateSearch)
            app.Distributors.vm.search();

        localStorage.setItem("distributorGridSelectedColumns", query);
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