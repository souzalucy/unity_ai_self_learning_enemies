# Building SelfLearningEnemies as a Unity DLL Plugin

This folder compiles the project's C# source into **precompiled DLLs** that you
can drop into any Unity project instead of shipping the raw `.cs` files. Use this
when the code is stable and you want a closed-source / black-box plugin, or you
don't want consumers editing the source.

## What gets built

| Project | Output DLL | Contents |
|---------|-----------|----------|
| `SelfLearningEnemies.csproj` | `SelfLearningEnemies.dll` | Runtime: `Core/`, `Actions/`, `Observations/`, `Rewards/` |
| `SelfLearningEnemies.Editor.csproj` | `SelfLearningEnemies.Editor.dll` | Editor inspector: `Editor/*.cs` (tests excluded) |

`Editor/Tests/` is intentionally excluded — tests do not ship.

## Prerequisites

1. **.NET SDK 6.0+** (`dotnet --version`).
2. A **Unity 2021.3+ install** (for the `UnityEngine`/`UnityEditor` managed DLLs).
3. A **Unity project with `com.unity.ml-agents` installed** (Release 21), **opened
   at least once** so Unity generates the ML-Agents assemblies under
   `Library/ScriptAssemblies/`. ML-Agents is *not* on NuGet, so these DLLs must
   come from a real Unity project.

## 1. Configure the two paths

Edit `Directory.Build.props` (or pass them per-build with `-p:`):

- `UnityManaged` — path to `.../Editor/Data/Managed` of your Unity install.
- `UnityProject` — path to the Unity project that has ML-Agents installed.

## 2. Build

```bash
cd Build

# Runtime DLL
dotnet build SelfLearningEnemies.csproj -c Release

# Editor DLL (also builds the runtime DLL via ProjectReference)
dotnet build SelfLearningEnemies.Editor.csproj -c Release
```

Outputs:

```
SelfLearningEnemies/bin/Release/netstandard2.1/SelfLearningEnemies.dll
SelfLearningEnemies.Editor/bin/Release/netstandard2.1/SelfLearningEnemies.Editor.dll
```

Override paths without editing the file:

```bash
dotnet build SelfLearningEnemies.csproj -c Release \
    -p:UnityManaged="/path/to/Unity/Editor/Data/Managed" \
    -p:UnityProject="/path/to/UnityProjectWithMLAgents"
```

## 3. Import into the consumer Unity project

1. Copy `SelfLearningEnemies.dll` → `Assets/Plugins/SelfLearningEnemies.dll`.
2. Copy `SelfLearningEnemies.Editor.dll` → `Assets/Plugins/Editor/SelfLearningEnemies.Editor.dll`.
3. **Delete** from the consumer project (otherwise Unity compiles the same types
   twice → duplicate-definition errors):
   - all `.cs` files under `Core/`, `Actions/`, `Observations/`, `Rewards/`, `Editor/`
   - `SelfLearningEnemies.asmdef`
   - `Editor/SelfLearningEnemies.Editor.asmdef`
   - `Editor/Tests/` (and its `.asmdef`)
4. Keep `com.unity.ml-agents` installed (the DLL binds to `Unity.ML-Agents` by
   assembly name at runtime).
5. Use it: `using SelfLearningEnemies;` and attach components (`EnemyBrain`,
   `Obs*`, `Action*`, `Reward*`) exactly as before — `GetComponents<T>()`
   auto-discovery still works on precompiled DLLs.

## Important caveats

- **ML-Agents must stay installed** in the consumer project, and its **version
  must match** the one you compiled against (mismatches cause
  `MissingMethodException`).
- **API Compatibility Level** must match the target framework — keep the player
  setting at **.NET Standard 2.1** (ML-Agents Release 21 requirement).
- The runtime DLL was compiled with `UNITY_EDITOR` **undefined**, so editor-only
  code (`#if UNITY_EDITOR`, e.g. `DebugGizmos.OnDrawGizmos`) is stripped — correct
  for a player build.
- If the compiler reports a missing `UnityEngine.*Module` type, add the
  corresponding `<Reference>` entry (Unity keeps each subsystem in its own module
  DLL under `$(UnityManaged)/UnityEngine/`).

## Simpler alternative

Because this project already ships `.asmdef` files, Unity compiles the DLLs on
every build anyway. If you don't strictly need closed source, open the project in
Unity once and copy `Library/ScriptAssemblies/SelfLearningEnemies.dll` and
`Library/ScriptAssemblies/SelfLearningEnemies.Editor.dll` — no external project
needed. This folder exists for the explicit, reproducible external-compile route.
