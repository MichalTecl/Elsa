(function () {
    "use strict";

    lt.element("halloweenBat").attach(function () {
        var bat = this;
        // The widget may be loaded again after logging in. Keep just one companion.
        if (window.elsaHalloweenBat) {
            window.elsaHalloweenBat.destroy();
        }
        document.body.appendChild(bat);

        var motion = window.matchMedia("(prefers-reduced-motion: reduce)");
        var frame = null;
        var lastTime = null;
        var elapsed = 0;
        var state = "away";
        var stateUntil = 0;
        var nextTarget = 0;
        var x = window.innerWidth * .5;
        var y = Math.min(180, window.innerHeight * .4);
        var vx = 0;
        var vy = 0;
        var target = { x: x, y: y };
        var arrivalOrigin = { x: x, y: y };
        var arrivalStarted = 0;
        var mouse = null;
        var destroyed = false;
        var perchOffset = .5;
        var WIDTH = 96;
        var HEIGHT = 64;
        var ARRIVAL_DURATION = 3;

        function random(min, max) {
            return min + Math.random() * (max - min);
        }

        function clamp(value, min, max) {
            return Math.max(min, Math.min(max, value));
        }

        function bounds() {
            return {
                left: Math.min(WIDTH / 2, window.innerWidth / 2),
                right: Math.max(window.innerWidth / 2, window.innerWidth - WIDTH / 2),
                top: Math.min(HEIGHT / 2, window.innerHeight / 2),
                bottom: Math.max(window.innerHeight / 2, window.innerHeight - HEIGHT / 2)
            };
        }

        function perch() {
            var header = document.getElementById("userPanelHead");
            if (!header) {
                return null;
            }
            var rect = header.getBoundingClientRect();
            if (rect.bottom < 0 || rect.bottom > window.innerHeight - HEIGHT || rect.width < WIDTH) {
                return null;
            }
            var area = bounds();
            return {
                x: clamp(rect.left + rect.width * perchOffset, area.left, area.right),
                // In the upside-down pose the feet are 20px above the centre.
                y: clamp(rect.bottom + 20, area.top, area.bottom)
            };
        }

        function refreshVisibility() {
            bat.style.visibility = state === "away" || motion.matches || document.hidden ? "hidden" : "visible";
        }

        function setState(value) {
            state = value;
            bat.classList.remove("halloween-bat--settling");
            bat.classList.toggle("halloween-bat--hanging", value === "hanging");
            bat.classList.toggle("halloween-bat--frantic", value === "frantic" || value === "departing");
            bat.classList.toggle("halloween-bat--away", value === "away");
            refreshVisibility();
        }

        function render() {
            var tilt = state === "hanging" ? 0 : clamp(vx * .055, -22, 22);
            bat.style.transform = "translate3d(" + (x - WIDTH / 2) + "px," + (y - HEIGHT / 2) + "px,0) rotate(" + tilt + "deg)";
        }

        function pickTarget() {
            var area = bounds();
            target = { x: random(area.left, area.right), y: random(Math.min(area.top + 30, area.bottom), area.bottom) };
            nextTarget = elapsed + random(.3, .7);
        }

        function arrive(anchor) {
            perchOffset = random(.2, .8);
            anchor = perch();
            var fromLeft = Math.random() < .5;
            x = fromLeft ? -WIDTH * 1.5 : window.innerWidth + WIDTH * 1.5;
            y = clamp(anchor.y + random(30, 90), bounds().top, bounds().bottom);
            vx = fromLeft ? 100 : -100;
            vy = 0;
            target = anchor;
            arrivalOrigin = { x: x, y: y };
            arrivalStarted = elapsed;
            // Place the bat outside the viewport before making it visible.
            render();
            setState("arriving");
        }

        function depart() {
            var toLeft = mouse ? mouse.x >= x : x < window.innerWidth / 2;
            target = {
                x: toLeft ? -WIDTH * 2 : window.innerWidth + WIDTH * 2,
                y: random(bounds().top, bounds().bottom)
            };
            setState("departing");
        }

        function hideAway() {
            setState("away");
            stateUntil = elapsed + 5;
            vx = vy = 0;
        }

        function canBeScared() {
            return state === "arriving" || state === "hanging";
        }

        function startFranticFlight() {
            setState("frantic");
            stateUntil = elapsed + random(2, 3.5);
            pickTarget();
            var dx = x - mouse.x;
            var dy = y - mouse.y;
            var distance = Math.hypot(dx, dy) || 1;
            vx = dx / distance * 480;
            vy = dy / distance * 480 + 100;
        }

        function scare(event) {
            mouse = { x: event.clientX, y: event.clientY };
            if (canBeScared() && Math.hypot(x - mouse.x, y - mouse.y) < 85) {
                startFranticFlight();
            }
        }

        function tick(time) {
            frame = null;
            if (destroyed || motion.matches || document.hidden) {
                return;
            }
            var dt = lastTime === null ? 0 : Math.min((time - lastTime) / 1000, .05);
            lastTime = time;
            // Count only time spent on the visible home screen, including the return delay.
            elapsed += dt;
            var anchor = perch();

            if (state === "away") {
                if (elapsed >= stateUntil && anchor) {
                    arrive(anchor);
                }
            } else if (state === "hanging") {
                if (anchor) {
                    x = anchor.x;
                    y = anchor.y;
                } else {
                    depart();
                }
            } else {
                if (state === "arriving") {
                    if (anchor) {
                        target = anchor;
                    } else {
                        hideAway();
                    }
                } else if (state === "frantic") {
                    if (elapsed >= stateUntil) {
                        depart();
                    } else if (elapsed >= nextTarget || Math.hypot(target.x - x, target.y - y) < 35) {
                        pickTarget();
                    }
                }

                if (state !== "away") {
                    var arrivalProgress = 0;
                    if (state === "arriving") {
                        // Use a timed path so every arrival takes three seconds on any screen size.
                        arrivalProgress = clamp((elapsed - arrivalStarted) / ARRIVAL_DURATION, 0, 1);
                        var travel = arrivalProgress * arrivalProgress * (3 - 2 * arrivalProgress);
                        var weave = Math.sin(Math.PI * arrivalProgress);
                        var oldX = x;
                        var oldY = y;
                        x = arrivalOrigin.x + (target.x - arrivalOrigin.x) * travel + Math.sin(elapsed * 12) * 12 * weave;
                        y = arrivalOrigin.y + (target.y - arrivalOrigin.y) * travel + Math.cos(elapsed * 16) * 12 * weave;
                        vx = dt > 0 ? (x - oldX) / dt : 0;
                        vy = dt > 0 ? (y - oldY) / dt : 0;
                        // Turn upside down during the last .45s, finishing as the feet reach the bar.
                        bat.classList.toggle("halloween-bat--settling", arrivalProgress >= .85);
                    } else {
                        var dx = target.x - x;
                        var dy = target.y - y;
                        var distance = Math.hypot(dx, dy) || 1;
                        var wantedX = dx / distance * 520 + Math.sin(elapsed * 19) * 150;
                        var wantedY = dy / distance * 520 + Math.cos(elapsed * 23) * 130;
                        var easing = Math.min(1, dt * 9);
                        vx += (wantedX - vx) * easing;
                        vy += (wantedY - vy) * easing;
                        x += vx * dt;
                        y += vy * dt;
                    }
                    var area = bounds();
                    y = clamp(y, area.top, area.bottom);
                    // Arrival and departure must be able to cross the screen edge.
                    if (state === "frantic") {
                        x = clamp(x, area.left, area.right);
                    }
                    if (state === "arriving" && arrivalProgress >= 1) {
                        x = target.x;
                        y = target.y;
                        vx = vy = 0;
                        setState("hanging");
                    } else if (state === "departing" && (x < -WIDTH || x > window.innerWidth + WIDTH)) {
                        hideAway();
                    }
                }
            }

            // Also react if the bat lands near a stationary mouse pointer.
            if (mouse && canBeScared() && Math.hypot(x - mouse.x, y - mouse.y) < 75) {
                startFranticFlight();
            }
            render();
            frame = window.requestAnimationFrame(tick);
        }

        function resume() {
            if (frame !== null) {
                window.cancelAnimationFrame(frame);
                frame = null;
            }
            lastTime = null;
            refreshVisibility();
            if (!destroyed && !motion.matches && !document.hidden) {
                frame = window.requestAnimationFrame(tick);
            }
        }

        function forgetMouse() {
            mouse = null;
        }

        function destroy() {
            destroyed = true;
            window.cancelAnimationFrame(frame);
            document.removeEventListener("pointermove", scare);
            document.removeEventListener("pointerdown", scare);
            document.removeEventListener("pointerleave", forgetMouse);
            document.removeEventListener("visibilitychange", resume);
            window.removeEventListener("blur", forgetMouse);
            window.removeEventListener("pagehide", suspend);
            window.removeEventListener("pageshow", resume);
            motion.removeEventListener("change", resume);
            bat.remove();
            if (window.elsaHalloweenBat === companion) {
                delete window.elsaHalloweenBat;
            }
        }

        function suspend() {
            window.cancelAnimationFrame(frame);
            frame = null;
            lastTime = null;
        }

        var companion = { destroy: destroy };
        window.elsaHalloweenBat = companion;
        document.addEventListener("pointermove", scare, { passive: true });
        document.addEventListener("pointerdown", scare, { passive: true });
        document.addEventListener("pointerleave", forgetMouse);
        document.addEventListener("visibilitychange", resume);
        window.addEventListener("blur", forgetMouse);
        window.addEventListener("pagehide", suspend);
        window.addEventListener("pageshow", resume);
        motion.addEventListener("change", resume);
        setState("away");
        stateUntil = 3;
        resume();
    });
}());
