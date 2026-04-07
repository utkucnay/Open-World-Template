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
- fixed-size collections (`FixedArray`, `FixedList`, `FixedQueue`, `FixedStack`)

This helps avoid common runtime spikes caused by frequent allocations and container growth.

## What you get

- `Assets/Module/Core` - handles and logging
- `Assets/Module/Allocator` - allocator implementations
- `Assets/Module/Collection` - fixed-capacity containers
- `Assets/Module/Tween` - tween API and runtime internals
- `Assets/Module/Gameplay` - usage example script

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

```csharp
using Glai.Tween;

var handle = transform.DoMoveY(0f, 10f, 1.5f);
Tween.SetTweenSpeed(handle, 2f);
yield return handle.ToYield();
```

## Who this is for

Use this template if your upcoming projects care about:

- predictable runtime memory,
- low-GC gameplay loops,
- and a modular, reusable codebase structure.

For deeper technical notes, see `README.dev.md`.
