(function () {
    "use strict";

    // Prevent adding the idle timer more than once.
    if (window.marketLineIdleTimerInstalled) {
        return;
    }

    window.marketLineIdleTimerInstalled = true;

    // 2 minutes = 120,000 milliseconds
    const IDLE_TIMEOUT = 120000;
    const IDLE_PAGE = "/Idle";

    // Do not run the idle timer while already on Idle Page.
    const currentPath = window.location.pathname
        .replace(/\/+$/, "")
        .toLowerCase();

    if (currentPath === "/idle") {
        return;
    }

    let idleTimer;

    function goToIdlePage() {
        // replace prevents cashier from using browser Back button
        // to return to the exact previous work page after inactivity.
        window.location.replace(IDLE_PAGE);
    }

    function restartIdleTimer() {
        window.clearTimeout(idleTimer);

        idleTimer = window.setTimeout(() => {
            goToIdlePage();
        }, IDLE_TIMEOUT);
    }

    // Actions that mean the cashier is currently active.
    const activityEvents = [
        "mousemove",
        "mousedown",
        "pointerdown",
        "touchstart",
        "keydown",
        "scroll",
        "click",
        "input",
        "change"
    ];

    activityEvents.forEach(eventName => {
        document.addEventListener(eventName, restartIdleTimer, {
            passive: true
        });
    });

    // When a page comes from browser cache, restart its two-minute timer.
    window.addEventListener("pageshow", restartIdleTimer);

    // Start timer when page loads.
    restartIdleTimer();
})();