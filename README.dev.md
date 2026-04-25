# Developer README

> [!WARNING]
> **Glai is under active development and is not ready for production or general use.**
> This document is for contributors working on the engine itself. APIs, module boundaries, and internal wiring change frequently.

## Project Status at a Glance

Glai is at version **`0.0.1`**. There is no published package, no CI pipeline, and no build scripts. The engine is being developed inside this Unity host project, with the package embedded at `Packages/com.utkucnay.glai/`.

**What is actively being worked on:**
- ECS archetype storage, chunked memory layout, and Burst-compiled parallel query jobs
- Gameplay system runner and ECS integration
- Custom memory allocators (Arena, MemoryPool, Stack, Persist) and fixed-size collections
- Editor tooling (setup wizard, memory analytics, custom hierarchy)

**What is not yet functional:**
- Tween rotation/scale (position tweens work, but `[ModuleRegister]` is commented out — tween module does not auto-load)
- Renderer module (registered but completely empty)
- Async module (folder exists, no code)
- No package publishing workflow, no CI, no build scripts

## Source Of Truth

- Edit code under `Packages/com.utkucnay.glai/`.
- Treat root `*.csproj` files and `Glai-dev.slnx` as Unity-generated mirrors — they may contain stale assembly references.
- Unity is pinned to `6000.4.2f1` in `ProjectSettings/ProjectVersion.txt`.

## Repo Layout

```
Packages/com.utkucnay.glai/
├── Runtime/
│   ├── Module/         ← ModuleManager, player loop replacement, [ModuleRegister] discovery
│   ├── ECS/            ← ECSAPI, QueryBuilder, archetypes, Burst query jobs
│   │   └── Core/       ← EntityManager, Chunk, Archetype, memory state
│   ├── Core/           ← Global memory pool, allocators, collections, EventBus, Logger
│   │   ├── Allocator/  ← Arena, MemoryPool, Stack, Persist allocators
│   │   └── Collections/← FixedList, FixedArray, FixedDictionary, FixedQueue, etc.
│   ├── Gameplay/       ← GameplayManager, System base, TransformComponent
│   │   └── Core/       ← GameplayMemoryState, System.cs
│   ├── Tween/          ← TweenManager (disabled), TweenDispatcher, SequenceBuilder
│   │   └── Core/       ← Tween core types
│   ├── Analytics/      ← AnalyticsManager, MemoryAnalytics
│   ├── Renderer/       ← RendererManager (empty shell)
│   └── Async/          ← (planned, empty)
├── Editor/
│   ├── SetupWindow/    ← GlaiSetupWindow (auto-opens on first launch)
│   ├── Analytics/      ← Memory analytics editor window
│   ├── Hierarchy/      ← Custom hierarchy view
│   ├── Analyzers/      ← Checked-in ECS source generator DLL
│   └── Config/         ← Editor configuration
├── Tests/              ← EditMode tests (no PlayMode tests)
└── Samples~/StarterContent/  ← Distributable sample content
```

### Build Scenes

`ProjectSettings/EditorBuildSettings.asset` currently points at:

- **`Assets/StarterContent/Scenes/Temp.unity`** — primary development/test scene, start here
- `Assets/Scenes/SampleScene.unity` — secondary reference scene

## Runtime Wiring

### Startup Sequence

1. `Global.Initialize()` creates a 250 MB `MemoryPool` as the default allocator.
2. `ModuleManager.CreateOnInitialize()` runs on `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`:
   - Creates a `ModuleManager` GameObject marked `DontDestroyOnLoad`.
   - **Replaces the entire Unity player loop** with a stripped-down version (only essential subsystems like input, rendering, audio, time).
3. `ModuleManager.RegisterModules()` scans all loaded assemblies for non-abstract `ModuleBase` types with `[ModuleRegister]`, ordered by priority.
4. Each module's `Initialize()` is called, then `Start()` on the first frame, then `Tick()`/`LateTick()` every frame.

### Currently Registered Modules (by priority)

| Priority | Module | Lifecycle |
|---|---|---|
| -200 | `AnalyticsManager` | Initialize only |
| 0 | `RendererManager` | Initialize only (empty) |
| 0 | `EntityManager` | Initialize, Start, Tick |
| 100 | `GameplayManager` | Initialize, Start, Tick, LateTick |

> [!NOTE]
> `TweenManager` has `[ModuleRegister]` commented out and does not auto-load.

## Editor Wiring

- `GlaiSetupWindow` is `[InitializeOnLoad]` and auto-opens on first non-batch editor launch.
- The setup flow can rewrite `Packages/manifest.json` and `Packages/packages-lock.json`.
- Verified menu entries:
  - `Tools/Glai/Setup`
  - `Tools/Glai/Memory Analytics`
  - `Tools/Glai/Custom Hierarchy`

## ECS Generator

The ECS uses a Roslyn source generator to emit Burst-compatible code at compile time.

- **Checked-in DLL**: `Packages/com.utkucnay.glai/Editor/Analyzers/Glai.ECS.SourceGen.dll`
- **Editable source**: `SourceGenerators/QueryJobGenerator.cs` and `SourceGenerators/GenericTemplateGenerator.cs`
- **Rebuild workflow**:
  ```powershell
  dotnet build SourceGenerators/Glai.ECS.SourceGen.csproj
  # then copy the output DLL + PDB into Packages/com.utkucnay.glai/Editor/Analyzers/
  ```
- ECS APIs (`EntityManager`, `QueryBuilder`, `Archetype`, `ECSAPI`) are `partial` classes. The generator emits companion members at compile time — there are no committed `*.gen.cs` files in the repo.

> [!IMPORTANT]
> Changing `SourceGenerators/*.cs` has **no effect** until you rebuild the DLL and copy it back into the `Editor/Analyzers/` folder.

## Testing

EditMode tests only — no PlayMode test folders exist in the package.

Test assemblies:
- `Glai.Core.Tests.EditMode`
- `Glai.ECS.Tests.EditMode`
- `Glai.ECS.Core.Tests.EditMode`
- `Glai.Gameplay.Tests.EditMode`
- `Glai.Module.Tests.EditMode`
- `Glai.Allocator.Tests.EditMode`
- `Glai.Collection.Tests.EditMode`
- `Glai.Mathematics.Tests.EditMode`
- `Glai.Tween.Core.Tests.EditMode`
- `Glai.Tween.Tests.EditMode`
- `Glai.Analytics.Tests.EditMode`
- `Glai.Analytics.Editor.Tests.EditMode`

### Run All Tests

```powershell
"<UnityEditorPath>\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-tests.xml -logFile Logs/editmode-tests.log
```

### Run a Single Assembly

```powershell
"<UnityEditorPath>\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -assemblyNames Glai.ECS.Tests.EditMode -testResults Logs/ecs-tests.xml -logFile Logs/ecs-tests.log
```

> [!WARNING]
> Do **not** pass `-quit` to CLI test runs — it currently errors after tests complete. Omit `-quit` and let the process exit on its own.

Keep CLI runs in `-batchmode` so the first-open setup prompt is skipped.

## Current Caveats

- **Not published.** There is no UPM registry entry, no `.tgz` release, no install-via-URL workflow. The only way to use Glai is to clone this repo and open it in Unity `6000.4.2f1`.
- **No build scripts.** The repo contains no `*.cmd`, `.bat`, `.ps1`, or shell scripts for packaging or CI.
- **Stale inner README.** `Packages/com.utkucnay.glai/README.md` still describes an older `Assets/`-based authoring flow with `build-package.cmd` — it is inaccurate and should be ignored.
- **Tween module disabled.** `TweenManager` has its `[ModuleRegister]` commented out; rotation and scale are stubbed.
- **Renderer and Async are empty shells.** Registered/planned but contain no meaningful implementation.
