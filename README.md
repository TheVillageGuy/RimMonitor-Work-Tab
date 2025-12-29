RimMonitor Work Tab

Standalone Work Tab implementation for RimWorld. This Readme was made by AI. As is this entire mod. 

It serves as an example of how RimMonitor's API can be used to add a mod to the simple HTTP server

The mod reconstructs the vanilla work-priority view from authoritative game data and exposes it through a single, immutable world-state snapshot, rendered both in-game and via RimMonitor.

The project is intentionally structured to separate game state, publication, and UI, and serves as a compact, real-world example of integrating a mod with RimMonitor’s API.

RimMonitor API integration

The Work Tab does not expose RimWorld state directly to the UI. Instead, it publishes a consolidated snapshot that is consumed by both the in-game tab and the web interface.

The integration follows these principles:

RimWorld remains the sole source of truth

All state is built outside the UI layer

HTTP routes never touch RimWorld APIs

One state model is shared across all frontends

Relevant files and responsibilities
State building & publication

These files are responsible for reading RimWorld data and producing the canonical state:

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
Fetches JSON from the RimMonitor endpoint and renders independently.
All layout and interaction logic lives outside game code.

Design constraints

No tick-based polling

Pause-safe updates

Explicit rebuild triggers

Minimal main-thread work

Deterministic, inspectable state flow
