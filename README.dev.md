# Unity Runtime Template (Developer Notes)

A Unity project template focused on **predictable runtime memory**, **low-GC gameplay code**, and **modular systems** you can reuse in upcoming games.

This repository is a baseline for future projects where ad-hoc allocations should be avoided and core gameplay features are built on top of reusable runtime primitives.

## Why this template exists

Most prototype code starts fast but becomes hard to scale: frequent GC spikes, container churn, and logic tied to scene scripts.

This template addresses that by:

- introducing allocator-backed containers early,
- using handle-based access patterns for safety,
- keeping systems in assembly-based modules with explicit dependencies,
- and providing a tween runtime as a real usage example.

## Why allocators and fixed collections?

### 1) Predictable memory usage

Runtime capacity is explicit at construction time, so memory growth is planned instead of accidental.

### 2) Lower GC pressure

Data is stored in preallocated native memory and accessed via handles and spans, which avoids per-frame heap allocations in hot paths.

### 3) Better runtime safety

Handles carry generation data, so stale references can be detected and rejected (`InvalidOperationException` on invalid handle state).

### 4) Faster iteration on systems code

With shared allocator and container primitives, new systems (AI queues, combat buffers, effect pools, etc.) can be built with consistent behavior.

### 5) Easier performance tuning

Allocator strategy is a clear tuning knob depending on lifecycle:

- `Persist`: long-lived data, bump allocator — deallocation is intentionally unsupported.
- `Arena`: grouped allocations with a `Clear()` reset — deallocation increments generation but does not reclaim memory until `Clear()`.
- `Stack`: LIFO allocation/deallocation — deallocation rolls back the pointer and invalidates all higher handles.

## What is included

### `Assets/Scripts/Core` (asmdef: `Glai.Core`)

| Type | Description |
|---|---|
| `struct Handle` | Typed slot reference with `Id`, `Index`, `ArrayIndex`, `Generation`. `IsValid(Handle)` checks id equality and generation match. |
| `struct HandleArray` | Same as `Handle` with an additional `Capacity` field; used for array slots. |
| `static class Logger` | Thin wrappers around `Debug.Log`, `Debug.LogWarning`, `Debug.LogError`. |
| `abstract class Object : IDisposable` | Auto-GUID base with Editor play-mode auto-dispose and protected log helpers. |
| `abstract class SceneNode : Object` | Wraps a `GameObject` with `Position`, `Rotation`, `LocalScale`, etc. Destroys the `GameObject` on `Dispose()`. |

### `Assets/Scripts/Allocator` (asmdef: `Glai.Allocator`, unsafe)

| Type | Description |
|---|---|
| `interface IAllocator` | `Allocate<T>()`, `AllocateArray<T>(int)`, `Deallocate(Handle)`, `Get<T>(Handle)`, `GetArray<T>(HandleArray)`, `Set<T>(Handle, T)`, `SetArray<T>(HandleArray, Span<T>)` |
| `class Persist` | Bump allocator. `Deallocate` throws — deallocation is unsupported by design. |
| `class Arena` | Bump allocator with `Clear()`. `Deallocate` increments generation but reclaims nothing until `Clear()`. |
| `class Stack` | LIFO allocator. `Deallocate` rolls back the pointer and bumps all higher-handle generations. |
| `abstract class MemoryState` | Registry of allocators. Collections take a `MemoryState` + `MemoryStateHandle` instead of a raw allocator. |

Data structs (`PersistData`, `ArenaData`, `StackData`) hold `name`, `capacityBytes`, and `maxHandles`.

### `Assets/Scripts/Collection` (asmdef: `Glai.Collection`, unsafe)

All collections are `struct`s with `where T : unmanaged`. They do not own memory; they hold a reference into a `MemoryState`.

| Type | Key members |
|---|---|
| `FixedArray<T>` | `Get(int)`, `Set(int, T)`, `Swap(int, int)`, indexer. `Count == Capacity` (always full). |
| `FixedList<T>` | `Add(T)`, `RemoveAt(int)` (swap-remove), `Get(int)`, `Contains(T)`, `Clear()`, `AsSpan()`, indexer. |
| `FixedQueue<T>` | `Enqueue(T)`, `Dequeue()`, `Peek()`, `Clear()`. No public `Count` or `Capacity`. |
| `FixedStack<T>` | `Push(T)`, `Pop()`, `Peek()`, `Clear()`. Public `Count`. |
| `FixedDictionary<TKey, TValue>` | `Add(TKey, TValue)`, `ContainsKey(TKey)`, `Remove(TKey)`, `Get(TKey)`, `TryGetValue(TKey, out TValue)`, `SetValue(TKey, TValue)`, indexer get/set. Robin-hood open-addressing. **Note:** the indexer setter calls `SetValue`, which throws `KeyNotFoundException` if the key is not already present — use `Add` for new keys. |

All collections have a `Dispose(MemoryState)` method (not `IDisposable`) that deallocates back into the owning state.

### `Assets/Scripts/Mathematics` (asmdef: `Glai.Mathematics`)

`static class Math` — unit helpers: `GB(int)`, `MB(int)`, `KB(int)`, `B(int)`.

### `Assets/Scripts/Module` (asmdef: `Glai.Module`)

| Type | Description |
|---|---|
| `class ModuleManager : MonoBehaviour` | Auto-created singleton (via `RuntimeInitializeOnLoadMethod`). Discovers `[ModuleRegister]` classes, calls `Initialize()`, routes `Start()` and `Tick(deltaTime)`. `GetModule<T>()` for retrieval. |
| `abstract class ModuleBase : Object` | `abstract void Initialize()`. |
| `class ModuleRegisterAttribute` | Marker attribute. Decorate a `ModuleBase` subclass to auto-register it. |
| `interface ITick` | `void Tick(float deltaTime)` |
| `interface IStart` | `void Start()` |

### `Assets/Scripts/Analytics` (asmdef: `Glai.Analytics`)

`static class MemoryAnalytics` — `RegisterCollection`, `UnregisterCollection`, `GetCollections`. Infrastructure stub; nothing in the codebase calls it yet.

### `Assets/Scripts/Tween/Core` (asmdef: `Glai.Tween.Core`, unsafe)

| Type | Description |
|---|---|
| `struct Tween<T>` | Value type holding `FromValue`, `ToValue`, `Duration`, `Speed`, `Target`. `GetValue(float, Func<T,T,float,T>)`, `IncreaseTime(float)`, `IsComplete()`. |
| `struct TweenTarget` | `TargetType` (Transform, SpriteRenderer, CanvasGroup, Material) + `PropertyType` (Position, Rotation, Scale, Color, Alpha) + `targetObjectId`. |
| `class TweenState : MemoryState` | Owns the persist allocator for active tweens and a stack of arena handles for sequences. `PopArenaHandle()` / `PushArenaHandle()`. Capacities are hardcoded (10 MB persist, 100 × 16 KB arenas). |
| `interface ITweenManager` | `TweenState TweenState`. Singleton contract implemented by `TweenManager`. |

### `Assets/Scripts/Tween` (asmdef: `Glai.Tween`)

| Type | Description |
|---|---|
| `static class Tween` | Public API. `SetTweenSpeed(TweenHandle, float)`, `SetGlobalSpeed(float)`, `CreateSequence(int capacity, int concurrentTweenCapacity)`. Extension methods on `Transform`: `DoMove`, `DoMoveX`, `DoMoveY`, `DoMoveZ` — each with explicit `(from, to, duration)` and implicit `(to, duration)` overloads. |
| `struct TweenHandle` | Read-only fields: `Id`, `Index`, `ArrayIndex`, `Generation`. Returned by all dispatch methods. |
| `enum Ease` | Linear, EaseIn/Out/InOut for Quad, Cubic, Quart, Quint. |
| `struct SequenceBuilder : IDisposable` | `Append(TweenHandle)` — adds a sequential step. `Join(TweenHandle)` — adds to the current step (concurrent). `Dispose()` — commits to `TweenManager` and releases arena memory. |

**Current tween limitations:**

- Position tweens (`DoMove*`) are fully functional.
- Rotation and scale dispatchers exist in the codebase but their `Dispatch()` calls are commented out; `TweenType.Rotation` and `TweenType.Scale` throw `NotImplementedException` in `TweenManager`.

### `Assets/Scripts/Gameplay` (asmdef: `Glai.Gameplay`)

`struct Entity` — `Id` and `Generation` fields. Data-carrier stub; no systems consume it yet.

## Architecture notes

- One asmdef per module keeps dependencies explicit and compile times short.
- `Glai.Allocator` and `Glai.Collection` allow unsafe code for native memory access.
- `MemoryState` is the central ownership container; collections do not own allocators directly.
- `ModuleManager` bootstraps itself before scene load via `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`.
- Runtime currently targets Unity `6000.3.8f1` and C# 9 / `netstandard2.1`.

## Quick start

### Prerequisites

- Unity Editor `6000.3.8f1`
- Windows PowerShell (examples below)

Set Unity executable path:

```powershell
$env:UNITY_EXE = "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe"
```

### Compile check

```powershell
& $env:UNITY_EXE -batchmode -nographics -projectPath . -quit -logFile Logs/compile.log
```

### Run all EditMode tests

```powershell
& $env:UNITY_EXE -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testResults Logs/editmode-results.xml -logFile Logs/editmode-tests.log
```

### Run a single test

```powershell
& $env:UNITY_EXE -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter "MyGame.TweenTests.Dispatch_CompletesTween" -testResults Logs/single-test.xml -logFile Logs/single-test.log
```

Use fully-qualified name format: `Namespace.ClassName.MethodName`

## Usage examples

### Allocator + MemoryState + fixed list

```csharp
using Glai.Allocator;
using Glai.Collection;

// Create a memory state that owns the allocator
var state = new MyMemoryState(); // subclass of MemoryState that adds a Persist allocator

// Collections take the state and the handle returned by AddAllocator
var list = new FixedList<int>(256, state.PersistHandle, state);
list.Add(10);
list.Add(25);
ref int val = ref list.Get(0); // val == 10
```

### Tween a position

```csharp
using Glai.Tween;

// Explicit from/to
TweenHandle handle = transform.DoMoveY(0f, 10f, 1.5f);

// From current position
TweenHandle handle2 = transform.DoMoveX(5f, 1f);

Tween.SetTweenSpeed(handle, 2f);
Tween.SetGlobalSpeed(1f);
```

### Build a sequence

```csharp
using Glai.Tween;

// Sequential steps
SequenceBuilder seq = Tween.CreateSequence(capacity: 4, concurrentTweenCapacity: 2);
seq.Append(transform.DoMoveX(0f, 5f, 1f));   // step 1
seq.Append(transform.DoMoveY(0f, 3f, 0.5f)); // step 2 (runs after step 1)
seq.Join(transform.DoMoveZ(0f, 1f, 0.5f));   // concurrent with step 2
seq.Dispose(); // commits and releases arena memory
```

### Mathematics helpers

```csharp
using Glai.Mathematics;

int bufferSize = Math.MB(10); // 10 485 760 bytes
```

## Current status

- Core allocator and collection primitives are in place and functional.
- Tween position movement is active (`DoMove`, `DoMoveX`, `DoMoveY`, `DoMoveZ`).
- Rotation and scale tweens: dispatch structure exists, not yet enabled.
- `MemoryAnalytics` infrastructure exists but is not wired up.
- `Entity` in Gameplay is a data-carrier stub; no systems use it yet.
- No project-owned automated tests (recommended next step).

## Recommended next steps

1. Add EditMode tests for allocators and fixed collections under `Assets/Tests`.
2. Enable and test rotation and scale tween dispatchers.
3. Wire `MemoryAnalytics` to allocator construction/disposal for runtime introspection.
4. Add benchmarks for allocator and container operations.
5. Expand tween features: easing curve selection per-tween, cancellation API.
6. Add sample gameplay modules that stress-test allocation patterns.

## Notes

- Unity-generated files (`*.csproj`, `Template-Project.slnx`) are regenerated by Unity; avoid manual edits.
- Cache/output folders (`Library/`, `Temp/`, `Logs/`, `UserSettings/`) are generated state.
- `TweenState` capacity values (10 MB persist, 100 × 16 KB arenas) are currently hardcoded; the `TweenStateData` parameter to its constructor is unused.

## Antivirus / Windows Defender false positives

The allocator module (`Glai.Allocator`) allocates unmanaged native memory at runtime. Previous versions of this code used `System.Runtime.InteropServices.Marshal.AllocHGlobal` and `Marshal.FreeHGlobal`, which call the Win32 `LocalAlloc`/`LocalFree` APIs under the hood via P/Invoke. Combined with `Marshal.PtrToStructure` / `Marshal.StructureToPtr` (also P/Invoke marshaling routines), Windows Defender's heuristics can flag the compiled assembly as a potential shellcode loader.

This was replaced with `Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Malloc` / `UnsafeUtility.Free` (the Unity-recommended path for native memory) and direct unsafe pointer reads/writes. This avoids the suspicious Win32 API surface that triggers the heuristic while keeping all functionality identical.
