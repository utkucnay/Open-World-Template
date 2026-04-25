using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PMPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Glai.Core.Editor
{
    [InitializeOnLoad]
    internal static class GlaiSetupBootstrap
    {
        static GlaiSetupBootstrap()
        {
            string initialPromptKey = GetInitialPromptKey();
            if (Application.isBatchMode || EditorPrefs.GetBool(initialPromptKey, false))
            {
                return;
            }

            EditorApplication.delayCall += ShowInitialPrompt;
        }

        private static void ShowInitialPrompt()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ShowInitialPrompt;
                return;
            }

            EditorPrefs.SetBool(GetInitialPromptKey(), true);
            GlaiSetupWindow.Open(initialPrompt: true);
        }

        private static string GetInitialPromptKey()
        {
            return $"Glai.Setup.InitialPromptShown::{Application.dataPath}";
        }
    }

    public sealed class GlaiSetupWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Glai/Setup";
        private const float ToggleLabelWidth = 240f;

        private static readonly string[] RequiredPackages =
        {
            "com.unity.burst",
            "com.unity.collections",
            "com.unity.mathematics",
        };

        private static readonly PackageRule[] PackageRules =
        {
            new PackageRule("com.unity.adaptiveperformance", new[] { "UnityEngine.AdaptivePerformance" }, new[] { "Unity.AdaptivePerformance" }),
            new PackageRule("com.unity.adaptiveperformance.android", Array.Empty<string>(), Array.Empty<string>()),
            new PackageRule("com.unity.ai.navigation", new[] { "Unity.AI.Navigation" }, new[] { "Unity.AI.Navigation" }),
            new PackageRule("com.unity.asset-manager-for-unity", Array.Empty<string>(), Array.Empty<string>()),
            new PackageRule("com.unity.burst", new[] { "Unity.Burst" }, new[] { "Unity.Burst" }),
            new PackageRule("com.unity.collections", new[] { "Unity.Collections" }, new[] { "Unity.Collections" }),
            new PackageRule("com.unity.ide.rider", Array.Empty<string>(), Array.Empty<string>()),
            new PackageRule("com.unity.ide.visualstudio", Array.Empty<string>(), Array.Empty<string>()),
            new PackageRule("com.unity.mathematics", new[] { "Unity.Mathematics" }, new[] { "Unity.Mathematics" }),
            new PackageRule("com.unity.memoryprofiler", new[] { "Unity.MemoryProfiler" }, new[] { "Unity.MemoryProfiler" }),
            new PackageRule("com.unity.multiplayer.center", new[] { "Unity.Multiplayer.Center" }, new[] { "Unity.Multiplayer.Center" }),
            new PackageRule("com.unity.render-pipelines.core", new[] { "UnityEngine.Rendering" }, new[] { "Unity.RenderPipelines.Core.Runtime", "Unity.RenderPipelines.Core.Editor" }),
            new PackageRule("com.unity.render-pipelines.universal", new[] { "UnityEngine.Rendering.Universal" }, new[] { "Unity.RenderPipelines.Universal.Runtime", "Unity.RenderPipelines.Universal.Editor" }),
            new PackageRule("com.unity.searcher", new[] { "UnityEditor.Searcher" }, new[] { "Unity.Searcher", "Unity.Searcher.Editor" }),
            new PackageRule("com.unity.shadergraph", new[] { "UnityEditor.ShaderGraph", "UnityEditor.Rendering", "UnityEditor.VFX" }, new[] { "Unity.ShaderGraph", "Unity.ShaderGraph.Editor" }),
            new PackageRule("com.unity.test-framework", new[] { "UnityEngine.TestTools", "NUnit.Framework", "Unity.PerformanceTesting" }, new[] { "UnityEngine.TestRunner", "Unity.PerformanceTesting" }),
            new PackageRule("com.unity.vectorgraphics", new[] { "Unity.VectorGraphics" }, new[] { "Unity.VectorGraphics" }),
            new PackageRule("com.unity.visualscripting", new[] { "Unity.VisualScripting" }, new[] { "Unity.VisualScripting" }),
        };

        private static readonly ModuleOption[] OptionalModuleDefaults =
        {
            new ModuleOption("com.unity.modules.accessibility", true, "Accessibility APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.ai", true, "No runtime AI module usage was found."),
            new ModuleOption("com.unity.modules.amd", true, "Legacy vendor-specific module is not referenced."),
            new ModuleOption("com.unity.modules.androidjni", true, "Android JNI bindings are not used by Glai systems."),
            new ModuleOption("com.unity.modules.animation", true, "Animation module is not used by Glai systems."),
            new ModuleOption("com.unity.modules.assetbundle", true, "AssetBundle APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.audio", false, "Unchecked by default because ModuleManager currently references Unity audio player loop systems."),
            new ModuleOption("com.unity.modules.cloth", true, "Cloth simulation is not used by Glai systems."),
            new ModuleOption("com.unity.modules.director", true, "Timeline/Director APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.hierarchycore", true, "Hierarchy core module is not used by Glai systems."),
            new ModuleOption("com.unity.modules.imageconversion", true, "Image conversion APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.imgui", true, "Runtime IMGUI is not used by Glai systems."),
            new ModuleOption("com.unity.modules.jsonserialize", true, "No JsonUtility or Unity JSON serialization usage was found."),
            new ModuleOption("com.unity.modules.nvidia", true, "Legacy vendor-specific module is not referenced."),
            new ModuleOption("com.unity.modules.particlesystem", true, "Particle system APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.physics", true, "3D physics is not used by Glai systems."),
            new ModuleOption("com.unity.modules.physics2d", true, "2D physics is not used by Glai systems."),
            new ModuleOption("com.unity.modules.screencapture", true, "Screen capture APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.subsystems", true, "Subsystem registration is not used by Glai systems."),
            new ModuleOption("com.unity.modules.terrain", true, "Terrain APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.terrainphysics", true, "Terrain physics is not used by Glai systems."),
            new ModuleOption("com.unity.modules.tilemap", true, "Tilemap APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.ui", true, "UGUI runtime is not used by Glai systems."),
            new ModuleOption("com.unity.modules.uielements", true, "Runtime UIElements is not used by Glai systems."),
            new ModuleOption("com.unity.modules.unityanalytics", true, "Unity Analytics package/module is not used by Glai systems."),
            new ModuleOption("com.unity.modules.umbra", true, "Umbra occlusion APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.unitywebrequest", true, "UnityWebRequest base APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.unitywebrequestassetbundle", true, "UnityWebRequest AssetBundle APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.unitywebrequestaudio", true, "UnityWebRequest audio APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.unitywebrequesttexture", true, "UnityWebRequest texture APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.unitywebrequestwww", true, "UnityWebRequest WWW are not used by Glai systems."),
            new ModuleOption("com.unity.modules.vectorgraphics", true, "Vector Graphics APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.adaptiveperformance", true, "Adaptive Performance APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.vehicles", true, "Vehicle APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.video", true, "Video playback APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.vr", true, "Legacy VR APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.wind", true, "Wind zone APIs are not used by Glai systems."),
            new ModuleOption("com.unity.modules.xr", true, "XR APIs are not used by Glai systems."),
        };

        private readonly List<PackageOption> optionalPackages = new List<PackageOption>();
        private readonly List<ModuleOption> optionalModules = new List<ModuleOption>();
        private readonly List<string> transitiveRegistryPackages = new List<string>();

        private Vector2 scrollPosition;
        private bool initialPrompt;
        private bool ensureRequiredPackages = true;
        private bool stripOptionalPackages = true;
        private bool stripOptionalModules = true;
        private bool showPackageDebug;
        private long lastManifestWriteTicks;
        private long lastLockWriteTicks;
        private ListRequest packageListRequest;
        private string packageRefreshStatus = "Using package files.";
        private readonly List<string> livePackageDebugEntries = new List<string>();
        [MenuItem(MenuPath)]
        public static void OpenMenu()
        {
            Open(initialPrompt: false);
        }

        internal static void Open(bool initialPrompt)
        {
            GlaiSetupWindow window = GetWindow<GlaiSetupWindow>("Glai Setup");
            window.minSize = new Vector2(720f, 520f);
            window.initialPrompt = initialPrompt;
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            ReloadOptions();
            ApplyRecommendedPreset();
        }

        private void OnGUI()
        {
            TryConsumePackageListRequest();
            RefreshOptionsIfNeeded();
            SetupPlan plan = BuildPlan();

            DrawHeader();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawPresetButtons();
            DrawPackageSection();
            DrawModuleSection();
            DrawPreviewSection(plan);
            EditorGUILayout.EndScrollView();

            DrawFooter(plan);
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Glai Setup", EditorStyles.boldLabel);

            string message = initialPrompt
                ? "Review the aggressive recommended cleanup before applying it to this project."
                : "Remove packages/modules that are outside the Glai stack.";
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void DrawPresetButtons()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Recommended", GUILayout.Width(160f)))
            {
                ApplyRecommendedPreset();
            }

            if (GUILayout.Button("Minimal", GUILayout.Width(100f)))
            {
                ApplyMinimalPreset();
            }

            if (GUILayout.Button("Reload Current Project", GUILayout.Width(180f)))
            {
                ReloadOptions();
                ApplyRecommendedPreset();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void DrawPackageSection()
        {
            EditorGUILayout.LabelField("Unity Package Strip", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Package candidates are detected from live Package Manager data plus project code/asmdefs. Direct Unity packages are toggleable even when Unity reports them as BuiltIn.", MessageType.Info);
            EditorGUILayout.LabelField(packageRefreshStatus, EditorStyles.wordWrappedMiniLabel);
            ensureRequiredPackages = EditorGUILayout.ToggleLeft("Ensure required packages", ensureRequiredPackages);
            stripOptionalPackages = EditorGUILayout.ToggleLeft("Remove unused direct Unity packages", stripOptionalPackages);

            using (new EditorGUI.DisabledScope(!stripOptionalPackages))
            {
                DrawToggleList(optionalPackages);
            }

            DrawTransitivePackageSection();
            DrawPackageDebugSection();

            EditorGUILayout.Space(10f);
        }

        private void DrawPackageDebugSection()
        {
            showPackageDebug = EditorGUILayout.Foldout(showPackageDebug, "Package Debug", true);
            if (!showPackageDebug)
            {
                return;
            }

            EditorGUILayout.LabelField($"Live package count: {livePackageDebugEntries.Count}", EditorStyles.miniBoldLabel);
            if (livePackageDebugEntries.Count == 0)
            {
                EditorGUILayout.LabelField("No live package data captured yet.", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            for (int i = 0; i < livePackageDebugEntries.Count; i++)
            {
                EditorGUILayout.LabelField($"- {livePackageDebugEntries[i]}", EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawTransitivePackageSection()
        {
            if (transitiveRegistryPackages.Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField("Transitive Unity Packages", EditorStyles.miniBoldLabel);
            for (int i = 0; i < transitiveRegistryPackages.Count; i++)
            {
                EditorGUILayout.LabelField($"- {transitiveRegistryPackages[i]}", EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawModuleSection()
        {
            EditorGUILayout.LabelField("Module Strip", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Audio is available to strip, but it stays unchecked by default because ModuleManager currently references Unity audio player loop systems.", MessageType.Warning);
            stripOptionalModules = EditorGUILayout.ToggleLeft("Strip selected built-in modules", stripOptionalModules);

            using (new EditorGUI.DisabledScope(!stripOptionalModules))
            {
                DrawToggleList(optionalModules);
            }

            EditorGUILayout.Space(10f);
        }

        private void DrawToggleList<T>(List<T> items) where T : ToggleOption
        {
            for (int i = 0; i < items.Count; i++)
            {
                T option = items[i];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16f);
                option.Selected = EditorGUILayout.Toggle(option.Selected, GUILayout.Width(18f));
                EditorGUILayout.LabelField(option.Name, GUILayout.Width(ToggleLabelWidth));
                EditorGUILayout.LabelField(option.Reason, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPreviewSection(SetupPlan plan)
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (plan.IsEmpty)
            {
                EditorGUILayout.HelpBox("No changes are currently selected.", MessageType.Info);
                return;
            }

            DrawPreviewList("Packages To Add", plan.PackagesToAdd);
            DrawPreviewList("Packages To Remove", plan.PackagesToRemove);
            DrawPreviewList("Modules To Remove", plan.ModulesToRemove);
            DrawPreviewList("Warnings", plan.Warnings);
        }

        private static void DrawPreviewList(string title, List<string> entries)
        {
            if (entries.Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            for (int i = 0; i < entries.Count; i++)
            {
                EditorGUILayout.LabelField($"- {entries[i]}", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawFooter(SetupPlan plan)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(plan.IsEmpty))
            {
                if (GUILayout.Button("Apply Selected", GUILayout.Width(140f)))
                {
                    ApplySelected(plan);
                }
            }

            if (GUILayout.Button("Close", GUILayout.Width(100f)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void ApplySelected(SetupPlan plan)
        {
            string confirmation = plan.ToConfirmationText();
            bool confirmed = EditorUtility.DisplayDialog("Apply Glai Setup", confirmation, "Apply", "Cancel");
            if (!confirmed)
            {
                return;
            }

            try
            {
                if (plan.ManifestChanged)
                {
                    ApplyManifestChanges(plan);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent("Glai setup applied."));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Glai Setup] Failed to apply setup: {exception}");
                EditorUtility.DisplayDialog("Glai Setup Failed", exception.Message, "OK");
            }
        }

        private void ApplyManifestChanges(SetupPlan plan)
        {
            string manifestPath = GetManifestPath();
            string manifestJson = File.ReadAllText(manifestPath);
            Dictionary<string, string> dependencies = ParseDependencies(manifestJson);

            for (int i = 0; i < plan.PackagesToRemove.Count; i++)
            {
                dependencies.Remove(plan.PackagesToRemove[i]);
            }

            for (int i = 0; i < plan.ModulesToRemove.Count; i++)
            {
                dependencies.Remove(plan.ModulesToRemove[i]);
            }

            for (int i = 0; i < plan.RequiredPackagesToEnsure.Count; i++)
            {
                PackageOption option = plan.RequiredPackagesToEnsure[i];
                dependencies[option.Name] = option.Version;
            }

            string updatedJson = ReplaceDependenciesObject(manifestJson, dependencies);
            File.WriteAllText(manifestPath, updatedJson, new UTF8Encoding(false));
        }

        private SetupPlan BuildPlan()
        {
            SetupPlan plan = new SetupPlan();
            string manifestPath = GetManifestPath();
            if (!File.Exists(manifestPath))
            {
                plan.Warnings.Add("Packages/manifest.json was not found.");
                return plan;
            }

            Dictionary<string, string> dependencies = ParseDependencies(File.ReadAllText(manifestPath));

            if (ensureRequiredPackages)
            {
                for (int i = 0; i < RequiredPackages.Length; i++)
                {
                    string packageName = RequiredPackages[i];
                    string version = GetRequiredPackageVersion(packageName, dependencies);
                    if (!dependencies.TryGetValue(packageName, out string currentVersion) || !string.Equals(currentVersion, version, StringComparison.Ordinal))
                    {
                        plan.RequiredPackagesToEnsure.Add(new PackageOption(packageName, version, true, "Required by Glai runtime."));
                        plan.PackagesToAdd.Add($"{packageName} @ {version}");
                    }
                }
            }

            if (stripOptionalPackages)
            {
                for (int i = 0; i < optionalPackages.Count; i++)
                {
                    PackageOption option = optionalPackages[i];
                    if (option.Selected && dependencies.ContainsKey(option.Name))
                    {
                        plan.PackagesToRemove.Add(option.Name);
                    }
                }
            }

            if (stripOptionalModules)
            {
                for (int i = 0; i < optionalModules.Count; i++)
                {
                    ModuleOption option = optionalModules[i];
                    if (option.Selected && dependencies.ContainsKey(option.Name))
                    {
                        plan.ModulesToRemove.Add(option.Name);
                    }
                }
            }

            if (plan.ModulesToRemove.Contains("com.unity.modules.audio"))
            {
                plan.Warnings.Add("Audio module removal is aggressive because ModuleManager currently references Unity audio player loop systems.");
            }

            if (plan.ModulesToRemove.Contains("com.unity.modules.imgui"))
            {
                plan.Warnings.Add("Runtime IMGUI module removal is aggressive. Keep it if your host project uses OnGUI/GUILayout at runtime.");
            }

            return plan;
        }

        private void ApplyRecommendedPreset()
        {
            ensureRequiredPackages = true;
            stripOptionalPackages = true;
            stripOptionalModules = true;

            SetRecommendedSelections(optionalPackages);
            SetRecommendedSelections(optionalModules);
        }

        private void ApplyMinimalPreset()
        {
            ensureRequiredPackages = true;
            stripOptionalPackages = false;
            stripOptionalModules = false;

            SetSelection(optionalPackages, false);
            SetSelection(optionalModules, false);
        }

        private void ReloadOptions()
        {
            Dictionary<string, bool> selectedByName = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (int i = 0; i < optionalPackages.Count; i++)
            {
                selectedByName[optionalPackages[i].Name] = optionalPackages[i].Selected;
            }

            optionalPackages.Clear();
            optionalPackages.AddRange(BuildOptionalPackageOptions());
            for (int i = 0; i < optionalPackages.Count; i++)
            {
                if (selectedByName.TryGetValue(optionalPackages[i].Name, out bool selected))
                {
                    optionalPackages[i].Selected = selected;
                }
            }

            transitiveRegistryPackages.Clear();
            transitiveRegistryPackages.AddRange(BuildTransitiveRegistryPackages());
            UpdatePackageTimestamps();
            StartPackageListRefresh();

            if (optionalModules.Count == 0)
            {
                for (int i = 0; i < OptionalModuleDefaults.Length; i++)
                {
                    ModuleOption option = OptionalModuleDefaults[i];
                    optionalModules.Add(new ModuleOption(option.Name, option.Recommended, option.Reason));
                }
            }
        }

        private void RefreshOptionsIfNeeded()
        {
            string manifestPath = GetManifestPath();
            string lockPath = GetPackagesLockPath();
            long manifestTicks = File.Exists(manifestPath) ? File.GetLastWriteTimeUtc(manifestPath).Ticks : 0;
            long lockTicks = File.Exists(lockPath) ? File.GetLastWriteTimeUtc(lockPath).Ticks : 0;
            if (manifestTicks == lastManifestWriteTicks && lockTicks == lastLockWriteTicks)
            {
                return;
            }

            ReloadOptions();
        }

        private void UpdatePackageTimestamps()
        {
            string manifestPath = GetManifestPath();
            string lockPath = GetPackagesLockPath();
            lastManifestWriteTicks = File.Exists(manifestPath) ? File.GetLastWriteTimeUtc(manifestPath).Ticks : 0;
            lastLockWriteTicks = File.Exists(lockPath) ? File.GetLastWriteTimeUtc(lockPath).Ticks : 0;
        }

        private void StartPackageListRefresh()
        {
            if (packageListRequest != null && !packageListRequest.IsCompleted)
            {
                return;
            }

            packageRefreshStatus = "Refreshing live package data...";
            packageListRequest = Client.List(true, true);
        }

        private void TryConsumePackageListRequest()
        {
            if (packageListRequest == null || !packageListRequest.IsCompleted)
            {
                return;
            }

            if (packageListRequest.Status == StatusCode.Success)
            {
                List<PMPackageInfo> packages = new List<PMPackageInfo>();
                foreach (PMPackageInfo package in packageListRequest.Result)
                {
                    packages.Add(package);
                }

                ApplyLivePackageData(packages);
                packageRefreshStatus = $"Using live Package Manager data ({packages.Count} packages).";
            }
            else
            {
                packageRefreshStatus = "Live Package Manager refresh failed. Falling back to package files.";
            }

            packageListRequest = null;
        }

        private void ApplyLivePackageData(List<PMPackageInfo> packages)
        {
            Dictionary<string, bool> selectedByName = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (int i = 0; i < optionalPackages.Count; i++)
            {
                selectedByName[optionalPackages[i].Name] = optionalPackages[i].Selected;
            }

            HashSet<string> usedPackages = DetectUsedPackages();
            Dictionary<string, PMPackageInfo> packagesByName = new Dictionary<string, PMPackageInfo>(StringComparer.Ordinal);
            for (int i = 0; i < packages.Count; i++)
            {
                packagesByName[packages[i].name] = packages[i];
            }

            packages.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

            livePackageDebugEntries.Clear();
            for (int i = 0; i < packages.Count; i++)
            {
                PMPackageInfo package = packages[i];
                string directness = package.isDirectDependency ? "direct" : "transitive";
                livePackageDebugEntries.Add($"{package.name} @ {package.version} [{package.source}, {directness}]");
            }

            optionalPackages.Clear();
            for (int i = 0; i < packages.Count; i++)
            {
                PMPackageInfo package = packages[i];
                if (Array.IndexOf(RequiredPackages, package.name) >= 0 || !IsSupportedUnityPackageSource(package.source) || !package.isDirectDependency)
                {
                    continue;
                }

                bool used = usedPackages.Contains(package.name);
                string reason = used
                    ? "Usage was detected in project code or asmdefs, so this stays unchecked by default."
                    : "No package-specific usage was detected in project code or asmdefs.";

                List<string> transitivePackages = CollectLiveTransitivePackages(package.name, packagesByName);
                if (transitivePackages.Count > 0)
                {
                    reason += " Removing it also clears: " + string.Join(", ", transitivePackages) + ".";
                }

                PackageOption option = new PackageOption(package.name, package.version, !used, reason);
                if (selectedByName.TryGetValue(option.Name, out bool selected))
                {
                    option.Selected = selected;
                }

                optionalPackages.Add(option);
            }

            transitiveRegistryPackages.Clear();
            for (int i = 0; i < packages.Count; i++)
            {
                PMPackageInfo package = packages[i];
                if (!IsSupportedUnityPackageSource(package.source) || package.isDirectDependency)
                {
                    continue;
                }

                List<string> owners = FindLiveOwningDirectPackages(package.name, packagesByName);
                transitiveRegistryPackages.Add(owners.Count == 0
                    ? package.name
                    : $"{package.name} <- {string.Join(", ", owners)}");
            }
        }

        private static void SetRecommendedSelections<T>(List<T> items) where T : ToggleOption
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].Selected = items[i].Recommended;
            }
        }

        private static void SetSelection<T>(List<T> items, bool value) where T : ToggleOption
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].Selected = value;
            }
        }

        private static string GetManifestPath()
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, "Packages", "manifest.json");
        }

        private static string GetPackagesLockPath()
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, "Packages", "packages-lock.json");
        }

        private static List<PackageOption> BuildOptionalPackageOptions()
        {
            List<PackageOption> options = new List<PackageOption>();
            string manifestPath = GetManifestPath();
            if (!File.Exists(manifestPath))
            {
                return options;
            }

            Dictionary<string, string> dependencies = ParseDependencies(File.ReadAllText(manifestPath));
            Dictionary<string, LockedPackage> lockedPackages = ParseLockedPackages();
            HashSet<string> usedPackages = DetectUsedPackages();
            List<string> packageNames = new List<string>(lockedPackages.Keys);
            packageNames.Sort(StringComparer.Ordinal);

            for (int i = 0; i < packageNames.Count; i++)
            {
                string packageName = packageNames[i];
                if (Array.IndexOf(RequiredPackages, packageName) >= 0)
                {
                    continue;
                }

                if (!lockedPackages.TryGetValue(packageName, out LockedPackage package) || !string.Equals(package.Source, "registry", StringComparison.Ordinal) || package.Depth != 0)
                {
                    continue;
                }

                bool used = usedPackages.Contains(packageName);
                string reason = used
                    ? "Usage was detected in project code or asmdefs, so this stays unchecked by default."
                    : "No package-specific usage was detected in project code or asmdefs.";

                List<string> transitivePackages = CollectTransitivePackages(packageName, lockedPackages);
                if (transitivePackages.Count > 0)
                {
                    reason += " Removing it also clears: " + string.Join(", ", transitivePackages) + ".";
                }

                string version = dependencies.TryGetValue(packageName, out string directVersion) ? directVersion : package.Version;
                options.Add(new PackageOption(packageName, version, !used, reason));
            }

            return options;
        }

        private static List<string> BuildTransitiveRegistryPackages()
        {
            List<string> summaries = new List<string>();
            Dictionary<string, LockedPackage> lockedPackages = ParseLockedPackages();
            if (lockedPackages.Count == 0)
            {
                return summaries;
            }

            List<string> packageNames = new List<string>(lockedPackages.Keys);
            packageNames.Sort(StringComparer.Ordinal);
            for (int i = 0; i < packageNames.Count; i++)
            {
                string packageName = packageNames[i];
                if (!lockedPackages.TryGetValue(packageName, out LockedPackage package) || !string.Equals(package.Source, "registry", StringComparison.Ordinal) || package.Depth == 0)
                {
                    continue;
                }

                List<string> roots = FindOwningDirectPackages(packageName, lockedPackages);
                if (roots.Count == 0)
                {
                    summaries.Add(packageName);
                    continue;
                }

                summaries.Add($"{packageName} <- {string.Join(", ", roots)}");
            }

            return summaries;
        }

        private static List<string> CollectLiveTransitivePackages(string packageName, Dictionary<string, PMPackageInfo> packagesByName)
        {
            List<string> results = new List<string>();
            if (!packagesByName.TryGetValue(packageName, out PMPackageInfo root))
            {
                return results;
            }

            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal) { packageName };
            Queue<string> queue = new Queue<string>();
            EnqueuePackageDependencies(root, queue);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (!visited.Add(current) || !packagesByName.TryGetValue(current, out PMPackageInfo currentPackage))
                {
                    continue;
                }

                results.Add(current);
                EnqueuePackageDependencies(currentPackage, queue);
            }

            results.Sort(StringComparer.Ordinal);
            return results;
        }

        private static List<string> FindLiveOwningDirectPackages(string packageName, Dictionary<string, PMPackageInfo> packagesByName)
        {
            List<string> owners = new List<string>();
            List<string> packageNames = new List<string>(packagesByName.Keys);
            packageNames.Sort(StringComparer.Ordinal);
            for (int i = 0; i < packageNames.Count; i++)
            {
                string candidateName = packageNames[i];
                if (!packagesByName.TryGetValue(candidateName, out PMPackageInfo candidate) || !IsSupportedUnityPackageSource(candidate.source) || !candidate.isDirectDependency)
                {
                    continue;
                }

                List<string> transitivePackages = CollectLiveTransitivePackages(candidateName, packagesByName);
                if (transitivePackages.Contains(packageName))
                {
                    owners.Add(candidateName);
                }
            }

            return owners;
        }

        private static void EnqueuePackageDependencies(PMPackageInfo package, Queue<string> queue)
        {
            if (package.dependencies == null)
            {
                return;
            }

            for (int i = 0; i < package.dependencies.Length; i++)
            {
                queue.Enqueue(package.dependencies[i].name);
            }
        }

        private static bool IsSupportedUnityPackageSource(PackageSource source)
        {
            return source == PackageSource.Registry || source == PackageSource.BuiltIn;
        }

        private static Dictionary<string, LockedPackage> ParseLockedPackages()
        {
            Dictionary<string, LockedPackage> packages = new Dictionary<string, LockedPackage>(StringComparer.Ordinal);
            string lockPath = GetPackagesLockPath();
            if (!File.Exists(lockPath))
            {
                return packages;
            }

            string json = File.ReadAllText(lockPath);
            (int start, int end) = FindNamedObjectSpan(json, "dependencies");
            int index = start + 1;
            while (index < end)
            {
                SkipWhitespaceAndCommas(json, ref index, end);
                if (index >= end || json[index] != '"')
                {
                    break;
                }

                string packageName = ReadQuotedString(json, ref index);
                SkipWhitespace(json, ref index, end);
                if (index >= end || json[index] != ':')
                {
                    break;
                }

                index++;
                SkipWhitespace(json, ref index, end);
                if (index >= end || json[index] != '{')
                {
                    break;
                }

                int objectEnd = FindObjectEnd(json, index, end);
                string packageJson = json.Substring(index, objectEnd - index + 1);
                packages[packageName] = ParseLockedPackage(packageName, packageJson);
                index = objectEnd + 1;
            }

            return packages;
        }

        private static HashSet<string> DetectUsedPackages()
        {
            HashSet<string> usedPackages = new HashSet<string>(StringComparer.Ordinal);
            string assetsPath = Application.dataPath;
            if (!Directory.Exists(assetsPath))
            {
                return usedPackages;
            }

            string[] codeFiles = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);
            string[] asmdefFiles = Directory.GetFiles(assetsPath, "*.asmdef", SearchOption.AllDirectories);

            for (int i = 0; i < codeFiles.Length; i++)
            {
                MarkUsedPackages(codeFiles[i], true, usedPackages);
            }

            for (int i = 0; i < asmdefFiles.Length; i++)
            {
                MarkUsedPackages(asmdefFiles[i], false, usedPackages);
            }

            return usedPackages;
        }

        private static void MarkUsedPackages(string filePath, bool includeNamespaceTokens, HashSet<string> usedPackages)
        {
            string content = File.ReadAllText(filePath);
            for (int i = 0; i < PackageRules.Length; i++)
            {
                PackageRule rule = PackageRules[i];
                if (includeNamespaceTokens && ContainsAny(content, rule.NamespaceTokens))
                {
                    usedPackages.Add(rule.Name);
                    continue;
                }

                if (ContainsAny(content, rule.AssemblyTokens))
                {
                    usedPackages.Add(rule.Name);
                }
            }
        }

        private static bool ContainsAny(string content, string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (content.IndexOf(tokens[i], StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> CollectTransitivePackages(string packageName, Dictionary<string, LockedPackage> packages)
        {
            List<string> results = new List<string>();
            if (!packages.TryGetValue(packageName, out LockedPackage root))
            {
                return results;
            }

            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal) { packageName };
            Queue<string> queue = new Queue<string>();
            for (int i = 0; i < root.DependencyNames.Count; i++)
            {
                queue.Enqueue(root.DependencyNames[i]);
            }

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (!visited.Add(current) || !packages.TryGetValue(current, out LockedPackage currentPackage))
                {
                    continue;
                }

                results.Add(current);
                for (int i = 0; i < currentPackage.DependencyNames.Count; i++)
                {
                    queue.Enqueue(currentPackage.DependencyNames[i]);
                }
            }

            results.Sort(StringComparer.Ordinal);
            return results;
        }

        private static List<string> FindOwningDirectPackages(string packageName, Dictionary<string, LockedPackage> packages)
        {
            List<string> owners = new List<string>();
            List<string> packageNames = new List<string>(packages.Keys);
            packageNames.Sort(StringComparer.Ordinal);
            for (int i = 0; i < packageNames.Count; i++)
            {
                string candidateName = packageNames[i];
                if (!packages.TryGetValue(candidateName, out LockedPackage candidate) || !string.Equals(candidate.Source, "registry", StringComparison.Ordinal) || candidate.Depth != 0)
                {
                    continue;
                }

                List<string> transitivePackages = CollectTransitivePackages(candidateName, packages);
                if (transitivePackages.Contains(packageName))
                {
                    owners.Add(candidateName);
                }
            }

            return owners;
        }

        private static LockedPackage ParseLockedPackage(string packageName, string packageJson)
        {
            return new LockedPackage(
                packageName,
                MatchJsonString(packageJson, "version") ?? string.Empty,
                MatchJsonInt(packageJson, "depth"),
                MatchJsonString(packageJson, "source") ?? string.Empty,
                ParseNestedDependencyNames(packageJson));
        }

        private static List<string> ParseNestedDependencyNames(string json)
        {
            List<string> names = new List<string>();
            if (!TryFindNamedObjectSpan(json, "dependencies", out (int start, int end) span))
            {
                return names;
            }

            string dependenciesJson = json.Substring(span.start, span.end - span.start + 1);
            MatchCollection matches = Regex.Matches(dependenciesJson, "\"(?<name>[^\"]+)\"\\s*:");
            for (int i = 0; i < matches.Count; i++)
            {
                string name = matches[i].Groups["name"].Value;
                if (!string.Equals(name, "dependencies", StringComparison.Ordinal))
                {
                    names.Add(name);
                }
            }

            return names;
        }

        private static string MatchJsonString(string json, string key)
        {
            Match match = Regex.Match(json, $"\"{Regex.Escape(key)}\"\\s*:\\s*\"(?<value>[^\"]+)\"");
            return match.Success ? match.Groups["value"].Value : null;
        }

        private static int MatchJsonInt(string json, string key)
        {
            Match match = Regex.Match(json, $"\"{Regex.Escape(key)}\"\\s*:\\s*(?<value>\\d+)");
            return match.Success ? int.Parse(match.Groups["value"].Value) : 0;
        }

        private static string ReadQuotedString(string json, ref int index)
        {
            int start = ++index;
            while (index < json.Length)
            {
                if (json[index] == '"' && json[index - 1] != '\\')
                {
                    string value = json.Substring(start, index - start);
                    index++;
                    return value;
                }

                index++;
            }

            throw new InvalidOperationException("Quoted string could not be parsed.");
        }

        private static void SkipWhitespace(string json, ref int index, int end)
        {
            while (index < end && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        private static void SkipWhitespaceAndCommas(string json, ref int index, int end)
        {
            while (index < end && (char.IsWhiteSpace(json[index]) || json[index] == ','))
            {
                index++;
            }
        }

        private static int FindObjectEnd(string json, int objectStart, int end)
        {
            int depth = 0;
            for (int i = objectStart; i <= end; i++)
            {
                char ch = json[i];
                if (ch == '{')
                {
                    depth++;
                }
                else if (ch == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            throw new InvalidOperationException("JSON object is missing a closing brace.");
        }

        private static bool TryFindNamedObjectSpan(string json, string key, out (int start, int end) span)
        {
            int keyIndex = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
            if (keyIndex < 0)
            {
                span = default;
                return false;
            }

            int objectStart = json.IndexOf('{', keyIndex);
            if (objectStart < 0)
            {
                span = default;
                return false;
            }

            span = (objectStart, FindObjectEnd(json, objectStart, json.Length - 1));
            return true;
        }

        private static (int start, int end) FindNamedObjectSpan(string json, string key)
        {
            if (TryFindNamedObjectSpan(json, key, out (int start, int end) span))
            {
                return span;
            }

            throw new InvalidOperationException($"JSON does not contain an object named '{key}'.");
        }

        private static string GetRequiredPackageVersion(string packageName, Dictionary<string, string> dependencies)
        {
            if (dependencies.TryGetValue(packageName, out string currentVersion))
            {
                return currentVersion;
            }

            return GetRecommendedVersion(packageName);
        }

        private static string GetRecommendedVersion(string packageName)
        {
            return packageName switch
            {
                "com.unity.burst" => "1.8.29",
                "com.unity.collections" => "6.4.0",
                "com.unity.mathematics" => "1.3.3",
                _ => "latest",
            };
        }

        private static Dictionary<string, string> ParseDependencies(string manifestJson)
        {
            (int start, int end) = FindNamedObjectSpan(manifestJson, "dependencies");
            string objectJson = manifestJson.Substring(start, end - start + 1);
            MatchCollection matches = Regex.Matches(objectJson, "\"(?<name>[^\"]+)\"\\s*:\\s*\"(?<version>[^\"]+)\"");
            Dictionary<string, string> dependencies = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                dependencies[match.Groups["name"].Value] = match.Groups["version"].Value;
            }

            return dependencies;
        }

        private static string ReplaceDependenciesObject(string manifestJson, Dictionary<string, string> dependencies)
        {
            (int start, int end) = FindNamedObjectSpan(manifestJson, "dependencies");
            string replacement = BuildDependenciesObject(dependencies);
            return manifestJson.Substring(0, start) + replacement + manifestJson.Substring(end + 1);
        }

        private static string BuildDependenciesObject(Dictionary<string, string> dependencies)
        {
            List<string> keys = new List<string>(dependencies.Keys);
            keys.Sort(StringComparer.Ordinal);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                string trailingComma = i < keys.Count - 1 ? "," : string.Empty;
                builder.Append("    \"").Append(key).Append("\": \"").Append(dependencies[key]).Append('"').AppendLine(trailingComma);
            }

            builder.Append("  }");
            return builder.ToString();
        }

        private abstract class ToggleOption
        {
            protected ToggleOption(string name, bool recommended, string reason)
            {
                Name = name;
                Recommended = recommended;
                Reason = reason;
                Selected = recommended;
            }

            public string Name { get; }
            public bool Recommended { get; }
            public string Reason { get; }
            public bool Selected { get; set; }
        }

        private sealed class PackageOption : ToggleOption
        {
            public PackageOption(string name, string version, bool recommended, string reason)
                : base(name, recommended, reason)
            {
                Version = version;
            }

            public string Version { get; }
        }

        private sealed class PackageRule
        {
            public PackageRule(string name, string[] namespaceTokens, string[] assemblyTokens)
            {
                Name = name;
                NamespaceTokens = namespaceTokens;
                AssemblyTokens = assemblyTokens;
            }

            public string Name { get; }
            public string[] NamespaceTokens { get; }
            public string[] AssemblyTokens { get; }
        }

        private sealed class LockedPackage
        {
            public LockedPackage(string name, string version, int depth, string source, List<string> dependencyNames)
            {
                Name = name;
                Version = version;
                Depth = depth;
                Source = source;
                DependencyNames = dependencyNames;
            }

            public string Name { get; }
            public string Version { get; }
            public int Depth { get; }
            public string Source { get; }
            public List<string> DependencyNames { get; }
        }

        private sealed class ModuleOption : ToggleOption
        {
            public ModuleOption(string name, bool recommended, string reason)
                : base(name, recommended, reason)
            {
            }
        }

        private sealed class SetupPlan
        {
            public readonly List<string> PackagesToAdd = new List<string>();
            public readonly List<string> PackagesToRemove = new List<string>();
            public readonly List<string> ModulesToRemove = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public readonly List<PackageOption> RequiredPackagesToEnsure = new List<PackageOption>();

            public bool ManifestChanged => PackagesToAdd.Count > 0 || PackagesToRemove.Count > 0 || ModulesToRemove.Count > 0;

            public bool IsEmpty => !ManifestChanged;

            public string ToConfirmationText()
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Apply these Glai setup changes?");
                AppendSection(builder, "Packages To Add", PackagesToAdd);
                AppendSection(builder, "Packages To Remove", PackagesToRemove);
                AppendSection(builder, "Modules To Remove", ModulesToRemove);
                AppendSection(builder, "Warnings", Warnings);
                return builder.ToString().TrimEnd();
            }

            private static void AppendSection(StringBuilder builder, string title, List<string> entries)
            {
                if (entries.Count == 0)
                {
                    return;
                }

                builder.AppendLine();
                builder.AppendLine(title + ":");
                for (int i = 0; i < entries.Count; i++)
                {
                    builder.AppendLine("- " + entries[i]);
                }
            }
        }
    }
}
