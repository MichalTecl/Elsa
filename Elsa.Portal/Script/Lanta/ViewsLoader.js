var lanta = lanta || {};
lanta.ViewLoading = lanta.ViewLoading || {};
lanta.ViewLoading.makeScriptsLive = lanta.ViewLoading.makeScriptsLive ||
    function(div) {

        var allScripts = div.getElementsByTagName("script");

        for (var i = 0; i < allScripts.length; i++) {
            var sourceTag = allScripts[i];

            var newTag = document.createElement("script");
            var sourceAttributes = sourceTag.attributes;
            if (!!sourceAttributes) {
                for (var j = 0; j < sourceAttributes.length; j++) {
                    var sourceAttribute = sourceAttributes[j];
                    newTag.setAttribute(sourceAttribute.name, sourceAttribute.value);
                }
            }

            newTag.innerHTML = sourceTag.innerHTML;
            sourceTag.parentElement.insertBefore(newTag, sourceTag);
            sourceTag.parentElement.removeChild(sourceTag);
        }

    };

lanta.ViewLoading.loader = function(target, value, attribute) {
    
    lt.api(value).useCache().noJson().get(function(html) {
        
        if (attribute === "fill-by") {
            target.innerHTML = html;
            return;
        }

        if (attribute === "replace-by") {
            var xdiv = document.createElement("DIV");
            xdiv.innerHTML = html;

            lanta.ViewLoading.makeScriptsLive(xdiv);

            var parent = target.parentElement;

            var els = [];
            for (var nid = 0; nid < xdiv.childNodes.length; nid++) {
                els.push(xdiv.childNodes[nid]);
            }
            xdiv.innerHTML = '';
            for (var i = 0; i < els.length; i++) {
                parent.insertBefore(els[i], target);
            }
            
            parent.removeChild(target);
        }

    });

};

lt.watchCustomAttribute("fill-by", lanta.ViewLoading.loader);
lt.watchCustomAttribute("replace-by", lanta.ViewLoading.loader);