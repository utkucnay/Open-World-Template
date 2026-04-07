# Unity Runtime Template

A Unity template for projects that need stable frame-time behavior and reusable runtime systems.

This template is built around allocator-backed, fixed-capacity data structures so gameplay code can stay predictable under load.

## Why this project exists

- Keep memory usage explicit and planned instead of accidental.
- Reduce GC pressure in hot paths.
- Build reusable systems on top of shared runtime primitives.
- Start new projects from a strong performance-first foundation.

## Core idea

Instead of relying on dynamic containers everywhere, this template uses:

- allocator strategies (`Persist`, `Arena`, `Stack`)
- handle-based access with generation checks
- fixed-size collections (`FixedArray`, `FixedList`, `FixedQueue`, `FixedStack`, `FixedDictionary`)
- a module system (`ModuleManager`, `ModuleBase`) for lifecycle-managed singletons

This helps avoid common runtime spikes caused by frequent allocations and container growth.

## What you get

- `Assets/Scripts/Core` - handles, base object, scene node, and logging
- `Assets/Scripts/Allocator` - allocator implementations (`Persist`, `Arena`, `Stack`)
- `Assets/Scripts/Collection` - fixed-capacity containers
- `Assets/Scripts/Mathematics` - unit conversion helpers (`GB`, `MB`, `KB`, `B`)
- `Assets/Scripts/Module` - module manager and lifecycle interfaces
- `Assets/Scripts/Analytics` - memory analytics stub
- `Assets/Scripts/Tween/Core` - tween runtime internals and state
- `Assets/Scripts/Tween` - public tween API, sequence builder, easing
- `Assets/Scripts/Gameplay` - usage example types

## Quick start

### Unity version

- `6000.3.8f1`

### Compile check

```powershell
$env:UNITY_EXE = "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe"
& $env:UNITY_EXE -batchmode -nographics -projectPath . -quit -logFile Logs/compile.log
```

### Run all EditMode tests

```powershell
& $env:UNITY_EXE -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testResults Logs/editmode-results.xml -logFile Logs/editmode-tests.log
```

### Run one test

```powershell
& $env:UNITY_EXE -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter "Namespace.ClassName.MethodName" -testResults Logs/single-test.xml -logFile Logs/single-test.log
```

## Example usage

### Tween a transform

```csharp
using Glai.Tween;

// Move from current position to Y=10 over 1.5 seconds
TweenHandle handle = transform.DoMoveY(0f, 10f, 1.5f);
Tween.SetTweenSpeed(handle, 2f);
```

### Build a sequence

```csharp
using Glai.Tween;

SequenceBuilder seq = Tween.CreateSequence(capacity: 4, concurrentTweenCapacity: 2);
seq.Append(transform.DoMoveX(0f, 5f, 1f)); // step 1
seq.Append(transform.DoMoveY(0f, 3f, 0.5f)); // step 2
seq.Dispose(); // commits and releases arena memory
```

## Current status

- Core allocator and collection primitives are in place.
- Tween position movement is functional (`DoMove`, `DoMoveX`, `DoMoveY`, `DoMoveZ`).
- Rotation and scale tweens exist in the dispatch structure but are not yet active.
- No project-owned automated tests yet (recommended next step).

## Who this is for

Use this template if your upcoming projects care about:

- predictable runtime memory,
- low-GC gameplay loops,
- and a modular, reusable codebase structure.

For deeper technical notes, see `README.dev.md`.
