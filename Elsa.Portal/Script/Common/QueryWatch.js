(function () {
    const watch = function () {

        const self = this;
        const watches = [];

        const getQueryParamValue = (key) => {
            const params = new URLSearchParams(window.location.search);
            let value = params.get(key);

            if (!!value)
                return value;

            const hashParams = new URLSearchParams(window.location.hash.substring(1));
            return hashParams.get(key);
        };

        const notifyWatches = () => {

            const index = {};

            watches.forEach(w => {

                let currentValue = index[w.key];
                if (!index.hasOwnProperty(w.key)) {
                    currentValue = index[w.key] = getQueryParamValue(w.key);
                }

                if (currentValue !== w.lastValue || (!w.hasOwnProperty("lastValue"))) {
                    w.lastValue = currentValue;
                    w.callback(currentValue);
                }
            });
        };

        self.watch = (key, callback) => {

            const existingWatch = watches.find(w => w.key === key && w.callback === callback);
            if (!!existingWatch)
                return;

            watches.push({ "key": key, "callback": callback });

            if (watches.length === 1)
                window.addEventListener('hashchange', notifyWatches);

            notifyWatches();
        };

    };

    window.queryWatch = window.queryWatch || new watch();
})();