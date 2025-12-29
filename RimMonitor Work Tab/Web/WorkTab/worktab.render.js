/*
    worktab.render.js

    Fully data-driven renderer.

    Responsibilities:
    - Build the complete WorkTab grid from __WORKTAB_STATE__
    - Apply visual classes only (no authoritative state mutation)
    - Remain stable across refreshes (no flicker / no flashing)
*/

(function () {

    function renderWorkTab(state) {
        if (!state || !state.maps) return;

        const root = document.getElementById("worktab");
        if (!root) return;

        // Fully JS-driven: wipe any template markup.
        root.innerHTML = "";

        const manualEnabled = !!(state.manualPrioritiesEnabled || state.ManualPrioritiesEnabled);

        root.classList.toggle("manual-on", manualEnabled);
        root.classList.toggle("manual-off", !manualEnabled);

        // WorkTypes are required for canonical ordering.
        const workTypes = state.workTypes || state.WorkTypes || [];
        const showMapTitles = state.maps.length > 1;

        console.log(
            "WORKTYPES AT LOAD:",
            workTypes.map(w => w.id || w.Id).join(" | ")
        );

        function div(className) {
            const d = document.createElement("div");
            if (className) d.className = className;
            return d;
        }

        function getCellState(pawn, workTypeId) {
            if (!pawn) return null;

            // JSON uses "cellsByWorkType"
            const dict = pawn.cellsByWorkType || pawn.CellsByWorkType;
            if (!dict) return null;

            return dict[workTypeId] || null;
        }

        function getBool(cellState, camel, pascal) {
            if (!cellState) return false;
            if (cellState[camel] === true) return true;
            if (cellState[pascal] === true) return true;
            return false;
        }

        function getInt(cellState, camel, pascal) {
            if (!cellState) return null;
            const v = (cellState[camel] !== undefined) ? cellState[camel] : cellState[pascal];
            if (v === undefined || v === null) return null;
            const n = Number(v);
            return Number.isFinite(n) ? n : null;
        }

        function getFloat(cellState, camel, pascal) {
            if (!cellState) return null;
            const v = (cellState[camel] !== undefined) ? cellState[camel] : cellState[pascal];
            if (v === undefined || v === null) return null;
            const n = Number(v);
            return Number.isFinite(n) ? n : null;
        }

        function buildHeader() {
            const header = div("worktab-header");

            const manual = div("manual-priorities");

            const manualLabel = document.createElement("span");
            manualLabel.className = "label";
            manualLabel.textContent = "Manual priorities";
            manual.appendChild(manualLabel);

            const manualState = document.createElement("span");
            manualState.className = "state " + (manualEnabled ? "on" : "off");
            manual.appendChild(manualState);

            header.appendChild(manual);

            const headers = div("worktype-headers");

            for (let i = 0; i < workTypes.length; i++) {
                const wt = workTypes[i];

                const h = div("worktype-header");
                h.textContent = wt.shortLabel || wt.ShortLabel || "";
                h.title = wt.label || wt.Label || "";

                h.classList.toggle("stagger-top", (i % 2) === 0);
                h.classList.toggle("stagger-bottom", (i % 2) === 1);

                headers.appendChild(h);
            }

            header.appendChild(headers);
            return header;
        }

        function buildEmptyCell(map, pawn, workTypeId, rowIndex, colIndex) {
            const cell = div("workcell empty");

            // Keep row/col indices so layout stays aligned.
            // Do NOT make it interactable: CSS .empty has pointer-events:none.
            cell.dataset.mapId = String(map.mapId);
            cell.dataset.pawnId = String(pawn.pawnThingId);
            cell.dataset.workTypeId = String(workTypeId);
            cell.dataset.priority = "0";
            cell.dataset.rowIndex = String(rowIndex);
            cell.dataset.colIndex = String(colIndex);

            return cell;
        }

        function buildRow(map, pawn, rowIndex) {
            const row = div("colonist-row");
            row.dataset.pawnId = pawn.pawnThingId;

            const ident = div("colonist-identity");

            const name = document.createElement("span");
            name.className = "name";
            name.textContent = pawn.name || "";
            ident.appendChild(name);

            row.appendChild(ident);

            const cells = div("workcells");

            for (let i = 0; i < workTypes.length; i++) {
                const wt = workTypes[i];
                const workTypeId = wt.id || wt.Id;

                const cellState = getCellState(pawn, workTypeId);

                // Unavailable work type: render empty placeholder cell
                if (!cellState || cellState.available === false) {
                    cells.appendChild(buildEmptyCell(map, pawn, workTypeId, rowIndex, i));
                    continue;
                }

                const cell = div("workcell");

                const prioritySpan = document.createElement("span");
                prioritySpan.className = "priority";
                cell.appendChild(prioritySpan);

                const priority = getInt(cellState, "priority", "Priority") || 0;

                cell.dataset.mapId = String(map.mapId);
                cell.dataset.pawnId = String(pawn.pawnThingId);
                cell.dataset.workTypeId = String(workTypeId);
                cell.dataset.priority = String(priority);
                cell.dataset.rowIndex = String(rowIndex);
                cell.dataset.colIndex = String(i);

                // If an explicit disabled flag exists, respect it.
                // Otherwise treat priority==0 as disabled.
                const isDisabled = getBool(cellState, "isDisabled", "IsDisabled") || (priority <= 0);

                const ideologyOpposed =
                    getBool(cellState, "ideologyOpposed", "IdeologyOpposed") ||
                    getBool(cellState, "isIdeologyOpposed", "IsIdeologyOpposed");

                cell.classList.toggle("disabled", isDisabled);
                cell.classList.toggle("enabled", !isDisabled);
                cell.classList.toggle("ideo-opposed", ideologyOpposed);

                // Activity enrichment (if present)
                const activityScore = getFloat(cellState, "activityScore", "ActivityScore");
                const recentlyActive = getBool(cellState, "recentlyActive", "RecentlyActive");

                if (recentlyActive) cell.classList.add("activity-recent");
                if (activityScore !== null) cell.classList.toggle("activity-high", activityScore >= 0.6);

                const skillLevel = getInt(cellState, "skillLevel", "SkillLevel");
                const passion = getInt(cellState, "passion", "Passion");

                if (typeof applySkillClass === "function") {
                    applySkillClass(cell, skillLevel, priority);
                }

                if (typeof applyPassionClass === "function") {
                    applyPassionClass(cell, passion);
                }

                if (typeof applyPriorityVisual === "function") {
                    applyPriorityVisual(cell, priority, manualEnabled);
                }

                cells.appendChild(cell);
            }

            row.appendChild(cells);
            return row;
        }

        for (let m = 0; m < state.maps.length; m++) {
            const map = state.maps[m];
            const mapEl = div("worktab-map");

            if (showMapTitles) {
                const title = div("worktab-map-title");
                title.textContent = map.mapLabel || "";
                mapEl.appendChild(title);
            }

            mapEl.appendChild(buildHeader());

            const body = div("worktab-body");
            for (let p = 0; p < map.pawns.length; p++) {
                body.appendChild(buildRow(map, map.pawns[p], p));
            }

            mapEl.appendChild(body);
            root.appendChild(mapEl);
        }
    }

    // Initial render (same behavior as before)
    renderWorkTab(window.__WORKTAB_STATE__);

    // =========================
    // Revision polling (new)
    // =========================

    let lastRevision = -1;

    let pollDelay = 500;
    const MIN_DELAY = 100;
    const MAX_DELAY = 500;
    const STEP_DELAY = 50;

    let lastChangeTime = 0;

    function adjustPollDelay() {
        if (pollDelay >= MAX_DELAY)
            return;

        const elapsed = Date.now() - lastChangeTime;
        if (elapsed >= 1000) {
            pollDelay = Math.min(MAX_DELAY, pollDelay + STEP_DELAY);
            lastChangeTime = Date.now();
        }
    }

    function pollRevision() {
        fetch("/mod/worktab/revision")
            .then(r => r.json())
            .then(data => {
                const rev = data.revision;
                if (rev !== lastRevision) {
                    lastRevision = rev;
                    pollDelay = MIN_DELAY;
                    lastChangeTime = Date.now();

                    fetch("/mod/worktab/state")
                        .then(r => r.json())
                        .then(state => {
                            renderWorkTab(state);
                        });
                }
            })
            .catch(err => {
                console.error("[WorkTab] poll error", err);
            })
            .finally(() => {
                adjustPollDelay();
                setTimeout(pollRevision, pollDelay);
            });
    }

    pollRevision();

})();
