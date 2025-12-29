/*
    skills.js

    Owns:
    - skill → visual mapping
    - low-skill warning rule
    - border / glow semantics

    This logic is purely visual.
    It must never affect interaction or server state.
*/

function applySkillClass(cell, skillLevel, priority) {
    if (!cell)
        return;

    // Never apply skill visuals to unavailable cells
    if (cell.classList.contains("empty"))
        return;

    // Clear previous skill classes
    for (let i = 0; i <= 20; i++)
        cell.classList.remove("skill-" + i);

    cell.classList.remove("low-skill");

    // If no skill info, stop after clearing
    if (skillLevel === null || skillLevel === undefined)
        return;

    // Clamp defensively
    if (skillLevel < 0) skillLevel = 0;
    if (skillLevel > 20) skillLevel = 20;

    // Always apply skill class, regardless of priority
    cell.classList.add("skill-" + skillLevel);

    // Low-skill warning
    if (skillLevel <= 2 && priority > 0)
        cell.classList.add("low-skill");
}

function applyPassionClass(cell, passion) {
    if (!cell)
        return;

    // Clear previous passion state
    cell.classList.remove("passion-1", "passion-2");

    if (passion === 1) {
        cell.classList.add("passion-1");
    }
    else if (passion === 2) {
        cell.classList.add("passion-2");
    }
}

