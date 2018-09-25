var app = app || {};
app.ui = app.ui || {};
app.ui.MaterialList = app.ui.MaterialList || {};
app.ui.MaterialList.ViewModel = app.ui.MaterialList.ViewModel || function() {
    
    var self = this;

    var replacements = {
        "á": "a",
        "č": "c",
        "ď": "d",
        "ě": "e",
        "é": "e",
        "í": "i",
        "ň": "n",
        "ó": "o",
        "ř": "r",
        "š": "s",
        "ť": "t",
        "ů": "u",
        "ú": "u",
        "ý": "y",
        "ž": "z"
    };

    var allowedChars = "abcdefghijklmnopqrstuvwxyz1234567890";

    var allMaterials = null;

    var normalizeText = function(inp) {
        if (!inp) {
            return "";
        }

        inp = inp.toLowerCase();

        var result = [];

        for (var i = 0; i < inp.length; i++) {
            var ch = inp.charAt(i);

            ch = replacements[ch] || ch;

            if (allowedChars.indexOf(ch) === -1) {
                continue;
            }

            result.push(ch);
        }

        return result.join("");
    };
    
    var loadMaterials = function(callback) {
        
        if (!!allMaterials) {
            if (!!callback) {
                callback(allMaterials);
            }

            return;
        }

        lt.api("/virtualProducts/getAllMaterials").get(function (received) {

            for (var i = 0; i < received.length; i++) {
                received[i].normalizedName = normalizeText(received[i].Name);
            }

            allMaterials = received;

            if (!!callback) {
                callback(allMaterials);
            }
        });
    };

    self.findMaterials = function(query, callback) {
        
        loadMaterials(function(all) {

            var originalQuery = query;

            query = normalizeText(query);

            var filtered = [];
            for (var i = 0; i < all.length; i++) {
                var src = all[i];
                
                if (src.Name === originalQuery) {
                    callback([]);
                    return;
                }

                if (src.normalizedName.indexOf(query) !== -1) {
                    filtered.push(src.Name);
                }
            }

            callback(filtered);
        });
    };

    self.parseMaterialEntry = function (e) {

        var result = {
            amount: 0,
            unitName: null,
            materialName: null,
            error: "Zadejte množství"
        };

        var re = /((\d+(?:[\.\,]\d{1,2})?))(\s*)(\S*)(\s*)(.*)/;
        var match = re.exec(e);

        if (!match) {
            return result;
        }

        try {
            
            result.amount = parseFloat(match[1]);
            result.unitName = match[4];
            result.materialName = match[6];

            if (!result.amount) {
                result.error = "Zadejte množství";
            }
            else if (!result.unitName) {
                result.error = "Chybí měrná jednotka";
            }
            else if (!result.materialName) {
                result.error = "Chybí název materiálu";
            } else {
                result.error = null;
            }

        } catch (exc) {
            result.error = exc.message;
        }

        return result;
    };

    self.validateMaterialEntry = function(e, callback) {
        
        var entry = self.parseMaterialEntry(e);

        if (!!entry.error) {
            callback(entry);
            return;
        }



    };

    setTimeout(loadMaterials, 100);
};

app.ui.MaterialList.vm = app.ui.MaterialList.vm || new app.ui.MaterialList.ViewModel();

app.ui.MaterialList.renderTo = app.ui.MaterialList.renderTo || function(target) {
    lt.fillBy(target, "/UI/Controls/Common/MaterialList/MaterialListView.html", function() {
        
        var materialItemController = function(suggestions, suggestionTemplate, errorsDisplay, tbMaterialEntry, butMinus) {

            var self = this;

            var currentEntry = null;

            var updateSuggestions = function() {
                
                if (!!currentEntry && (!!currentEntry.materialName) && currentEntry.materialName.length > 1) {
                    app.ui.MaterialList.vm.findMaterials(currentEntry.materialName,
                        function(suggestedItems) {

                            var sugary = [];

                            for (var i = 0; i < suggestedItems.length; i++) {
                                sugary.push({ "text": suggestedItems[i] });
                            }

                            lt.generate(suggestions, suggestionTemplate, sugary, function (s) { return s.text; });

                            if (sugary.length === 0) {
                                suggestions.style.display = 'none';
                            } else {
                                suggestions.style.display = 'block';
                            }

                            if (getSelectedSuggestionIndex() === -1) {
                                moveSuggestionSelection(1);
                            }

                        });
                } else {
                    lt.generate(suggestions, suggestionTemplate, []);
                    suggestions.style.display = 'none';
                }
            };

            this.onSuggestionClick = function (vm) {
                var currentText = tbMaterialEntry.value;
                var parsed = app.ui.MaterialList.vm.parseMaterialEntry(currentText);
                parsed.materialName = vm.text;

                tbMaterialEntry.value = parsed.amount + parsed.unitName + " " + parsed.materialName;
            };

            var getSelectedSuggestionIndex = function() {
                
                for (var i = 0; i < suggestions.children.length; i++) {
                    var sugNode = suggestions.children[i];
                    if (sugNode.getAttribute("lt-selected-item") === "1") {
                        return i;
                    }
                }

                return -1;
            };
            
            var moveSuggestionSelection = function(direction) {

                if (suggestions.children.length === 0) {
                    return;
                }

                var selectedSug = getSelectedSuggestionIndex();
                if (selectedSug > -1) {
                    var ch = suggestions.children[selectedSug];
                    ch.setAttribute("lt-selected-item", "0");
                }


                selectedSug += direction;

                if (selectedSug < 0) {
                    selectedSug = suggestions.children.length - 1;
                }

                if (selectedSug >= suggestions.children.length) {
                    selectedSug = 0;
                }

                suggestions.children[selectedSug].setAttribute("lt-selected-item", "1");
            };

            var applyCurrentSuggestion = function() {

                var sugIndex = getSelectedSuggestionIndex();
                if (sugIndex === -1) {
                    return;
                }

                var sugItem = suggestions.children[sugIndex];
                var vm = lt.getViewModel(sugItem);

                if ((!vm) || (!vm.text)) {
                    return;
                }

                self.onSuggestionClick(vm);
            };

            this.onMaterialTextChange = function (vm, newText) {

                if (vm.displayText === newText) {
                    return;
                }
                
                vm.displayText = newText;

                var parsed = app.ui.MaterialList.vm.parseMaterialEntry(newText);
                currentEntry = parsed;

                if (!!parsed.error) {
                    errorsDisplay.innerHTML = parsed.error;
                    errorsDisplay.style.display = 'block';
                } else {
                    errorsDisplay.innerHTML = "";
                    errorsDisplay.style.display = 'none';
                }
                
                setTimeout(updateSuggestions, 1);
            };

            this.onInputFocus = function () {

                self.onMaterialTextChange(lt.getViewModel(tbMaterialEntry), tbMaterialEntry.value);
                butMinus.style.display = 'inline-block';
            };

            this.onInputBlur = function () {

                setTimeout(function() {
                        butMinus.style.display = 'none';
                    },
                    200);
                suggestions.style.display = "none";
                suggestions.innerHTML = "";
                errorsDisplay.innerHTML = "";
                errorsDisplay.style.display = 'none';
            };

            this.onSuggestionKeyDown = function (e) {
                
                if (e.key === "ArrowDown") {
                    moveSuggestionSelection(1);
                    e.stopPropagation();
                    e.preventDefault();
                } else if (e.key === "ArrowUp") {
                    moveSuggestionSelection(-1);
                    e.stopPropagation();
                    e.preventDefault();
                } else if (e.key === "Enter") {
                    applyCurrentSuggestion();
                    e.stopPropagation();
                    e.preventDefault();
                }

            };
            
        };
        


        lt.element(target).attach(function (theList, materialEntryItemTemplate) {

            var self = this;
            var materialsCollection = [];

            var setMaterials = function(materials) {

                for (var i = 0; i < materials.length; i++) {
                    var src = materials[i];

                    if (!src.displayText && (!!src.Amount)) {
                        materials[i].displayText = src.Amount + src.UnitSymbol + " " + src.Name;
                    }
                }

                materialsCollection = materials;
                lt.generate(theList, materialEntryItemTemplate, materials, null, materialItemController);
            };


            this.bind(setMaterials).materialsRelativeToVm();

            this.deleteMaterial = function(model) {
                
                for (var i = 0; i < materialsCollection.length; i++) {

                    var existing = materialsCollection[i];

                    if (existing === model) {
                        materialsCollection.splice(i, 1);
                        break;
                    }
                }

                setMaterials(materialsCollection);
                lt.notify();
            };

            this.addMaterial = function() {
                materialsCollection.push({});
                setMaterials(materialsCollection);
                lt.notify();
            };

        });
    });
};