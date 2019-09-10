var XTable = XTable || function() {

    var self = this;
    var data = {};

    var visitElement = function(element, parentObject) {

        if (element.hasAttribute("xcell")) {
            parentObject.Cells = parentObject.Cells || [];
            parentObject.Cells.push({ "Value": element.innerHTML, "Data": element.getAttribute("xcell"), "NumberFormat": element.getAttribute("xnumber") });
            return;
        }

        if (element.hasAttribute("xrow")) {
            var row = { "Data": element.getAttribute("xrow") };

            parentObject.Rows = parentObject.Rows || [];
            parentObject.Rows.push(row);
            parentObject = row;

        } else if (element.hasAttribute("xsheet")) {
            var sheet = { "Data": element.getAttribute("xsheet") };
            parentObject.Sheets = parentObject.Sheets || [];
            parentObject.Sheets.push(sheet);
            parentObject = sheet;
        }

        for (var i = 0; i < element.children.length; i++) {
            visitElement(element.children[i], parentObject);
        }
    };

    self.scan = function (root) {
        visitElement(root, data);
    };

    self.createExcel = function(fileName) {
        lt.api("/XTable/UploadWorkbook").body(data).post(function(result) {
            
            var link = document.createElement('a');
            link.href = "/XTable/GetExcel?contentKey=" + result + "&fileName=" + fileName;
            link.download = fileName;
            link.click();
        });
    };
};

