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

        var headerRow = document.createElement("TR");
        table.appendChild(headerRow);

        var columns = source.Columns;
        for (var columnId = 0; columnId < columns.length; columnId ++) {
            var column = columns[columnId];

            var th = document.createElement("TH");
            th.innerHTML = column.Title;
            headerRow.appendChild(th);
        }

        var rows = source.Rows;
        for (var rowId = 0; rowId < rows.length; rowId++) {

            var tr = document.createElement("TR");
            table.appendChild(tr);

            var values = rows[rowId].Values;

            for (var i = 0; i < values.length; i++) {
                var td = document.createElement("TD");
                td.innerHTML = values[i];
                tr.appendChild(td);
            }
        }



    };