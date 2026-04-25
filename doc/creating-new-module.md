# Creating a New Glai Module

This document is the template for adding new runtime modules to Glai.

Module source lives under:

`Packages/com.utkucnay.glai/Runtime/`

Do not edit root `.csproj` files or `Glai-dev.slnx`; Unity regenerates them.

## Module Basics

A Glai module is discovered automatically by `ModuleManager`.

A class is registered only when it:

- Inherits `Glai.Module.ModuleBase`.
- Is not abstract.
- Has `[ModuleRegister]`.
- Is not compiled into an assembly whose name contains `.Tests.`.

Use `[Preserve]` on module classes to avoid Unity stripping issues.

Runtime module assemblies should also be marked with `[assembly: AlwaysLinkAssembly]` and `[assembly: Preserve]` in `AssemblyInfo.cs`.

## Minimal Module

```csharp
using Glai.Module;
using UnityEngine.Scripting;

namespace Glai.Example
{
    [Preserve, ModuleRegister(priority: 0)]
    public sealed class ExampleManager : ModuleBase
    {
        public override void Initialize()
        {
        }

        public override void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            base.Dispose();
        }
    }
}
```

## Lifecycle Module

Implement lifecycle interfaces only when needed.

```csharp
using Glai.Module;
using UnityEngine.Scripting;

namespace Glai.Example
{
    [Preserve, ModuleRegister(priority: 0)]
    public sealed class ExampleManager : ModuleBase, IStart, ITick, ILateTick
    {
        public override void Initialize()
        {
        }

        public void Start()
        {
        }

        public void Tick(float deltaTime)
        {
        }

        public void LateTick(float deltaTime)
        {
        }

        public override void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            base.Dispose();
        }
    }
}
```

## Lifecycle Order

`ModuleManager` is created before the first scene loads.

Order:

1. `ModuleManager.Awake()` discovers registered module types.
2. Each module instance is created with `Activator.CreateInstance`.
3. Lifecycle interfaces are collected.
4. `Initialize()` runs on every module.
5. `IStart.Start()` runs before ticking.
6. `ITick.Tick(float deltaTime)` runs during Unity `Update`.
7. `ILateTick.LateTick(float deltaTime)` runs during Unity `LateUpdate`.
8. `Dispose()` runs when `ModuleManager` is destroyed.

## Priority

Modules are sorted by:

1. `ModuleRegisterAttribute.Priority`
2. Full type name

Lower priority loads first.

Existing priorities:

- `AnalyticsManager`: `-200`
- `EntityManager`: `-100`
- `GameplayManager`: `100`

Choose priority based on dependencies.

If your module needs `EntityManager` already initialized, use a priority higher than `-100`.

Example:

```csharp
[Preserve, ModuleRegister(priority: 0)]
public sealed class ExampleManager : ModuleBase
{
}
```

## Assembly Definition

A new runtime module assembly must reference `Glai.Module`.

Recommended layout:

```text
Packages/com.utkucnay.glai/Runtime/Example/
  ExampleManager.cs
  AssemblyInfo.cs
  Config/
    ExampleConfigAsset.cs
    ExampleManagerConfig.cs
  Glai.Example.asmdef
```

Example asmdef:

```json
{
    "name": "Glai.Example",
    "rootNamespace": "Glai.Example",
    "references": [
        "Glai.Module"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Add extra references only when required.

Common references:

- `Glai.Core`
- `Glai.ECS`
- `Glai.Gameplay`

## Assembly Link and Preservation

Every runtime module assembly should include `AssemblyInfo.cs` with assembly-level link and preservation attributes.

This keeps Unity from stripping the assembly or registered module types before `ModuleManager` can discover them by reflection.

```csharp
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
[assembly: Preserve]
```

Add `InternalsVisibleTo` only when the module has tests that need access to internal members.

Example:

```csharp
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
[assembly: Preserve]
[assembly: InternalsVisibleTo("Glai.Example.Tests.EditMode")]
```

## Config Template

Use config only when the module needs user-tunable values.

Every config struct should have a `Default` property. Initialize every field in `Default`.

```csharp
using System;

namespace Glai.Example
{
    [Serializable]
    public struct ExampleManagerConfig
    {
        public int Capacity;
        public float UpdateRate;

        public static ExampleManagerConfig Default => new ExampleManagerConfig
        {
            Capacity = 1024,
            UpdateRate = 1f,
        };
    }
}
```

Create a `ScriptableObject` asset wrapper.

```csharp
using UnityEngine;

namespace Glai.Example
{
    [CreateAssetMenu(menuName = "Glai/Example Config", fileName = "ExampleConfig")]
    public sealed class ExampleConfigAsset : ScriptableObject
    {
        public ExampleManagerConfig ExampleManager = ExampleManagerConfig.Default;
    }
}
```

Load the config in the module.

```csharp
using Glai.Module;
using UnityEngine;
using UnityEngine.Scripting;

namespace Glai.Example
{
    [Preserve, ModuleRegister(priority: 0)]
    public sealed class ExampleManager : ModuleBase
    {
        const string ConfigResourcePath = "Glai/ExampleConfig";

        public ExampleManagerConfig Config { get; private set; } = ExampleManagerConfig.Default;

        public override void Initialize()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            var asset = Resources.Load<ExampleConfigAsset>(ConfigResourcePath);
            Config = asset != null ? asset.ExampleManager : ExampleManagerConfig.Default;
        }

        public override void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            base.Dispose();
        }
    }
}
```

## Resources Config Asset

For `Resources.Load<ExampleConfigAsset>("Glai/ExampleConfig")`, the asset must exist under a `Resources` folder.

Correct runtime project path:

```text
Assets/.../Resources/Glai/ExampleConfig.asset
```

Correct load path:

```csharp
Resources.Load<ExampleConfigAsset>("Glai/ExampleConfig");
```

Do not include `Resources` or `.asset` in the load path.

Wrong:

```csharp
Resources.Load<ExampleConfigAsset>("Resources/Glai/ExampleConfig.asset");
```

Package samples can include the asset here:

```text
Packages/com.utkucnay.glai/Samples~/StarterContent/Resources/Glai/ExampleConfig.asset
```

But `Samples~` content is not loaded directly by `Resources.Load`. It must be imported or copied into `Assets`.

## Accessing Other Modules

Use:

```csharp
var module = ModuleManager.Instance.GetModule<ExampleManager>();
```

`GetModule<T>()` throws if the module is not registered.

Only access other modules after they are initialized. Use priority to control dependency order.

## Disposal Template

Override `Dispose()` when the module owns:

- Native memory
- Glai fixed collections
- Event subscriptions
- Disposable systems
- Runtime-created objects

Template:

```csharp
public override void Dispose()
{
    if (Disposed)
    {
        return;
    }

    // Dispose owned state here.

    base.Dispose();
}
```

Call `base.Dispose()` last.

## Testing

Add EditMode tests under:

```text
Packages/com.utkucnay.glai/Tests/<ModuleName>/EditMode/
```

Reference existing module tests:

```text
Packages/com.utkucnay.glai/Tests/Module/EditMode/ModuleSmokeTests.cs
```

Focused test command:

```powershell
"<UnityEditorPath>\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -assemblyNames Glai.<ModuleName>.Tests.EditMode -testResults Logs/<module>-tests.xml -logFile Logs/<module>-tests.log
```

Do not add `-quit`; this repo currently errors after tests when `-quit` is used.

## Checklist

- Module is under `Packages/com.utkucnay.glai/Runtime/<ModuleName>/`.
- Module assembly references `Glai.Module`.
- Module has `AssemblyInfo.cs`.
- Module assembly has `[assembly: AlwaysLinkAssembly]`.
- Module assembly has `[assembly: Preserve]`.
- Module class inherits `ModuleBase`.
- Module class has `[Preserve, ModuleRegister(priority: ...)]`.
- `Initialize()` sets up runtime state.
- Optional lifecycle interfaces are used only when needed.
- Priority matches module dependencies.
- Config struct has a complete `Default` property.
- Config asset wrapper initializes fields from `Default`.
- `Resources.Load` path matches `Assets/.../Resources/Glai/<ConfigName>.asset`.
- Missing config asset falls back to `Default`.
- `Dispose()` releases owned state and calls `base.Dispose()` last.
- Tests are added when registration, lifecycle, config, or dependencies matter.
