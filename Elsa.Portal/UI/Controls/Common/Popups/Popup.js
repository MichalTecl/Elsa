var Popup = (() => {
    
    const openOverlays = [];

    function open(overlay, closeCallback) {
        if (!overlay) return;

        overlay.classList.add("open");

        if (!!closeCallback) {
            overlay.closeCallbacks = overlay.closeCallbacks || [];
            overlay.closeCallbacks.push(closeCallback);
        }

        const onClick = (e) => {
            if (!overlay.contains(e.target)) return;
            if (e.target.closest(".popupBody")) return;

            close(overlay); 
        };

        overlay.addEventListener("click", onClick);

        let needsRebind = false;
        [...overlay.querySelectorAll("[fill-by-lazy]"), overlay].forEach(el => {
            if (el.hasAttribute("fill-by-lazy")) {
                el.setAttribute("fill-by", el.getAttribute("fill-by-lazy"));
                el.removeAttribute("fill-by-lazy");
                needsRebind = true;
            }
        });

        if (needsRebind) {
            lt.notify(overlay);
        }

        openOverlays.push(overlay);
    }

    function close(overlay) {

        if (!overlay) {
            if (openOverlays.length === 0)
                return;

            return close(openOverlays[openOverlays.length - 1]);
        }

        const ooinx = openOverlays.indexOf(overlay);
        if (ooinx > -1)
            openOverlays.splice(ooinx, 1);

        overlay.classList.remove("open");

        if (!!overlay.closeCallbacks) {
            overlay.closeCallbacks.forEach(c => c());
        }
    }

    return {
        open,
        close
    };
})();
