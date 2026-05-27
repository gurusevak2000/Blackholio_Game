# Bug log — Blackholio

Log every bug here as it happens. Even one line helps.
Goal: never spend 3 hours rediscovering a fix you already found.

---

## ✅ Bug: SpacetimeDB Unity package failed to install

**Date:** 16/05/2026
**Status:** Fixed

**What happened:** Pasted the GitHub URL into Package Manager → nothing installed or threw an error.
**Expected:** Package installs successfully.
**Root cause:** Unity's Package Manager requires Git URLs to end with `.git`.
**Fix:** Used this exact URL:
`https://github.com/clockworklabs/com.clockworklabs.spacetimedbsdk.git`

---

## Known SpacetimeDB gotchas — learn these before hitting them

### Reducer name mismatch
**Symptom:** Calling a reducer from C# does nothing. No error, no server log.
**Cause:** The auto-generated bindings in `Assets/autogen/Reducer.g.cs` wrap reducer names
in a specific way. Always look up the exact method name there — don't guess from your C# server code.

### Client not receiving table updates
**Symptom:** Table changes on server but Unity UI doesn't update.
**Cause:** You must explicitly subscribe to each table you want to listen to.
Subscribing to one table does NOT auto-subscribe related ones.
**Fix:** Call the subscribe method for every table you need in `Game_Manager.cs` on connect.

### SpacetimeDB server not found when pressing Play
**Symptom:** Unity console shows a connection error immediately on Play.
**Cause 1:** Forgot to run `spacetime start` before opening Unity.
**Cause 2:** Module not published after a code change. Must re-run:
```bash
spacetime publish --server local blackholio
```
**Rule:** Always: start server → publish module → then press Play in Unity.

### Client bindings are stale after changing server tables
**Symptom:** Unity compiler errors in `autogen/` after adding a new table or reducer.
**Cause:** Bindings were not regenerated after the server module changed.
**Fix:** Re-run generate command every time you change `Lib.cs`:
```bash
spacetime generate --lang=csharp --out-dir ../Client-Unity/Assets/autogen
```

---


## ✅ Bug: Failed to find database `server-csharp-m2yo6`

**Date:** 21/05/2026
**Status:** Fixed

**What happened:** Ran `spacetime call blackholio Debug` and got error: `Error: failed to find database 'server-csharp-m2yo6'`.
**Expected:** Command should connect to the `blackholio` database and call the Debug reducer.
**Root cause:** The `spacetime.local.json` file had an outdated database name (`server-csharp-m2yo6`) instead of the actual database name (`blackholio`). The CLI reads this config file to determine which database to connect to.
**Fix:** Updated `spacetime.local.json` to:
```json
{
  "database": "blackholio"
}
```

---

## ✅ Bug: HTTP 404 error - No such database on maincloud

**Date:** 21/05/2026
**Status:** Fixed

**What happened:** After fixing the database name, ran `spacetime call blackholio Debug` and got: `Error: No such database. HTTP status client error (404 Not Found) for url (https://maincloud.spacetimedb.com/...)`.
**Expected:** Command should connect to the local database and call the Debug reducer.
**Root cause:** `spacetime.json` had `"server": "maincloud"` which made the CLI try to reach the cloud server (https://maincloud.spacetimedb.com) instead of the local server. The database was published to local (`--server local`) but the CLI was configured to hit the cloud.
**Fix:** Added `--server local` flag to the command:
```bash
spacetime call --server local blackholio Debug
```
Alternative: Update `spacetime.json` to `"server": "local"` for local development.

---

## ✅ Bug: No such reducer `Debug` - case sensitivity issue

**Date:** 21/05/2026
**Status:** Fixed

**What happened:** Ran `spacetime call --server local blackholio Debug` and got: `Error: No such reducer OR procedure 'Debug' for database 'blackholio'. A reducer with a similar name exists: 'debug'`.
**Expected:** The Debug reducer should be called successfully.
**Root cause:** Reducer names in `Lib.cs` are defined with capital letters (e.g., `Debug`), but SpacetimeDB CLI automatically converts reducer names to lowercase when registering them. The command was using the wrong case.
**Fix:** Called the reducer with lowercase name:
```bash
spacetime call --server local blackholio debug
```

---

## ✅ Bug: Unity compile errors — `WithModuleName` and `AuthToken.GetTokenKey`

**Date:** 21/05/2026
**Status:** Fixed

**What happened:** After adding `GameManager.cs` from the SpacetimeDB Unity tutorial, Unity showed:
- `CS1061: 'DbConnectionBuilder<DbConnection>' does not contain a definition for 'WithModuleName'`
- `CS0117: 'AuthToken' does not contain a definition for 'GetTokenKey'`
**Expected:** Project compiles and connects using the tutorial connection code.
**Root cause:** **Version / API mismatch on my side.** The tutorial was written for an older SpacetimeDB C# SDK. The installed package (`com.clockworklabs.spacetimedbsdk` from GitHub) renamed `WithModuleName` → `WithDatabaseName`. I also copied code that called `AuthToken.GetTokenKey()`, but in the Unity build of the SDK that method is **private** — the public API is `AuthToken.Token` and `AuthToken.SaveToken()`.
**Fix:**
- Replaced `.WithModuleName(MODULE_NAME)` with `.WithDatabaseName(MODULE_NAME)`.
- Replaced `PlayerPrefs.HasKey(AuthToken.GetTokenKey())` with `!string.IsNullOrEmpty(AuthToken.Token)`.
- Removed invalid `using SpacetimeDB.Auth;` (that namespace does not exist).

---

## ✅ Bug: Invalid `using SpacetimeDB.Auth`

**Date:** 21/05/2026
**Status:** Fixed

**What happened:** Added `using SpacetimeDB.Auth;` while trying to fix the `AuthToken` errors.
**Expected:** Compiler finds `AuthToken` and related types.
**Root cause:** **My mistake while guessing fixes.** There is no `SpacetimeDB.Auth` namespace in the SDK. `AuthToken` lives in `SpacetimeDB`, which was already imported.
**Fix:** Deleted the extra `using` line.

---

## ✅ Bug: WebSocket failed — client connected to `0.0.0.0:3000`

**Date:** 21/05/2026
**Status:** Fixed

**What happened:** Unity compiled, but on Play the console showed:
- `SpacetimeDBClient: Connecting to ws://0.0.0.0:3000 blackholio`
- `Failed to connect to SpacetimeDB: ... Addresses 0.0.0.0 (IPv4) and ::0 (IPv6) are unspecified addresses. You cannot use them as target address.`
**Expected:** Client connects to the local SpacetimeDB server started with `spacetime start`.
**Root cause:** **Confused server address with client address.** `spacetime start` logs `listening on 0.0.0.0:3000` — that means the **server** accepts connections on all interfaces. Unity is a **client** and cannot connect *to* `0.0.0.0`; .NET rejects it. I copied `http://0.0.0.0:3000` into `SERVER_URL` in `GameManager.cs` because it matched the terminal output.
**Fix:** Changed `SERVER_URL` to `http://127.0.0.1:3000` (same machine as `spacetime start`). Keep server running and publish module before Play:
```bash
spacetime start
spacetime publish --server local blackholio
```

---

## ⚠️ Bug: “The referenced script (Unknown) on this Behaviour is missing!” (×2)

**Date:** 21/05/2026
**Status:** Open / separate from SpacetimeDB connection

**What happened:** Unity showed two warnings about missing scripts on scene objects when entering Play mode. SpacetimeDB connection errors appeared at the same time, so it looked like one big failure.
**Expected:** No missing-script warnings in the Console.
**Root cause:** **Likely unrelated to SpacetimeDB.** In `SampleScene`, missing references are often on **URP camera / 2D light** components (e.g. Universal Additional Camera Data) when the render pipeline package or scene template is out of sync — not on `GameManager` (which still ran and attempted the WebSocket connect). May also happen after renaming scripts (`Game_Manager` vs `GameManager`) if scene GUIDs are stale.
**Fix (to try):** Select **Main Camera** (and any object with “Missing Script”) → remove missing component → re-add the correct URP component, or fix **Project Settings → Graphics** so URP is assigned. Re-attach `GameManager` on the `Game_Manager` object if that component shows Missing.

---

## Known SpacetimeDB gotchas — SDK version vs tutorial (added 21/05/2026)

### Tutorial code vs installed SDK
**Symptom:** `WithModuleName`, `GetTokenKey`, or `SpacetimeDB.Auth` do not compile.
**Cause:** Tutorial/docs written for an older client API; GitHub package uses `WithDatabaseName` and Unity `AuthToken.Token`.
**Fix:** Match the installed SDK in `Library/PackageCache/com.clockworklabs.spacetimedbsdk/.../src/`, not an old blog snippet.

### Client URL must not be `0.0.0.0`
**Symptom:** WebSocket error about “unspecified addresses” / `hostNameOrAddress` when connecting.
**Cause:** `0.0.0.0` is for **server bind**, not for **client connect**.
**Fix:** Use `http://127.0.0.1:3000` or `http://localhost:3000` in `GameManager.cs`.

--- 