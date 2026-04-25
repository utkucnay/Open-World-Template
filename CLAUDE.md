# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Source of Truth

- Edit package code under `Packages/com.utkucnay.glai/`. Root `*.csproj` files and `Glai-dev.slnx` are Unity-generated mirrors — do not edit them.
- Unity is pinned to `6000.4.2f1` (`ProjectSettings/ProjectVersion.txt`).
- `Packages/com.utkucnay.glai/README.md` is stale; ignore it. `README.dev.md` and `AGENTS.md` are authoritative.

## Running Tests

All package tests are EditMode-only. Do **not** add `-quit`; it currently errors after tests complete.

Full test run:
```powershell
"<UnityEditorPath>\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-tests.xml -logFile Logs/editmode-tests.log
```

Single assembly (use the asmdef name):
```powershell
"<UnityEditorPath>\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -assemblyNames Glai.ECS.Tests.EditMode -testResults Logs/ecs-tests.xml -logFile Logs/ecs-tests.log
```

Test assembly names: `Glai.Core.Tests.EditMode`, `Glai.ECS.Tests.EditMode`, `Glai.Gameplay.Tests.EditMode`, `Glai.Analytics.Editor.Tests.EditMode`, `Glai.Module.Tests.EditMode`, `Glai.Mathematics.Tests.EditMode`.

## Rebuilding the ECS Source Generator

The generator DLL is checked in at `Packages/com.utkucnay.glai/Editor/Analyzers/Glai.ECS.SourceGen.dll`. Editable source is in `SourceGenerators/`.

```powershell
dotnet build SourceGenerators/Glai.ECS.SourceGen.csproj
# Then copy the new DLL/PDB into Packages/com.utkucnay.glai/Editor/Analyzers/
```

Unity picks up the new DLL on next domain reload. There are no committed `*.gen.cs` files — generated members exist only in memory.

## Architecture

### Module System (`Glai.Module`)

`ModuleManager` (`Runtime/Module/ModuleManager.cs`) is the central runtime. It runs on `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`, creates a `DontDestroyOnLoad` GameObject, replaces the Unity player loop with a pruned version, and discovers all registered modules by reflection.

A class is auto-discovered when it:
- Inherits `ModuleBase`
- Is not abstract
- Has `[ModuleRegister(priority: N)]`
- Is not in an assembly whose name contains `.Tests.`

Lifecycle order: `Initialize()` → `IStart.Start()` → `ITick.Tick(dt)` each Update → `ILateTick.LateTick(dt)` each LateUpdate → `Dispose()` on destroy.

Access other modules: `ModuleManager.Instance.GetModule<T>()` (throws if not registered).

Registered modules and priorities:
- `AnalyticsManager`: `-200`
- `EntityManager`: `-100`
- `GameplayManager`: `100`

Lower priority loads first. New modules that depend on `EntityManager` must use priority > `-100`.

### Creating a New Module

See `doc/creating-new-module.md` for the full template. Key checklist:
- Place source under `Packages/com.utkucnay.glai/Runtime/<ModuleName>/`
- Create an asmdef referencing `Glai.Module`
- Add `AssemblyInfo.cs` with `[assembly: AlwaysLinkAssembly]` and `[assembly: Preserve]`
- Decorate the class with `[Preserve, ModuleRegister(priority: N)]`
- Implement `Initialize()` and `Dispose()` (call `base.Dispose()` last, guard with `if (Disposed) return`)
- Config: use a `ScriptableObject` wrapper loaded via `Resources.Load<T>("Glai/<ConfigName>")`, fall back to `Config.Default`

### ECS (`Glai.ECS`)

Custom archetype-based ECS backed by native memory. Key types:
- `EntityManager` (partial, source-generated members) — creates/destroys entities and archetypes, runs query jobs
- `ECSAPI` (partial, source-generated per-namespace wrappers) — static facade over `EntityManager`
- `Archetype` — stores entities in cache-friendly `Chunk`s
- `QueryBuilder` — fluent filter builder (`.All<T>()`, `.Any<T>()`, `.None<T>()`)
- `IComponent` — marker interface; components must be `unmanaged` structs
- `IBufferComponent` — marker for buffer components; capacity set with `[FixedBuffer(N)]`

**Query jobs** are `struct`s with `[QueryJob(QueryExecution.X)]`:

| `QueryExecution` | Runs on |
|---|---|
| `MainThread` | main thread, Burst-compiled |
| `Async` | background thread (single job) |
| `ChunkParallel` | parallel per chunk |
| `EntityParallel` | parallel per entity batch |

The source generator (`QueryJobGenerator.cs`) reads the `Execute(RefRW<T>/RefR<T>/Buffer...)` method signature and emits `__Dispatch`, `__ChunkData`, `__AsyncJob`/`__ChunkParallelJob`/`__EntityParallelJob`, and `__Extensions` types. SIMD overloads `ExecuteSSE` (4-wide) and `ExecuteAVX` (8-wide) are also supported. `[NonBurstQuery]` on a struct forces `RunNonBurst`.

### Memory Allocators (`Glai.Core.Allocator`)

All allocators implement `IAllocator` and return typed `Handle`/`HandleArray` structs (generation-checked). Allocations come from `Global.DefaultPool` (a `MemoryPool`). Available allocators:
- `Arena` — bump-pointer, cleared in bulk; used for per-frame scratch
- `Persist` — persistent allocations that survive frame boundaries
- `Stack` — LIFO

`ECSMemoryState` and `GameplayMemoryState` own and scope allocators for their respective modules.

### Gameplay (`Glai.Gameplay`)

`GameplayManager` owns a list of `System` instances that receive `Start()`, `Tick(dt)`, and `LateTick(dt)`. Systems are registered via `GameplayManager.AddSystem(system)`. Archetype IDs are tracked in `GameplayManager.archetypeIds`.

### Editor Tooling

- `GlaiSetupWindow` (`Editor/SetupWindow/`) — `[InitializeOnLoad]`, auto-opens on first non-batch launch; can rewrite `Packages/manifest.json`
- Menu entries: `Tools/Glai/Setup`, `Tools/Glai/Memory Analytics`, `Tools/Glai/Custom Hierarchy`
