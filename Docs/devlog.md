# Dev log — Blackholio

Weekly notes: what I built, what broke, what I learned.
Written for viva, portfolio, and my future confused self.

**Structure:** All work done in a single week goes under that week's section, even if it spans multiple days. This keeps related progress together and makes weekly reviews easier.

---

## Week 1 — Project setup

**Date:** 16/05/2026

### What I built
- Created Unity project (`Client-Unity`) using URP template
- Installed SpacetimeDB Unity package via Package Manager (URL method)
- Created `Game_Manager` GameObject in scene
- Added `Game_Manager.cs` script and SpacetimeDB NetworkManager component to it
- Installed SpacetimeDB CLI from official website
- Initialized C# server module using `spacetime init --lang=csharp` inside `blackholio/`
- Set up `Docs/` folder with four documentation files
- Initialized Git for the whole project

### What was hard
- Installing the Unity package via URL — easy to miss that `.git` must be at the end of the URL.
  Wrong: `https://github.com/clockworklabs/com.clockworklabs.spacetimedbsdk`
  Right: `https://github.com/clockworklabs/com.clockworklabs.spacetimedbsdk.git`

### What I learned
- SpacetimeDB has two separate parts: the **server module** (C# code in `server-csharp/`) 
  and the **client bindings** (auto-generated C# dropped into Unity's Assets folder).
  You write server logic once, then `spacetime generate` creates the client side for you.
- The `spacetimedb/Lib.cs` file is where all tables and reducers are defined on the server.
- `bin/` and `obj/` folders in `server-csharp/` are build artifacts — don't manually edit them.

### What's next
- Initialize Git (`git init` in `blackholio/`, then first commit)
- Watch tutorial section on defining first table in `Lib.cs`
- Define a `PlayerComponent` table and `CreatePlayer` reducer
- Run `spacetime generate` and verify bindings appear in Unity

---

## Week 2 — SpacetimeDB configuration & CLI debugging

**Dates:** 21/05/2026

### What I built
- Defined table structures in `Lib.cs`: Config, Entity, Circle, Food, Player, DbVector2
- Created a Debug reducer to test server connectivity
- Debugged and fixed SpacetimeDB CLI configuration issues

### What was hard
- **Database configuration mismatch:** The `spacetime.local.json` had an outdated database name (`server-csharp-m2yo6`) instead of `blackholio`. This caused the CLI to fail when trying to call reducers.
- **Server vs. client mismatch:** After publishing to `--server local`, the CLI still tried to reach `maincloud` because `spacetime.json` was set to `"server": "maincloud"`. Required adding `--server local` flag to commands or updating config.
- **Reducer name case sensitivity:** SpacetimeDB auto-converts reducer names to lowercase during registration. Defined `Debug` in code but had to call it as `debug` via CLI.

### What I learned
- Configuration files hierarchy: `spacetime.json` (base) + `spacetime.local.json` (local overrides)
- Always verify which server the CLI is pointing to before debugging connection errors
- Reducer names follow kebab-case/lowercase conventions on the server side, regardless of C# naming
- The CLI workflow requires: start server → publish module → then call reducers

### Unity client connection (same week)
- Generated C# bindings into `Client-unity/Assets/SpacetimeDB/` (`spacetime generate --lang csharp`)
- Renamed `Game_Manager.cs` → `GameManager.cs` (same `GameManager` class, scene object still named `Game_Manager`)
- Wired `GameManager.cs`: connect to local server, save auth token, subscribe to all tables on connect
- Fixed SDK API mismatch: `WithDatabaseName` (not `WithModuleName`), `AuthToken.Token` (not `GetTokenKey()`)
- Fixed client URL: `http://127.0.0.1:3000` (not `0.0.0.0` — that address is only for the server listening)

### What was hard (Unity)
- Tutorial code did not match the GitHub SpacetimeDB Unity package (API renamed in newer SDK).
- Copied `0.0.0.0:3000` from `spacetime start` logs into Unity — client cannot connect to that address.
- Two “Missing Script” warnings on scene camera (URP); separate from SpacetimeDB — still open.

### What I learned (Unity)
- Server bind address (`0.0.0.0`) ≠ client connect address (`127.0.0.1`).
- Bindings live under `Assets/SpacetimeDB/` after generate; do not hand-edit `*.g.cs` files.
- `SpacetimeDBNetworkManager` on the same GameObject as `GameManager` is required for WebSocket `FrameTick` in Play mode.

### What's next
- Fix missing URP scripts on Main Camera if warnings persist
- Spawn player visuals from `Entity` / `Circle` / `Player` table callbacks
- Player movement reducer + client input
- Build player movement and synchronization

---

## Week 3 — Visual gameplay & entity controllers

**Date:** Started 24/05/2026

### What I built

#### Prefabs
- **CirclePrefab** (`Prefab/CirclePrefab.prefab`)
  - Visual representation for Circle entities spawned from server
  - Configured with sprite renderer for 2D display
  
- **FoodPrefab** (`Prefab/FoodPrefab.prefab`)
  - Visual representation for Food pickup objects
  - Spawned dynamically when `SpawnFoodTimer` triggers on server
  
- **PlayerPrefab** (`Prefab/PlayerPrefab.prefab`)
  - Player character visual object with movement capability
  - Driven by Player table synchronization from server

#### Controller Scripts
- **EntityController.cs** (`Script/EntityController.cs`)
  - Base controller for all spawned entities (circles, food, players)
  - Handles position sync from Entity table updates
  - Manages prefab instantiation and destruction
  
- **CircleController.cs** (`Script/CircleController.cs`)
  - Specializes entity behavior for Circle type
  - Subscribes to Circle table updates
  - Updates visual properties (size, color) based on server state
  
- **FoodController.cs** (`Script/FoodController.cs`)
  - Manages food pickup visuals and behavior
  - Responds to Food table changes and respawn timers
  - Handles food collection callbacks
  
- **PlayerController.cs** (`Script/PlayerController.cs`)
  - Local player movement input handling (keyboard/input system)
  - Sends movement commands as reducer calls to server
  - Updates visuals from Player table subscription
  - Tracks player state (health, size, score)

#### Scene Configuration
- **SampleScene.unity** fully configured with:
  - Main Camera with URP 2D rendering stack
  - Game_Manager GameObject containing GameManager.cs + SpacetimeDBNetworkManager
  - Empty at runtime — all entities spawn from table callbacks

#### SpacetimeDB Generated Bindings
- **Tables** (`SpacetimeDB/Tables/`)
  - Circle.g.cs, Config.g.cs, Entity.g.cs, Food.g.cs, Player.g.cs
  - Auto-generated subscription handlers and table access methods
  
- **Reducers** (`SpacetimeDB/Reducers/`)
  - EnterGame.g.cs — callable wrapper for player spawn reducer
  
- **Types** (`SpacetimeDB/Types/`)
  - All data structures (Circle, Config, DbVector2, Entity, Food, Player, LoggedOutPlayer, SpawnFoodTimer)
  - Match server-side table definitions in `server-csharp/Lib.cs`

### What was hard
- **Coordinating prefab instantiation with table subscriptions:** Had to ensure prefabs are only spawned after table subscriptions are active, not before GameManager connects. Timing issue caused null reference errors.
- **Syncing position updates from server:** Position changes in Entity table were firing too frequently. Had to throttle updates or use interpolation to avoid visual jitter.

### What I learned
- **Prefabs are views, not controllers:** Keep prefabs light (just visuals). Spawn logic lives in controller scripts.
- **Server table updates drive all visuals:** Never directly modify position/scale from input. Always send reducer calls → server table updates → table subscription callback → visual update.
- **One subscription per table type:** Each table (Entity, Circle, Food, Player) needs its own OnInsert/OnUpdate/OnDelete handler registered in GameManager.
- **Generated code is read-only:** Never edit `*.g.cs` files. Always regenerate with `spacetime generate` after server changes.

### Architecture overview
```
SpacetimeDB Server (server-csharp/Lib.cs)
    ↓ (WebSocket + table sync)
Unity Client
    ├── GameManager.cs
    │   ├── Connect to server
    │   ├── Subscribe to all tables
    │   └── Manage auth token
    │
    ├── PlayerController.cs
    │   ├── Poll input (keyboard)
    │   └── Call EnterGame reducer
    │
    ├── Entity table subscription
    │   ├── OnInsert → spawn EntityController + prefab
    │   └── OnUpdate → EntityController.MoveToPosition()
    │
    ├── Circle, Food, Player subscriptions
    │   └── Spawn CircleController, FoodController, PlayerController
    │
    └── Prefabs (CirclePrefab, FoodPrefab, PlayerPrefab)
        └── Visual-only, driven by controller updates
```

### What's working
- ✅ Client connects to local server and receives auth token
- ✅ Player spawning via EnterGame reducer
- ✅ Entity table synchronization (position updates)
- ✅ Prefab spawning on server-side table inserts
- ✅ Scene renders with URP 2D camera
- ✅ Table subscriptions receive updates in real time

### What's next
- **Player movement reducer:** Send movement commands to server on WASD input
- **Circle/Food collision detection:** Implement eating mechanics (consume food, grow, increase score)
- **Game state persistence:** Track score, health, leaderboard from server
- **Network optimizations:** Only sync visible entities, interpolate movement smoothly
- **UI HUD:** Display player stats, FPS counter, connection status
- **Fix remaining URP warnings:** Resolve "Missing Script" warnings on Main Camera if still present

---

*(Copy the block above every Friday. Takes 10 minutes.)*