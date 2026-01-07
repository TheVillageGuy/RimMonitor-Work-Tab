RimMonitor Work Tab

Browser UI for RimWorld's native work tab

This mod is intended to showcase how, if you'd like to add your mod to RimMonitor, it's just a matter of registering it with RimMonitor via its API (a piece of cake) and making an HTML UI for it (not that much work with the help of AI). As you already have all the information you need from your working mod, there's no need to rewrite anything, just add a layer which makes everything ready to be parsed by the JavaScript in your UI. You can reuse everything in this mod as well. If your mod doesn't run its own worker thread, you can use this as an example as well, even if you're not planning to add it to RimMonitor

Having said that, this entire mod was generated with AI. You can feel the vibe by just looking at the direcory structure. So don't use it as a reference if you want to build a completely new mod. Or do, it's up to you. My point being that there's probably a lot that can be improved and if you feel the urge I invite you to do so by making a PR

The Work Tab mod does not expose RimWorld state directly to the UI. Instead, it publishes a snapshot that is consumed by the web UI

Note: This mod does not yet use the data published by RimMonitor, but later versions will. You don't have to use any of it, it's just some statistics gathered by tracking pawns which may allow you to make some cool visual effects in your UI, but I haven't yet done so myself.

The following part of this readme was made by AI. As is this entire mod. More information;

It serves as an example of how RimMonitor's API can be used to add a mod to the simple HTTP server

The mod reconstructs the vanilla work-priority view from authoritative game data and exposes it through a single, immutable world-state snapshot, rendered both in-game and via RimMonitor

The project is intentionally structured to separate game state, publication, and UI, and serves as a compact, real-world example of integrating a mod with RimMonitor’s API

The integration follows these principles:

RimWorld remains the sole source of truth

All state is built outside the UI layer

HTTP routes never touch RimWorld APIs

Relevant files and responsibilities
State building & publication

These files in this mod are responsible for reading RimWorld data and producing the canonical state:

WorkTabWorldState
Immutable data model containing all work-related information (pawns, work types, priorities, manual mode).

WorkTabStateBuilder (or equivalent builder class)
Reads RimWorld state and constructs a WorkTabWorldState.
Runs off the main thread and performs no UI work.

State publication logic
Publishes the latest built snapshot and revision counter for consumers.
The UI never queries RimWorld directly.

RimMonitor API surface

These files connect the Work Tab to RimMonitor:

Router classes (e.g. WorkTabRouter)
Register HTTP endpoints with RimMonitor.
Serve:

JSON state snapshots

static HTML/CSS/JS assets

State endpoint (/state or equivalent)
Serializes the current WorkTabWorldState to JSON.
Returns safe defaults during early load or rebuilds.

No RimWorld calls are allowed in this layer.

UI layers

These consume the published state:

In-game Work Tab UI
Renders directly from WorkTabWorldState.

Web UI (HTML / CSS / JS)
All layout and interaction logic lives outside game code. 

Design constraints

No tick-based polling

Pause-safe updates

Explicit rebuild triggers

Minimal main-thread work

Deterministic, inspectable state flow
