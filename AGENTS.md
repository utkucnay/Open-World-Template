# AGENTS.md

## Source Of Truth
- Unity is pinned to `6000.4.2f1` in `ProjectSettings/ProjectVersion.txt`.
- Edit package code under `Packages/com.utkucnay.glai/`; root `*.csproj` files and `Glai-dev.slnx` are Unity-generated mirrors and can contain stale assemblies.
- `Packages/com.utkucnay.glai/README.md` is stale: it describes an old `Assets/` authoring flow and `build-package.cmd`, but this repo has no package build scripts and runtime/editor/test code is under the embedded package.

## Repo Shape
- `Packages/com.utkucnay.glai/` is the package (`package.json` name `com.utkucnay.glai`); `Packages/manifest.json` marks it testable.
- `Assets/` is the host Unity project and copied starter/sample content, not the package source.
- Package boundaries are the asmdefs under `Packages/com.utkucnay.glai/Runtime/*`, `Editor/*`, and `Tests/*`.
- `ProjectSettings/EditorBuildSettings.asset` currently builds `Assets/StarterContent/Scenes/Temp.unity` and `Assets/Scenes/SampleScene.unity`.
- `Packages/com.utkucnay.glai/Samples~/StarterContent/` is distributable sample content; `Assets/StarterContent/` is the host-project copy.

## Runtime And Editor Wiring
- `Packages/com.utkucnay.glai/Runtime/Module/ModuleManager.cs` runs on `BeforeSceneLoad`, creates the `ModuleManager` object, and replaces the Unity player loop.
- Runtime modules are discovered by reflection as non-abstract `ModuleBase` types with `[ModuleRegister]`; verified registrations include `EntityManager`, `AnalyticsManager`, and `GameplayManager`.
- `Packages/com.utkucnay.glai/Editor/SetupWindow/GlaiSetupWindow.cs` is `[InitializeOnLoad]`, auto-opens on first non-batch editor launch, and can rewrite `Packages/manifest.json` and `Packages/packages-lock.json`.
- Verified Glai editor menus are `Tools/Glai/Setup`, `Tools/Glai/Memory Analytics`, and `Tools/Glai/Custom Hierarchy`.

## ECS Generator
- Unity consumes the checked-in analyzer DLL at `Packages/com.utkucnay.glai/Editor/Analyzers/Glai.ECS.SourceGen.dll`.
- Editable generator source is `SourceGenerators/Glai.ECS.SourceGen.csproj` plus `SourceGenerators/*.cs`; rebuild with `dotnet build SourceGenerators/Glai.ECS.SourceGen.csproj`, then copy the new DLL/PDB into `Packages/com.utkucnay.glai/Editor/Analyzers/`.
- ECS APIs rely on generated members: `EntityManager`, `QueryBuilder`, `Archetype`, and `ECSAPI` are `partial`, and there are no committed `*.gen.cs` files.

## Verification
- Package tests are EditMode-only under `Packages/com.utkucnay.glai/Tests/**/EditMode`; no package PlayMode test folders are present.
- Full EditMode run:
  ```powershell
  "<UnityEditorPath>\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-tests.xml -logFile Logs/editmode-tests.log
  ```
- Focused assembly run:
  ```powershell
  "<UnityEditorPath>\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -assemblyNames Glai.ECS.Tests.EditMode -testResults Logs/ecs-tests.xml -logFile Logs/ecs-tests.log
  ```
- Use package test asmdef names for `-assemblyNames`, for example `Glai.Core.Tests.EditMode`, `Glai.Gameplay.Tests.EditMode`, or `Glai.Analytics.Editor.Tests.EditMode`.
- Keep Unity CLI test runs in `-batchmode`, but omit `-quit`; adding `-quit` currently errors after tests. The setup bootstrap skips its first-open prompt only in batch mode.
