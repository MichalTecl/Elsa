var TextMatcher = TextMatcher || function(pattern) {
    
    pattern = TextMatcher.natcharmap.denat(pattern || "").trim().toLowerCase();
    var words = pattern.split(" ");

    this.match = function (text, emptyQueryResult) {

        if (pattern.length === 0) {
            return !!emptyQueryResult;
        }

        text = TextMatcher.natcharmap.denat(text || "").trim().toLowerCase();
        
        for (var i = 0; i < words.length; i++) {

            var word = words[i];
            if (word.trim().length < 1) {
                continue;
            }

            if (text.indexOf(word) < 0) {
                return false;
            }
        }

        return true;
    };
};

TextMatcher.natcharmap = TextMatcher.natcharmap ||
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
    "denat": function (inp) {
        var rb = [];

        for (var i = 0; i < inp.length; i++) {
            var orichar = inp.charAt(i);
            rb.push(natcharmap[orichar] || orichar);
        }

        return rb.join("");
    }
};
