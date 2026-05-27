# Feature tracker — Blackholio

Plan each feature before coding it. Check off tasks as you go.

---

## ✅ Project setup (complete)

**Status:** Done
**What it includes:**
- Unity project with URP and SpacetimeDB package
- Game_Manager object with NetworkManager
- SpacetimeDB CLI installed and server module initialized
- Git initialized, Docs folder created

---

## 🔲 Player spawning and movement

**Status:** In progress (following tutorial)
**Goal:** A player connects, spawns in the world, and can move around. Other connected players are visible.
**Done when:** Two Unity instances show each other moving in real time on localhost.

### Server tasks (server-csharp/spacetimedb/Lib.cs)
- [x] Define core tables (Entity, Circle, Food, Player, Config) — see `Lib.cs`
- [x] `Connect` reducer on client connect
- [ ] Write movement / split reducers (tutorial next steps)
- [ ] Write `UpdatePlayerPosition` reducer (or equivalent)

### Client tasks (Client-unity/)
- [x] Run `spacetime generate` → bindings in `Assets/SpacetimeDB/`
- [x] Connect to `127.0.0.1:3000` in `GameManager.cs` + `SpacetimeDBNetworkManager`
- [x] Subscribe to all tables on connect (`SubscribeToAllTables`)
- [ ] Subscribe to specific tables with row callbacks (spawn/update/delete)
- [ ] Spawn a player prefab on connect
- [ ] Send position updates every frame from `PlayerMovement.cs`
- [ ] Render other players from table subscription callbacks

### Notes
- `spacetime generate --lang csharp --out-dir ../Client-unity/Assets/SpacetimeDB`
- Never manually edit files inside `Assets/SpacetimeDB/*.g.cs` — they get overwritten on next generate
- See `Docs/bugs.md` for SDK version mismatches and `0.0.0.0` vs `127.0.0.1` connection issues

---

## 🔲 Combat system

**Status:** Planned
**Goal:** Players can attack each other and lose health.
**Done when:** Two players can reduce each other's HP to 0.

### Tasks
- [ ] Add `health` column to `PlayerComponent` table
- [ ] Write `AttackPlayer` reducer — validates range, applies damage
- [ ] Add attack input on client (Space key)
- [ ] Call reducer from `PlayerCombat.cs`
- [ ] Update health bar UI on subscription callback

### Notes
- Reducer name in C# bindings may differ slightly — check `autogen/Reducer.g.cs` for exact method name
- Start with simple distance check (within 2 units) before adding hitbox physics

---

## 🔲 Persistent XP and leveling

**Status:** Planned
**Goal:** Character XP and level survive disconnect and reconnect.
**Done when:** Log out, log back in, XP and level are the same.

### Tasks
- [ ] Add `xp` and `level` columns to `PlayerComponent`
- [ ] Write `GainXp` reducer with level-up logic
- [ ] Subscribe client to own row using Identity filter
- [ ] Show XP bar and level number in HUD

---

## 🔲 Enemy AI (server-simulated)

**Status:** Planned
**Goal:** Hostile NPCs exist in the world and chase/attack players.
**Done when:** Enemy spawns, patrols, chases player within range, deals damage.

### Tasks
- [ ] Define `EnemyComponent` table (id, position, health, state)
- [ ] Write `EnemyTick` reducer on a scheduled interval
- [ ] Implement FSM: Idle → Patrol → Chase → Attack
- [ ] Subscribe client to `EnemyComponent`, spawn and interpolate prefabs

---

## 🔲 Networked health bars and nameplates

**Status:** Planned
**Goal:** Every player shows name and HP bar above their character, visible to all clients.
**Done when:** Damage dealt to Player A is reflected on Player B's screen in real time.

### Tasks
- [ ] Create nameplate prefab (World Space Canvas, TextMeshPro, HP slider)
- [ ] Add billboard script (always faces camera)
- [ ] Bind HP value to subscription update callback

---

*(Add new features here as you plan them)*