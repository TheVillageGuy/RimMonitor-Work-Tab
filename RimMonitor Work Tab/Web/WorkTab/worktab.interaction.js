/*
    worktab.interaction.js

    Owns:
    - All UI interaction (click/drag/contextmenu suppression)
    - Forwarding user intent to the server via sendPriority(...)

    Does NOT own:
    - Rendering DOM (worktab.render.js)
    - Polling/sync (worktab.render.js revision polling)

    Contract with render.js:
    Each interactive work cell must have:
      .workcell
      data-map-id
      data-pawn-id
      data-work-type-id
      data-priority
      data-row-index
      data-col-index
*/

(function () {
    "use strict";

    function getRoot() {
        return document.getElementById("worktab");
    }

    function closestWorkCell(el) {
        if (!el) return null;
        if (el.classList && el.classList.contains("workcell")) return el;
        if (!el.closest) return null;
        return el.closest(".workcell");
    }

    function isManualEnabled(root) {
        // Render toggles these classes.
        return !!(root && root.classList && root.classList.contains("manual-on"));
    }

    function parseIntSafe(s, fallback) {
        var n = parseInt(s, 10);
        return isFinite(n) ? n : fallback;
    }

    function cellKey(mapId, rowIndex, colIndex) {
        return mapId + "|" + rowIndex + "|" + colIndex;
    }

    function buildCellIndex(root) {
        var index = Object.create(null);
        var cells = root.querySelectorAll(".workcell[data-map-id][data-row-index][data-col-index]");
        for (var i = 0; i < cells.length; i++) {
            var c = cells[i];

            // Unavailable work cells are placeholders only; never interact / drag-write through them.
            if (c.classList && c.classList.contains("empty"))
                continue;

            var k = cellKey(c.dataset.mapId, c.dataset.rowIndex, c.dataset.colIndex);
            index[k] = c;
        }
        return index;
    }

    function computeNextPriority(current, manualEnabled, dir) {
        if (manualEnabled) {
            if (dir > 0) {
                // RIGHT click → increase: 0→1→2→3→4→0
                if (current === 0) return 1;
                if (current === 4) return 0;
                return current + 1;
            } else {
                // LEFT click → decrease: 0→4→3→2→1→0
                if (current === 0) return 4;
                if (current === 1) return 0;
                return current - 1;
            }
        }

        // Checkbox mode unchanged
        return (current > 0) ? 0 : 3;
    }

    function applyLocalPriority(cell, next, manualEnabled) {
        if (!cell) return;

        cell.dataset.priority = String(next);

        if (typeof applyPriorityVisual === "function") {
            applyPriorityVisual(cell, next, manualEnabled);
        }

        // Keep enabled/disabled classes consistent even if applyPriorityVisual changes later.
        var enabled = next > 0;
        cell.classList.toggle("enabled", enabled);
        cell.classList.toggle("disabled", !enabled);
    }

    function postPriority(cell, next) {
        if (typeof sendPriority !== "function") return;

        var mapId = parseIntSafe(cell.dataset.mapId, null);
        var pawnId = parseIntSafe(cell.dataset.pawnId, null);
        var workTypeId = cell.dataset.workTypeId;

        if (mapId === null || pawnId === null || !workTypeId) return;

        // Fire-and-forget; authoritative state will come through revision polling.
        try {
            sendPriority(mapId, pawnId, workTypeId, next);
        } catch (e) {
            // Ignore; polling will reconcile.
        }
    }

    // ------------------------------------------------------------
    // Dragging (vertical + horizontal)
    // ------------------------------------------------------------

    var dragging = false;
    var dragManualEnabled = false;
    var dragNextValue = 0;

    var startMapId = null;
    var startPawnId = null;
    var startWorkTypeId = null;
    var startRow = 0;
    var startCol = 0;

    var axis = null; // "vertical" | "horizontal"
    var cellIndex = null;

    var lastAppliedKey = null;

    function beginDrag(root, cell, dir) {
        if (!cell) return;

        if (cell.classList && cell.classList.contains("empty"))
            return;

        dragManualEnabled = isManualEnabled(root);

        startMapId = cell.dataset.mapId;
        startPawnId = cell.dataset.pawnId;
        startWorkTypeId = cell.dataset.workTypeId;
        startRow = parseIntSafe(cell.dataset.rowIndex, 0);
        startCol = parseIntSafe(cell.dataset.colIndex, 0);

        var current = parseIntSafe(cell.dataset.priority, 0);
        var next = computeNextPriority(current, dragManualEnabled, dir);
        if (next === null || next === undefined) return;

        dragNextValue = next;
        axis = null;
        lastAppliedKey = null;

        cellIndex = buildCellIndex(root);

        dragging = true;
        root.classList.add("dragging");

        applyLocalPriority(cell, dragNextValue, dragManualEnabled);
        postPriority(cell, dragNextValue);
        lastAppliedKey = cellKey(startMapId, String(startRow), String(startCol));
    }


    function endDrag(root) {
        if (!dragging) return;
        dragging = false;
        axis = null;
        cellIndex = null;
        lastAppliedKey = null;

        if (root) root.classList.remove("dragging");
    }

    function decideAxis(targetCell) {
        if (!targetCell) return null;

        // Prefer pure row/col alignment first.
        if (targetCell.dataset.mapId === startMapId && targetCell.dataset.workTypeId === startWorkTypeId && targetCell.dataset.pawnId !== startPawnId)
            return "vertical";

        if (targetCell.dataset.mapId === startMapId && targetCell.dataset.pawnId === startPawnId && targetCell.dataset.workTypeId !== startWorkTypeId)
            return "horizontal";

        // If both changed, choose based on row/col delta.
        var r = parseIntSafe(targetCell.dataset.rowIndex, startRow);
        var c = parseIntSafe(targetCell.dataset.colIndex, startCol);

        var dr = Math.abs(r - startRow);
        var dc = Math.abs(c - startCol);

        return (dr >= dc) ? "vertical" : "horizontal";
    }

    function applyDragToTarget(root, targetCell) {
        if (!dragging || !targetCell || !cellIndex) return;

        if (targetCell.dataset.mapId !== startMapId)
            return; // Do not drag across maps.

        if (!axis) {
            axis = decideAxis(targetCell);
        }

        var r = parseIntSafe(targetCell.dataset.rowIndex, startRow);
        var c = parseIntSafe(targetCell.dataset.colIndex, startCol);

        var min, max;

        if (axis === "vertical") {
            // same column only
            min = Math.min(startRow, r);
            max = Math.max(startRow, r);

            for (var rr = min; rr <= max; rr++) {
                var k = cellKey(startMapId, String(rr), String(startCol));
                if (k === lastAppliedKey) continue;
                var cell = cellIndex[k];
                if (!cell) continue;

                applyLocalPriority(cell, dragNextValue, dragManualEnabled);
                postPriority(cell, dragNextValue);
                lastAppliedKey = k;
            }
            return;
        }

        if (axis === "horizontal") {
            // same row only
            min = Math.min(startCol, c);
            max = Math.max(startCol, c);

            for (var cc = min; cc <= max; cc++) {
                var kk = cellKey(startMapId, String(startRow), String(cc));
                if (kk === lastAppliedKey) continue;
                var cell2 = cellIndex[kk];
                if (!cell2) continue;

                applyLocalPriority(cell2, dragNextValue, dragManualEnabled);
                postPriority(cell2, dragNextValue);
                lastAppliedKey = kk;
            }
            return;
        }
    }

    // ------------------------------------------------------------
    // Event wiring
    // ------------------------------------------------------------

    document.addEventListener("contextmenu", function (e) {
        var root = getRoot();
        if (!root) return;

        var cell = closestWorkCell(e.target);
        if (!cell) return;

        e.preventDefault();
    }, { passive: false });

    document.addEventListener("mousedown", function (e) {
        var root = getRoot();
        if (!root) return;

        var cell = closestWorkCell(e.target);
        if (!cell) return;

        // Only left/right
        if (e.button !== 0 && e.button !== 2)
            return;

        // Placeholders are non-interactive; pointer-events:none should block these,
        // but keep a hard guard.
        if (cell.classList && cell.classList.contains("empty"))
            return;

        e.preventDefault();

        // In checkbox mode, left and right are identical: begin drag either way.
        // In manual mode, same (we compute a single dragNextValue).
        beginDrag(root, cell, e.button === 2 ? +1 : -1);
    }, { passive: false });

    document.addEventListener("mouseup", function (e) {
        var root = getRoot();
        if (!root) return;

        if (dragging) {
            e.preventDefault();
            endDrag(root);
        }
    }, { passive: false });

    document.addEventListener("mousemove", function (e) {
        if (!dragging) return;

        var root = getRoot();
        if (!root) return;

        var cell = closestWorkCell(e.target);
        if (!cell) return;

        if (cell.classList && cell.classList.contains("empty"))
            return;

        applyDragToTarget(root, cell);
    }, { passive: true });

})();
