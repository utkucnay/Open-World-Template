# Glai Engine

> [!CAUTION]
> **This project is under active development and is not ready for use.**
> Glai is an experimental Unity engine foundation. It is not published as a UPM package, has no stable API, and should not be used in any production project. Everything — module boundaries, ECS internals, editor tooling — is subject to breaking changes without notice.
>
> If you want to see what is being built, clone this repo, open it in Unity, and play `Temp.unity`.

## What Is Glai?

Glai is a lightweight, opinionated engine layer built on top of Unity. It replaces Unity's default player loop with a stripped-down version and provides its own module system, a custom ECS with Burst-compiled query jobs, a tween engine, custom memory allocators, and editor tooling — all shipped as a single embedded UPM package (`com.utkucnay.glai`).

The goal is a "debloated" foundation for gameplay development: fewer hidden systems running each frame, explicit memory management, and a data-oriented ECS that runs alongside (not inside) Unity's MonoBehaviour world.

## Current Project Status

**Version: `0.0.1` — early development, nothing is stable.**

| Module | Maturity | Notes |
|---|---|---|
| **Module System** (`Glai.Module`) | 🟡 Functional | Custom player loop, auto-discovery of modules via `[ModuleRegister]`, lifecycle hooks (`IStart`, `ITick`, `ILateTick`). Working and used by all other modules. |
| **ECS** (`Glai.ECS`) | 🟡 Functional | Archetype-based, chunked storage with per-component strides. Burst-compiled parallel query jobs via source-generated dispatch. Entity create/destroy, component get/set, `QueryBuilder` API. Under active refactoring. |
| **Gameplay** (`Glai.Gameplay`) | 🟡 Functional | System runner, archetype registration, `TransformComponent`, `PackedTransformComponent`. Has a sample bootstrap (`SampleGameplayBootstrap.cs`). |
| **Core** (`Glai.Core`) | 🟡 Functional | Custom memory allocators (Arena, MemoryPool, Stack, Persist), fixed-size collections (`FixedList`, `FixedArray`, `FixedDictionary`, `FixedQueue`, `FixedStack`, `FixedBitArray`, `FixedString128Bytes`), `EventBus`, `Logger`, `TypeRegistry`. |
| **Tween** (`Glai.Tween`) | 🟠 Partial | Position tweens work. Rotation and scale tweens are stubbed out. Sequence builder/dispatcher exists but is early. `[ModuleRegister]` is currently commented out — the tween system does not auto-load. |
| **Analytics** (`Glai.Analytics`) | 🟠 Partial | Memory analytics tracking exists. Editor window under `Tools/Glai/Memory Analytics`. Minimal implementation. |
| **Renderer** (`Glai.Renderer`) | 🔴 Skeleton | Manager registered but empty — `Initialize()` and `Dispose()` are no-ops. Has a `Native/` subfolder with some early work. |
| **Async** (`Glai.Async`) | 🔴 Empty | Runtime folder exists but contains no code yet. |
| **ECS Source Generator** | 🟡 Functional | Roslyn incremental generator emitting Burst-compatible `IQueryJobDispatch` structs and generic template specializations. Ships as a checked-in analyzer DLL. |
| **Editor Tooling** | 🟡 Functional | Setup wizard, memory analytics window, custom hierarchy. Auto-opens on first editor launch. |

### What Works Today

- Open the project → `ModuleManager` boots, strips the player loop, discovers and initializes modules.
- Create archetypes, spawn entities, run Burst-compiled ECS queries.
- The gameplay sample in `Temp.unity` exercises the ECS and gameplay systems end to end.
- Editor tooling provides setup, memory analytics, and a custom hierarchy view.

### What Does Not Work Yet

- Tween rotation/scale (stubbed), renderer (empty shell), async (no code).
- No published UPM package, no CI, no build scripts.
- APIs will break — there is no deprecation process yet.

## Requirements

- **Unity `6000.4.2f1`** (exact version, pinned in `ProjectSettings/ProjectVersion.txt`)

## Getting Started

> [!NOTE]
> **Do not install this as a UPM package.** The package is not published. The only way to explore Glai is to open the entire repository as a Unity project.

1. Clone this repository.
2. Open the folder in **Unity Hub** using Unity **`6000.4.2f1`**.
3. Let Unity import the project (first import may take a few minutes).
4. Open **`Assets/StarterContent/Scenes/Temp.unity`** — this is the primary development scene.
5. Press Play to see the module system boot and the gameplay sample run.
6. Optionally open `Assets/Scenes/SampleScene.unity` for a secondary reference scene.
7. Check `Tools/Glai/Setup` to inspect or apply the package's project setup flow.

## Repository Structure

```
Glai-dev/
├── Packages/com.utkucnay.glai/     ← the actual package (runtime, editor, tests, samples)
│   ├── Runtime/
│   │   ├── Module/                  ← module system & custom player loop
│   │   ├── ECS/                     ← archetype ECS with Burst query jobs
│   │   ├── Core/                    ← allocators, collections, event bus, math
│   │   ├── Gameplay/                ← gameplay systems, transform components
│   │   ├── Tween/                   ← tween engine (partial)
│   │   ├── Analytics/               ← memory analytics
│   │   ├── Renderer/                ← renderer (skeleton)
│   │   └── Async/                   ← (empty, planned)
│   ├── Editor/                      ← setup wizard, analytics window, hierarchy
│   ├── Tests/                       ← EditMode tests
│   └── Samples~/StarterContent/     ← distributable sample content
├── Assets/                          ← host-project scenes & copied sample content
├── SourceGenerators/                ← ECS Roslyn source generator (builds to analyzer DLL)
└── ProjectSettings/                 ← Unity project settings
```

## Developer Docs

See [README.dev.md](README.dev.md) for internal architecture, startup wiring, generator workflow, and CLI test commands.
