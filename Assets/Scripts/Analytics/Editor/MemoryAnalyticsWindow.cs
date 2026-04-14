using System;
using System.Collections.Generic;
using System.Reflection;
using Glai.Allocator.Core;
using UnityEditor;
using UnityEngine;

namespace Glai.Analytics.Editor
{
    public sealed class MemoryAnalyticsWindow : EditorWindow
    {
        private sealed class AnalyticsSnapshot
        {
            public List<AllocatorGroup> Groups = new List<AllocatorGroup>();
            public int TotalAllocated;
            public int TotalCapacity;
            public int AllocatorCount;
            public DateTime CapturedAt;
        }

        private sealed class AllocatorEntry
        {
            public string DisplayName;
            public int Allocated;
            public int Capacity;
        }

        private sealed class AllocatorGroup
        {
            public string BaseName;
            public readonly List<AllocatorEntry> Entries = new List<AllocatorEntry>();
            public int TotalAllocated;
            public int TotalCapacity;
        }

        private Vector2 scrollPosition;
        private readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        private AnalyticsSnapshot snapshot = new AnalyticsSnapshot();
        private bool isLive = true;

        private static readonly Color LightBackground = new Color(0.20f, 0.20f, 0.20f, 1f);
        private static readonly Color DarkBackground = new Color(0.13f, 0.13f, 0.13f, 1f);
        private static readonly Color UsageNormal = new Color(0.23f, 0.59f, 0.36f, 1f);
        private static readonly Color UsageWarning = new Color(0.87f, 0.67f, 0.20f, 1f);
        private static readonly Color UsageCritical = new Color(0.80f, 0.24f, 0.24f, 1f);

        [MenuItem("Tools/Glai/Memory Analytics")]
        public static void Open()
        {
            var window = GetWindow<MemoryAnalyticsWindow>("Memory Analytics");
            window.minSize = new Vector2(480f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            CaptureSnapshot();
        }

        private void OnInspectorUpdate()
        {
            if (isLive)
            {
                CaptureSnapshot();
            }

            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSummary(snapshot);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Allocators", EditorStyles.boldLabel);

            if (snapshot.AllocatorCount == 0)
            {
                string message = isLive
                    ? "No allocators are currently registered."
                    : "No allocator data in snapshot. Click Refresh to capture current state.";
                EditorGUILayout.HelpBox(message, MessageType.Info);
                return;
            }

            DrawAllocators(snapshot.Groups);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            bool previousLive = isLive;
            isLive = GUILayout.Toggle(isLive, "Live", EditorStyles.toolbarButton, GUILayout.Width(52f));
            if (isLive && !previousLive)
            {
                CaptureSnapshot();
            }

            using (new EditorGUI.DisabledScope(isLive))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    CaptureSnapshot();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(isLive ? "LIVE" : "SNAPSHOT", EditorStyles.miniBoldLabel);
            if (!isLive)
            {
                GUILayout.Space(10f);
                string stamp = snapshot.CapturedAt == default
                    ? "Last: -"
                    : $"Last: {snapshot.CapturedAt:HH:mm:ss}";
                GUILayout.Label(stamp, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummary(AnalyticsSnapshot state)
        {
            float totalUsage = state.TotalCapacity > 0 ? (float)state.TotalAllocated / state.TotalCapacity : 0f;
            totalUsage = Mathf.Clamp01(totalUsage);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Total", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Allocators: {state.AllocatorCount}");
                EditorGUILayout.LabelField($"Allocated: {FormatBytes(state.TotalAllocated)} / {FormatBytes(state.TotalCapacity)}");
                DrawUsageBar(
                    totalUsage,
                    $"Total: {FormatBytes(state.TotalAllocated)} / {FormatBytes(state.TotalCapacity)} ({totalUsage * 100f:0.00}%)");
            }
        }

        private void DrawAllocators(List<AllocatorGroup> groups)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < groups.Count; i++)
            {
                DrawGroup(groups[i]);
                EditorGUILayout.Space(8f);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroup(AllocatorGroup group)
        {
            float groupUsage = group.TotalCapacity > 0 ? (float)group.TotalAllocated / group.TotalCapacity : 0f;
            groupUsage = Mathf.Clamp01(groupUsage);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (group.Entries.Count == 1)
                {
                    AllocatorEntry single = group.Entries[0];
                    EditorGUILayout.LabelField(single.DisplayName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Allocated: {FormatBytes(single.Allocated)} / {FormatBytes(single.Capacity)}");
                    DrawUsageBar(groupUsage, $"{groupUsage * 100f:0.00}%");
                    return;
                }

                bool expanded = GetFoldoutState(group.BaseName);
                expanded = EditorGUILayout.Foldout(expanded, $"{group.BaseName} ({group.Entries.Count})", true);
                SetFoldoutState(group.BaseName, expanded);

                EditorGUILayout.LabelField($"Allocated: {FormatBytes(group.TotalAllocated)} / {FormatBytes(group.TotalCapacity)}");
                DrawUsageBar(
                    groupUsage,
                    $"Group Total: {FormatBytes(group.TotalAllocated)} / {FormatBytes(group.TotalCapacity)} ({groupUsage * 100f:0.00}%)");

                if (!expanded)
                {
                    return;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    for (int i = 0; i < group.Entries.Count; i++)
                    {
                        DrawAllocatorRow(group.Entries[i]);
                    }
                }
            }
        }

        private static void DrawAllocatorRow(AllocatorEntry entry)
        {
            float usage = entry.Capacity > 0 ? (float)entry.Allocated / entry.Capacity : 0f;
            usage = Mathf.Clamp01(usage);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(entry.DisplayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Allocated: {FormatBytes(entry.Allocated)} / {FormatBytes(entry.Capacity)}");
                DrawUsageBar(usage, $"{usage * 100f:0.00}%");
            }
        }

        private static void DrawUsageBar(float usage, string label)
        {
            Rect rect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));

            Color background = EditorGUIUtility.isProSkin ? DarkBackground : LightBackground;
            EditorGUI.DrawRect(rect, background);

            Color fill = usage >= 0.90f ? UsageCritical : usage >= 0.70f ? UsageWarning : UsageNormal;
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * usage, rect.height);
            EditorGUI.DrawRect(fillRect, fill);

            GUIStyle centered = new GUIStyle(EditorStyles.whiteMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            GUI.Label(rect, label, centered);
        }

        private void CaptureSnapshot()
        {
            IReadOnlyCollection<IAllocatorBase> allocators = GetAllocators();
            List<AllocatorGroup> groups = BuildGroups(allocators, out int totalAllocated, out int totalCapacity, out int allocatorCount);

            snapshot = new AnalyticsSnapshot
            {
                Groups = groups,
                TotalAllocated = totalAllocated,
                TotalCapacity = totalCapacity,
                AllocatorCount = allocatorCount,
                CapturedAt = DateTime.Now,
            };
        }

        private static IReadOnlyCollection<IAllocatorBase> GetAllocators()
        {
            Type analyticsType = Type.GetType("Glai.Analytics.MemoryAnalytics, Glai.Analytics");
            if (analyticsType == null)
            {
                return null;
            }

            MethodInfo method = analyticsType.GetMethod("GetCollections", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                return null;
            }

            object result = method.Invoke(null, null);
            return result as IReadOnlyCollection<IAllocatorBase>;
        }

        private List<AllocatorGroup> BuildGroups(
            IReadOnlyCollection<IAllocatorBase> allocators,
            out int totalAllocated,
            out int totalCapacity,
            out int allocatorCount)
        {
            var groups = new List<AllocatorGroup>();
            var groupByName = new Dictionary<string, AllocatorGroup>();

            totalAllocated = 0;
            totalCapacity = 0;
            allocatorCount = 0;

            if (allocators == null)
            {
                return groups;
            }

            foreach (IAllocatorBase allocator in allocators)
            {
                if (allocator == null)
                {
                    continue;
                }

                string displayName = allocator.Name.ToString();
                string baseName = displayName;
                if (string.IsNullOrEmpty(baseName))
                {
                    baseName = "Unnamed";
                    displayName = "Unnamed";
                }

                string[] split = baseName.Split('_');
                if (split.Length > 0 && !string.IsNullOrEmpty(split[0]))
                {
                    baseName = split[0];
                }

                if (!groupByName.TryGetValue(baseName, out AllocatorGroup group))
                {
                    group = new AllocatorGroup { BaseName = baseName };
                    groupByName.Add(baseName, group);
                    groups.Add(group);
                }

                int allocated = Mathf.Max(allocator.Count, 0);
                int capacity = Mathf.Max(allocator.Capacity, 0);

                var entry = new AllocatorEntry
                {
                    DisplayName = displayName,
                    Allocated = allocated,
                    Capacity = capacity,
                };

                group.Entries.Add(entry);
                group.TotalAllocated += allocated;
                group.TotalCapacity += capacity;
                totalAllocated += allocated;
                totalCapacity += capacity;
                allocatorCount++;
            }

            groups.Sort((a, b) => b.TotalAllocated.CompareTo(a.TotalAllocated));

            return groups;
        }

        private bool GetFoldoutState(string baseName)
        {
            if (!foldoutStates.TryGetValue(baseName, out bool expanded))
            {
                expanded = false;
                foldoutStates[baseName] = expanded;
            }

            return expanded;
        }

        private void SetFoldoutState(string baseName, bool expanded)
        {
            foldoutStates[baseName] = expanded;
        }

        private static string FormatBytes(int bytes)
        {
            if (bytes <= 0)
            {
                return "0 B";
            }

            string[] units = { "B", "KB", "MB", "GB" };
            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024d && unitIndex < units.Length - 1)
            {
                value /= 1024d;
                unitIndex++;
            }

            return $"{value:0.##} {units[unitIndex]}";
        }
    }
}
