/*
    priority.js

    Owns:
    - priority cycling logic
    - forwarding user intent to the server
*/

function nextPriority(current) {
    return current === 0 ? 4 :
        current === 1 ? 0 :
            current - 1;
}

function applyPriorityVisual(cell, priority, manualEnabled) {
    if (!cell)
        return;

    const enabled = priority > 0;

    cell.classList.toggle("enabled", enabled);
    cell.classList.toggle("disabled", !enabled);

    const p = cell.querySelector(".priority");
    if (p) {
        if (manualEnabled) {
            p.textContent = (priority > 0) ? String(priority) : "";
        } else {
            p.textContent = "";
        }
    }
}


function sendPriority(mapId, pawnId, workType, priority) {
    return fetch("/mod/worktab/setpriority", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            mapId: mapId,
            pawnId: pawnId,
            workType: workType,
            priority: priority
        })
    });
}
