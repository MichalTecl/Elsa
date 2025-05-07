var Popup = Popup || {
    "open": (overlay) => {
        if (!overlay) return;
        overlay.classList.add("open");

        if (!overlay["popupSetup"]) {
            const onClick = (e) => {
                if (!overlay.contains(e.target)) return;
                if (e.target.closest(".popupBody")) return;

                Popup.close(overlay);               
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

            overlay["popupSetup"] = true;
        }
    },

    "close": (overlay) => {
        overlay.classList.remove("open");  
    }
};