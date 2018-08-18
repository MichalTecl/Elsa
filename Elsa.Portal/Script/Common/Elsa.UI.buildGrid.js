var elsa = elsa || {};
elsa.ui = elsa.ui || {};
elsa.ui.buildGrid = elsa.ui.buildGrid ||
    function(source, target) {

        target.innerHTML = "";

        if (!source) {
            return;
        }

        var table = document.createElement("TABLE");
        target.appendChild(table);

        var thead = document.createElement("THEAD");
        table.appendChild(thead);

        var headerRow = document.createElement("TR");
        thead.appendChild(headerRow);

        var columns = source.Columns;
        for (var columnId = 0; columnId < columns.length; columnId ++) {
            var column = columns[columnId];

            var th = document.createElement("TH");
            th.innerHTML = column.Title;
            headerRow.appendChild(th);
        }

        var tbody = document.createElement("TBODY");
        table.appendChild(tbody);

        var rows = source.Rows;
        for (var rowId = 0; rowId < rows.length; rowId++) {

            var tr = document.createElement("TR");
            tbody.appendChild(tr);

            var values = rows[rowId].Values;

            for (var i = 0; i < values.length; i++) {
                var td = document.createElement("TD");
                td.innerHTML = values[i];
                tr.appendChild(td);
            }
        }

        table.setAttribute("class", "reportTable");

        return table;
    };