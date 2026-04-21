# Glai Engine

`com.glai.engine` is an opinionated Unity package built from the Glai repository.

## What it includes

- Runtime systems from `Assets/Scripts`
- Editor tooling from `Assets/Scripts/**/Editor`
- EditMode tests from `Assets/Tests`
- Starter sample content copied from `Assets/Scenes` and `Assets/Resources`
- The prebuilt ECS source generator DLL staged into the package during packaging

## Install workflow

1. Build the package artifact from this repository with `build-package.cmd`
2. Add the generated `dist/com.glai.engine` folder or publish it to your package feed/git tag
3. Install the package into a target Unity project
4. Open `Tools/Glai/Setup`
5. Review the final confirmation dialog and apply the recommended cleanup/settings

## Source generator

The package build script compiles `SourceGenerators/Glai.ECS.SourceGen.csproj` and stages the DLL into `Editor/Analyzers/Glai.ECS.SourceGen.dll` inside the output package. Consumers should not need to run `sourcegen.cmd` manually.

## Current packaging model

This repository still authors code under `Assets/`. The package artifact is assembled into `dist/com.glai.engine` during the package build step.
