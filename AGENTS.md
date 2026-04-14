# AGENTS.md

High-signal repo guide for OpenCode sessions.

## Rule precedence
- No `.cursor/rules`, `.cursorrules`, `.github/copilot-instructions.md`, or `opencode.json` found.
- This file is the repo-local instruction source.

## Verified project facts
- Unity editor version is `6000.4.2f1` (`ProjectSettings/ProjectVersion.txt`).
- Main runtime code is under `Assets/Scripts` (not `Assets/Module`).
- Unity Test Framework is installed (`com.unity.test-framework` in `Packages/manifest.json`).
- `README.md` / `README.dev.md` contain stale details (for example Unity `6000.3.8f1`); prefer project files and code.

## High-value code map
- Core primitives: `Assets/Scripts/Core` (`Object`, `Handle`, logging).
- Allocators + ownership: `Assets/Scripts/Allocator` and `Assets/Scripts/Allocator/Core` (`MemoryState`, `Persist`, `Arena`, `Stack`).
- Fixed containers: `Assets/Scripts/Collection`.
- Module bootstrap/lifecycle: `Assets/Scripts/Module` (`ModuleManager` auto-created via `RuntimeInitializeOnLoadMethod`).
- Tween system: `Assets/Scripts/Tween` and `Assets/Scripts/Tween/Core`.
- ECS system: `Assets/Scripts/ECS` and `Assets/Scripts/ECS/Core`.

## Critical behavior gotchas
- Runtime services are discovered by `ModuleManager` through `[ModuleRegister]` + `ModuleBase`; missing the attribute means no auto-registration.
- `TweenManager` currently dispatches position only; rotation/scale paths exist but are intentionally not active and throw `NotImplementedException` when used.
- `TweenState(TweenStateData data)` ignores `data` and hardcodes capacities (`10 MB` persist + `100 x 16 KB` arenas).

## Build/test commands (source of truth)
- Set editor path (per shell):
```powershell
$env:UNITY_EXE = "C:\Program Files\Unity\Hub\Editor\6000.4.2f1\Editor\Unity.exe"
```
- Compile check:
```powershell
& $env:UNITY_EXE -batchmode -nographics -projectPath . -quit -logFile Logs/compile.log
```
- All EditMode tests:
```powershell
& $env:UNITY_EXE -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testResults Logs/editmode-results.xml -logFile Logs/editmode-tests.log
```
- All PlayMode tests:
```powershell
& $env:UNITY_EXE -batchmode -nographics -projectPath . -runTests -testPlatform playmode -testResults Logs/playmode-results.xml -logFile Logs/playmode-tests.log
```
- Single test / fixture (`Namespace.Class.Method` or fixture name):
```powershell
& $env:UNITY_EXE -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter "MyNamespace.MyTests.MyCase" -testResults Logs/single-test.xml -logFile Logs/single-test.log
```

## Current testing state
- No project-owned tests were found under `Assets/**/Tests`.
- If adding tests, create explicit test asmdefs under `Assets/Tests` (EditMode and/or PlayMode).

## Editing constraints for this repo
- Do not hand-edit Unity-generated project files: `*.csproj`, `*.slnx`.
- Do not edit generated runtime folders: `Library/`, `Temp/`, `Logs/`, `UserSettings/`.
- Keep new runtime code inside existing asmdef modules under `Assets/Scripts/*` (avoid `Assembly-CSharp` drift).
- Unsafe code is already enabled in: `Glai.Allocator`, `Glai.Collection`, `Glai.ECS`, `Glai.ECS.Core`, `Glai.Tween`, `Glai.Tween.Core`.
