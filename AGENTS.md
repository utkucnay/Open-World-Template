# AGENTS.md

## Project Shape
- This is a Unity project pinned to `6000.4.2f1` in `ProjectSettings/ProjectVersion.txt`.
- The real source of truth is `Packages/com.utkucnay.glai/`. Root `*.csproj` files and `Glai-dev.slnx` are Unity-generated mirrors of the package asmdefs.
- Package boundaries are defined by asmdefs under `Packages/com.utkucnay.glai/Runtime/*`, `Editor/*`, and `Tests/*`. Edit asmdefs/package code, not generated solution metadata.
- The package README is partly stale: it still mentions authoring under `Assets/` and `build-package.cmd`, but this repo currently stores runtime/editor/tests directly under `Packages/com.utkucnay.glai` and has no packaging scripts at repo root.

## Runtime And Editor Entry Points
- `Packages/com.utkucnay.glai/Runtime/Module/ModuleManager.cs` bootstraps itself with `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` and rewrites the player loop. Changes there affect app startup globally.
- `Packages/com.utkucnay.glai/Editor/Core/GlaiSetupWindow.cs` is `[InitializeOnLoad]` and auto-prompts on first non-batch editor open. It can rewrite `Packages/manifest.json` and `Packages/packages-lock.json`.
- The setup window is also exposed as `Tools/Glai/Setup`. Other Glai editor tools live under `Tools/Glai/*`.

## ECS Source Generator
- `Packages/com.utkucnay.glai/Editor/Analyzers/Glai.ECS.SourceGen.dll` is checked in and actively referenced by Unity compilation.
- ECS types such as `EntityManager`, `QueryBuilder`, `Archetype`, and `ECSAPI` are `partial`; do not assume the visible file contains the full implementation.
- There are no committed `*.gen.cs` outputs in the package tree. If ECS behavior looks incomplete, account for analyzer-generated members before changing public shapes.

## Tests And Verification
- Current test coverage is EditMode-only under `Packages/com.utkucnay.glai/Tests/**/EditMode`. No package PlayMode test folders are present.
- Full EditMode run from CLI:
  ```powershell
  "<UnityEditorPath>\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-tests.xml -logFile Logs/editmode-tests.log -quit
  ```
- Single test assembly run:
  ```powershell
  "<UnityEditorPath>\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -assemblyNames Glai.ECS.Tests.EditMode -testResults Logs/ecs-tests.xml -logFile Logs/ecs-tests.log -quit
  ```
- Use assembly names from the test asmdefs/csproj names, e.g. `Glai.Core.Tests.EditMode`, `Glai.Gameplay.Tests.EditMode`, `Glai.Tween.Core.Tests.EditMode`.
- Batchmode matters here: the Glai setup bootstrap skips its first-open prompt when `Application.isBatchMode` is true.

## Sample / Host Project Notes
- `ProjectSettings/EditorBuildSettings.asset` currently includes `Assets/Scenes/SampleScene.unity` and `Assets/StarterContent/Scenes/Temp.unity`.
- The embedded package also ships sample content under `Packages/com.utkucnay.glai/Samples~/StarterContent/`. Be clear whether a change belongs to the host project under `Assets/` or the distributable package/sample under `Packages/com.utkucnay.glai`.
