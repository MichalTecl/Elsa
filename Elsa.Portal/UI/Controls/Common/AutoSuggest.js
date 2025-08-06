var natcharmap = natcharmap ||
{
    "Á": "A",
    "Č": "C",
    "Ď": "D",
    "Ě": "E",
    "É": "E",
    "Í": "I",
    "Ň": "N",
    "Ó": "O",
    "Ř": "R",
    "Š": "S",
    "Ť": "T",
    "Ú": "U",
    "Ů": "U",
    "Ý": "Y",
    "Ž": "Z",
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
    "ú": "u",
    "ů": "u",
    "ý": "y",
    "ž": "z",
    "denat": function(inp) {
        var rb = [];

        for (var i = 0; i < inp.length; i++) {
            var orichar = inp.charAt(i);
            rb.push(natcharmap[orichar] || orichar);
        }

        return rb.join("");
    }
 };





var app = app || {};
app.ui = app.ui || {};

app.ui.autosuggest = app.ui.autosuggest || function (container, itemsSource, argumentFactory, customizer, pickCallback) {

    if (!!container["__autosuggestAttached"])
        return;

    container["__autosuggestAttached"] = true;

    customizer = customizer || app.ui.autosuggest.defaultCustomizer;

    if (!window["autosuggestStlyeLoaded"]) {
        window["autosuggestStyleLoaded"] = true;
        
        var link = document.createElement("link");
        link.setAttribute("rel", "stylesheet");
        link.setAttribute("href", "/UI/Controls/Common/Autosuggest.css");
        document.body.appendChild(link);
    }

    var input = container.getElementsByTagName("input");
    if (input.length !== 1) {
        throw new Error("Autosuggest container must contain 1 input element");
    }

    var inp = input[0];
    inp.customizer = customizer;
    inp.setAttribute("autocomplete", "off");
    
    /*the autocomplete function takes two arguments,
  the text field element and an array of possible autocompleted values:*/
    var currentFocus;
    /*execute a function when someone writes in the text field:*/
    inp.addEventListener("input", function (e) {

        this.customizerContext = this.customizer.initContext(this);
        var a, b, i, val = this.customizer.toSearchExpression(this.value, this.customizerContext);
        
        closeAllLists();
        if (!val) { return false; }
        currentFocus = -1;

        /*create a DIV element that will contain the items (values):*/
        a = document.createElement("DIV");
        a.setAttribute("id", this.id + "autocomplete-list");
        a.setAttribute("class", "autocomplete-items");
        /*append the DIV element as a child of the autocomplete container:*/
        this.parentNode.appendChild(a);
        /*for each item in the array...*/

        var argument = null;
        if (argumentFactory) {
            argument = argumentFactory();
        }
        

        itemsSource(val, 
            function (arr) {

                var matcher = new TextMatcher(val);
                
                for (i = 0; i < arr.length; i++) {
                    
                    if (matcher.match(arr[i])) { //    natcharmap.denat(arr[i].toUpperCase()).indexOf(natcharmap.denat(val.toUpperCase())) > -1) {
                        /*create a DIV element for each matching element:*/
                        b = document.createElement("DIV");
                        /*make the matching letters bold:*/
                        b.innerHTML = "<strong>" + arr[i].substr(0, val.length) + "</strong>";
                        b.innerHTML += arr[i].substr(val.length);
                        /*insert a input field that will hold the current array item's value:*/
                        b.innerHTML += "<input type='hidden' value='" + arr[i] + "'>";
                        /*execute a function when someone clicks on the item value (DIV element):*/
                        b.addEventListener("click",
                            function(e) {
                                /*insert the value for the autocomplete text field:*/
                                var chosenValue = this.getElementsByTagName("input")[0].value;
                                inp.customizer.applySelectedValue(chosenValue, inp, inp.customizerContext);
                                
                                if ("createEvent" in document) {
                                    var evt = document.createEvent("HTMLEvents");
                                    evt.initEvent("change", false, true);
                                    evt.initEvent("input", false, true);
                                    inp.dispatchEvent(evt);
                                } else {
                                    inp.fireEvent("onchange");
                                    inp.fireEvent("oninput");
                                }

                                if (!!pickCallback)
                                    pickCallback(chosenValue);

                                closeAllLists();
                            });
                        a.appendChild(b);
                    }
                }
            }, argument);
    });


    /*execute a function presses a key on the keyboard:*/
    inp.addEventListener("keydown", function (e) {
        var x = document.getElementById(this.id + "autocomplete-list");
        if (x) x = x.getElementsByTagName("div");
        if (e.keyCode == 40) {
            /* If the arrow DOWN key is pressed,
            increase the currentFocus variable: */
            currentFocus++;
            /* and make the current item more visible: */
            addActive(x);
        } else if (e.keyCode == 38) { // up
            /* If the arrow UP key is pressed,
            decrease the currentFocus variable: */
            currentFocus--;
            /* and make the current item more visible: */
            addActive(x);
        } else if (e.keyCode == 13) { // enter
            /* If the ENTER key is pressed, prevent the form from being submitted: */
            e.preventDefault();
            if (currentFocus > -1) {
                /* Simulate a click on the "active" item: */
                if (x) x[currentFocus].click();
            } else {
                /* If no item is active, trigger change event with current input value: */
                if ("createEvent" in document) {
                    var evt = document.createEvent("HTMLEvents");
                    evt.initEvent("change", false, true);
                    evt.initEvent("input", false, true);
                    inp.dispatchEvent(evt);
                } else {
                    inp.fireEvent("onchange");
                    inp.fireEvent("oninput");
                }
                                
                /* Close all autocomplete lists: */
                closeAllLists();
            }
        }
    });

    function addActive(x) {
        /*a function to classify an item as "active":*/
        if (!x) return false;
        /*start by removing the "active" class on all items:*/
        removeActive(x);
        if (currentFocus >= x.length) currentFocus = 0;
        if (currentFocus < 0) currentFocus = (x.length - 1);
        /*add class "autocomplete-active":*/
        x[currentFocus].classList.add("autocomplete-active");

        x[currentFocus].scrollIntoView({ block: "nearest" });
    }
    function removeActive(x) {
        /*a function to remove the "active" class from all autocomplete items:*/
        for (var i = 0; i < x.length; i++) {
            x[i].classList.remove("autocomplete-active");
        }
    }
    function closeAllLists(elmnt) {
        /*close all autocomplete lists in the document,
        except the one passed as an argument:*/
        var x = document.getElementsByClassName("autocomplete-items");
        for (var i = 0; i < x.length; i++) {
            if (elmnt != x[i] && elmnt != inp) {
                x[i].parentNode.removeChild(x[i]);
            }
        }
    }
    /*execute a function when someone clicks in the document:*/
    document.addEventListener("click", function (e) {
        closeAllLists(e.target);
    });

};

app.ui.autosuggest.defaultCustomizer = app.ui.autosuggest.defaultCustomizer ||
{
    "initContext": function (inputElement) { return null; },
    "toSearchExpression": function (inputExpression, context) { return inputExpression; },
    "applySelectedValue": function (selectedValue, inputElement, context) { inputElement.value = selectedValue; }
};
