# Blackholio — Unity MMO (SpacetimeDB)

A real-time multiplayer game built with Unity and SpacetimeDB.
Players connect to a shared persistent world and interact in real time.

Built as a learning project during MCA Semester 1, University of Mumbai.

---

## Tech stack

| Layer | Technology |
|---|---|
| Game engine | Unity (URP) |
| Multiplayer backend | SpacetimeDB (local) |
| Server module language | C# (.NET 8, WASI) |
| Client language | C# |
| Version control | Git |

---

## Folder structure

```
blackholio/
├── Client-unity/       # Unity project (scenes, scripts, packages)
├── server-csharp/      # SpacetimeDB C# server module
│   └── spacetimedb/    # Lib.cs — tables and reducers go here
└── Docs/               # Project documentation
    ├── README.md
    ├── devlog.md
    ├── features.md
    └── bugs.md
```

---

## How to run locally

> Everything runs on your machine. No paid hosting needed.

### Prerequisites
- Unity installed (URP template)
- SpacetimeDB CLI installed (`spacetime` command works in terminal)
- .NET 8 SDK installed

### Steps

1. **Start SpacetimeDB and publish server module**
   ```bash
   spacetime start
   cd server-csharp
   spacetime publish --server local blackholio
   ```

2. **Generate C# client bindings in Unity**
   ```bash
   spacetime generate --lang csharp --out-dir ../Client-unity/Assets/SpacetimeDB
   ```

3. **Open Unity project**
   - Open Unity Hub → Add → select `Client-unity/` folder
   - Open `SampleScene`
   - Press Play

---

## Current status

- [x] Unity project created (URP)
- [x] SpacetimeDB Unity package installed via Package Manager
- [x] Game_Manager GameObject created with script + NetworkManager component
- [x] SpacetimeDB CLI installed
- [x] Server module initialized (`spacetime init --lang=csharp`)
- [x] Docs folder set up
- [x] Git initialized
- [x] Tables defined in `Lib.cs` (Config, Entity, Circle, Food, Player, DbVector2)
- [x] `Connect` reducer (client-connected lifecycle)
- [x] Client bindings generated (`Assets/SpacetimeDB/`)
- [x] Unity connects to local DB and subscribes to all tables
- [ ] Player spawning / movement in scene

---

**Author**

**Gur** — MCA Semester 1, University of Mumbai
Stack: Unity + SpacetimeDB (C#) | Started: 2026