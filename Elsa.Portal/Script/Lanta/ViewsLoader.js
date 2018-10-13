var lanta = lanta || {};
lanta.ViewLoading = lanta.ViewLoading || {};
lanta.ViewLoading.cache = lanta.ViewLoading.cache || {};
lanta.ViewLoading.makeScriptsLive = lanta.ViewLoading.makeScriptsLive ||
    function (div, sourceUrl) {

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

            newTag.innerHTML = sourceTag.innerHTML + "\r\n//# sourceURL=" + sourceUrl;
            sourceTag.parentElement.insertBefore(newTag, sourceTag);
            sourceTag.parentElement.removeChild(sourceTag);
        }

    };

lanta.ViewLoading.loader = function (target, value, attribute, callback) {

    lt.api(value).useCache().ignoreDisabledApi().noJson().get(function (html) {
        
        if (html.indexOf("{%GENERATE%}") > -1) {
            var dynamicValue = new Date().getUTCMilliseconds() + "_" + Math.random();

            while (html.indexOf("{%GENERATE%}") > -1) {
                html = html.replace("{%GENERATE%}", dynamicValue);
            }
        }

        if (attribute === "fill-by") {
            target.innerHTML = html;
            lanta.ViewLoading.makeScriptsLive(target, value);
            if (!!callback) {
                callback(target);
            }
            return;
        }

        if (attribute === "replace-by") {
            var xdiv = document.createElement("DIV");
            xdiv.innerHTML = html;

            lanta.ViewLoading.makeScriptsLive(xdiv, value);

            var parent = target.parentElement;

            var els = [];
            for (var nid = 0; nid < xdiv.childNodes.length; nid++) {
                els.push(xdiv.childNodes[nid]);
            }
            xdiv.innerHTML = '';
            for (var i = 0; i < els.length; i++) {

                try {
                    target.parentNode.insertBefore(els[i], target);
                }catch (ieieie){}
            }

            parent.removeChild(target);

            if (!!callback) {
                callback(els[0]);
            }
        }

    });

};

lt.watchCustomAttribute("fill-by", lanta.ViewLoading.loader);
lt.watchCustomAttribute("replace-by", lanta.ViewLoading.loader);

lt.replaceBy = lt.replaceBy || function (placeholder, src, callback) {
    lanta.ViewLoading.loader(placeholder, src, "replace-by", callback);
};

lt.fillBy = lt.fillBy || function (placeholder, src, callback) {
    lanta.ViewLoading.loader(placeholder, src, "fill-by", callback);
};