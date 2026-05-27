# Bug Log — Blackholio

Log every bug here as it happens. Even one line helps.  
**Goal:** Never spend 3 hours rediscovering a fix you already found.

---

## 🚀 Critical: SpacetimeDB SDK incompatible with Unity 6

**Date:** Started 16/05/2026, resolved ~24/05/2026  
**Status:** Not Fixed(But can be FIXED via full project restart)

**What happened:** Initial project setup used Unity 6000.3.8f1 with SpacetimeDB SDK from GitHub. Project immediately filled with compile errors:
- `CS0246: The type or namespace name 'Identity' could not be found`
- `CS0246: The type or namespace name 'ConnectionId' could not be found`
- `CS0246: The type or namespace name 'IStructuralReadWrite' could not be found`
- Burst compiler cascade errors throughout the SDK source

Debugging led down a rabbit hole: SpacetimeDB server connection problems? Incompatible Burst settings? Missing IL2CPP dependencies? No—none of these were the issue.

**Expected:** Project compiles cleanly and connects to local SpacetimeDB server.

**Root cause:** **The SpacetimeDB SDK version available on GitHub was built for Unity 2022 LTS / 2023 LTS. It is not compatible with Unity 6 (2024+).** The error cascade started with missing type definitions in SpacetimeDB's backend, which triggered Burst to fail on dependent code. The newer SDK doesn't target modern Unity APIs.

**Fix:** Deleted the entire broken project. Restarted fresh:
1. Installed Unity `2022.3.60f1` (LTS) instead of Unity 6
2. Installed SpacetimeDB SDK `2.2.0` (compatible version)
3. Recreated the project from scratch in a clean environment

**Result:** All compile errors disappeared immediately. Clean project compiles.

**Lesson:** Always verify SDK/engine version compatibility before deep integration work. GitHub "latest" isn't always the latest—check release notes for target platform support. In this case, recovery was fast because the project was young (mostly scaffolding), but catching this earlier would have saved debugging time.

---

## ✅ Bug: SpacetimeDB Unity package failed to install

**Date:** 16/05/2026  
**Status:** Fixed

**What happened:** Pasted the GitHub URL into Package Manager → nothing installed or threw an error.

**Expected:** Package installs successfully.

**Root cause:** Unity's Package Manager requires Git URLs to end with `.git`.

**Fix:** Used this exact URL format:
```
https://github.com/clockworklabs/com.clockworklabs.spacetimedbsdk.git
```

---

## ✅ Bug: Failed to find database `server-csharp-m2yo6`

**Date:** 21/05/2026  
**Status:** Fixed

**What happened:** Ran `spacetime call blackholio Debug` and got error:
```
Error: failed to find database 'server-csharp-m2yo6'
```

**Expected:** Command should connect to the `blackholio` database and call the Debug reducer.

**Root cause:** The `spacetime.local.json` file contained an outdated database name (`server-csharp-m2yo6`) instead of the actual database name (`blackholio`). The CLI reads this config file to determine which database to connect to.

**Fix:** Updated `spacetime.local.json` to:
```json
{
  "database": "blackholio"
}
```

---

## ✅ Bug: HTTP 404 error — No such database on maincloud

**Date:** 21/05/2026  
**Status:** Fixed

**What happened:** After fixing the database name, ran `spacetime call blackholio Debug` and got:
```
Error: No such database. HTTP status client error (404 Not Found) 
for url (https://maincloud.spacetimedb.com/...)
```

**Expected:** Command should connect to the local database and call the Debug reducer.

**Root cause:** `spacetime.json` had `"server": "maincloud"` which instructed the CLI to reach the cloud server (https://maincloud.spacetimedb.com) instead of the local server. The database was published to `--server local`, but the CLI was configured to hit the cloud.

**Fix:** Added `--server local` flag to the command:
```bash
spacetime call --server local blackholio Debug
```

Alternative: Update `spacetime.json` to `"server": "local"` for local development.

---

## ✅ Bug: No such reducer `Debug` — case sensitivity issue

**Date:** 21/05/2026  
**Status:** Fixed

**What happened:** Ran `spacetime call --server local blackholio Debug` and got:
```
Error: No such reducer OR procedure 'Debug' for database 'blackholio'. 
A reducer with a similar name exists: 'debug'
```

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

**Root cause:** **Version / API mismatch.** The tutorial was written for an older SpacetimeDB C# SDK. The installed package renamed `WithModuleName` → `WithDatabaseName`. The code also called `AuthToken.GetTokenKey()`, but in the Unity build, that method is **private**—the public API is `AuthToken.Token` and `AuthToken.SaveToken()`.

**Fix:**
- Replaced `.WithModuleName(MODULE_NAME)` with `.WithDatabaseName(MODULE_NAME)`
- Replaced `PlayerPrefs.HasKey(AuthToken.GetTokenKey())` with `!string.IsNullOrEmpty(AuthToken.Token)`
- Removed invalid `using SpacetimeDB.Auth;` (that namespace does not exist)

---

## ✅ Bug: Invalid `using SpacetimeDB.Auth`

**Date:** 21/05/2026  
**Status:** Fixed

**What happened:** Added `using SpacetimeDB.Auth;` while trying to fix the `AuthToken` errors.

**Expected:** Compiler finds `AuthToken` and related types.

**Root cause:** There is no `SpacetimeDB.Auth` namespace in the SDK. `AuthToken` lives in the root `SpacetimeDB` namespace, which was already imported.

**Fix:** Deleted the extraneous `using` line.

---

## ✅ Bug: WebSocket failed — client connected to `0.0.0.0:3000`

**Date:** 21/05/2026  
**Status:** Fixed

**What happened:** Unity compiled, but on Play the console showed:
```
SpacetimeDBClient: Connecting to ws://0.0.0.0:3000 blackholio
Failed to connect to SpacetimeDB: ... Addresses 0.0.0.0 (IPv4) and ::0 (IPv6) 
are unspecified addresses. You cannot use them as target address.
```

**Expected:** Client connects to the local SpacetimeDB server started with `spacetime start`.

**Root cause:** **Confused server bind address with client connect address.** When the server starts, it logs `listening on 0.0.0.0:3000` — that means the **server** accepts connections on all interfaces. A **client** cannot connect *to* `0.0.0.0`; .NET/.NET Framework rejects it as invalid. I copied `http://0.0.0.0:3000` into `SERVER_URL` in `GameManager.cs` because it matched the terminal output.

**Fix:** Changed `SERVER_URL` to `http://127.0.0.1:3000` (localhost). Keep the workflow:
```bash
spacetime start
spacetime publish --server local blackholio
```
Then press Play in Unity.

---

## ⚠️ Bug: "The referenced script (Unknown) on this Behaviour is missing!" (×2)

**Date:** 21/05/2026  
**Status:** Open / unrelated to SpacetimeDB connection

**What happened:** Unity showed two warnings about missing scripts on scene objects when entering Play mode. These warnings appeared alongside SpacetimeDB connection errors, creating confusion.

**Expected:** No missing-script warnings in the Console.

**Root cause:** **Likely unrelated to SpacetimeDB.** Missing script references in `SampleScene` commonly occur when:
- **URP camera / 2D light** components (e.g., Universal Additional Camera Data) are out of sync with the Render Pipeline package
- Scripts are renamed (e.g., `Game_Manager` → `GameManager`) and scene GUIDs become stale
- The object still references an old component that was deleted

**Fix (to try):**
1. Select **Main Camera** and any object showing "Missing Script"
2. Remove the missing component
3. Re-add the correct URP component from the Inspector
4. Verify **Project Settings → Graphics** has URP assigned to the Render Pipeline Asset
5. Re-attach `GameManager` script if that component shows Missing

---

## Known SpacetimeDB Gotchas

### Reducer name mismatch
**Symptom:** Calling a reducer from C# does nothing. No error, no server log.

**Cause:** The auto-generated bindings in `Assets/autogen/Reducer.g.cs` wrap reducer names in a specific way. Always check the exact method name there—don't guess from your C# server code.

### Client not receiving table updates
**Symptom:** Table changes on server but Unity UI doesn't update.

**Cause:** You must explicitly subscribe to each table you want to listen to. Subscribing to one table does NOT auto-subscribe related ones.

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

### Tutorial code vs installed SDK
**Symptom:** `WithModuleName`, `GetTokenKey`, or `SpacetimeDB.Auth` do not compile.

**Cause:** Tutorial/docs written for an older client API; newer SDK uses `WithDatabaseName` and `AuthToken.Token`.

**Fix:** Match the installed SDK in `Library/PackageCache/com.clockworklabs.spacetimedbsdk/.../src/`, not an old blog snippet.

### Client URL must not be `0.0.0.0`
**Symptom:** WebSocket error about "unspecified addresses" / `hostNameOrAddress` when connecting.

**Cause:** `0.0.0.0` is for **server bind**, not for **client connect**.

**Fix:** Use `http://127.0.0.1:3000` or `http://localhost:3000` in `GameManager.cs`.

### SDK version compatibility with Unity
**Symptom:** Compilation fails with type errors like `Identity`, `ConnectionId`, `IStructuralReadWrite` not found.

**Cause:** SpacetimeDB SDK on GitHub targets older Unity LTS versions (2022 / 2023). Unity 6 (2024+) is not yet supported by older SDK versions.

**Fix:** Use a compatible LTS version (Unity 2022.3 LTS or 2023 LTS) with the available SDK, or wait for SDK updates targeting newer Unity versions.

---