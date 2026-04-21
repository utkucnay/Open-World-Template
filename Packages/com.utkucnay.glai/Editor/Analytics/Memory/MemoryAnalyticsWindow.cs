using System;
using System.Collections.Generic;
using System.Reflection;
using Glai.Allocator;
using UnityEditor;
using UnityEngine;

namespace Glai.Analytics.Memory.Editor
{
    public sealed class MemoryAnalyticsWindow : EditorWindow
    {
        private const int NumericGroupPageSize = 10;

        private enum UsageDisplayMode
        {
            Current,
            Peak,
        }

        private sealed class AnalyticsSnapshot
        {
            public List<AllocatorNode> Groups = new List<AllocatorNode>();
            public long TotalAllocated;
            public long TotalPeakAllocated;
            public long TotalCapacity;
            public long TotalHandleCount;
            public long TotalPeakHandleCount;
            public long TotalHandleCapacity;
            public int AllocatorCount;
            public DateTime CapturedAt;
        }

        private sealed class AllocatorEntry
        {
            public string DisplayName;
            public long Allocated;
            public long PeakAllocated;
            public long Capacity;
            public long HandleCount;
            public long PeakHandleCount;
            public long HandleCapacity;
        }

        private sealed class AllocatorNode
        {
            public string Name;
            public string PathKey;
            public AllocatorNode Parent;
            public AllocatorEntry Entry;
            public readonly List<AllocatorNode> Children = new List<AllocatorNode>();
            public long TotalAllocated;
            public long TotalPeakAllocated;
            public long TotalCapacity;
            public long TotalHandleCount;
            public long TotalPeakHandleCount;
            public long TotalHandleCapacity;
        }

        private Vector2 scrollPosition;
        private readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> groupPageStates = new Dictionary<string, int>();
        private AnalyticsSnapshot snapshot = new AnalyticsSnapshot();
        private bool isLive = true;
        private UsageDisplayMode usageDisplayMode;

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

            if (GUILayout.Button("Reset Peaks", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            {
                ResetAllocatorPeaks();
                CaptureSnapshot();
            }

            GUILayout.Space(8f);
            usageDisplayMode = (UsageDisplayMode)GUILayout.Toolbar(
                (int)usageDisplayMode,
                new[] { "Current", "Peak" },
                EditorStyles.toolbarButton,
                GUILayout.Width(140f));

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
            long usageBytes = GetDisplayAllocated(state.TotalAllocated, state.TotalPeakAllocated);
            float totalUsage = state.TotalCapacity > 0 ? (float)usageBytes / state.TotalCapacity : 0f;
            totalUsage = Mathf.Clamp01(totalUsage);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Total", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Allocators: {state.AllocatorCount}");
                EditorGUILayout.LabelField($"Allocated: {FormatBytes(usageBytes)} / {FormatBytes(state.TotalCapacity)}");
                EditorGUILayout.LabelField($"Handles: {GetDisplayHandles(state.TotalHandleCount, state.TotalPeakHandleCount)} / {state.TotalHandleCapacity}");
                DrawUsageBar(
                    totalUsage,
                    GetUsageBarLabel(usageBytes, state.TotalCapacity, totalUsage));
            }
        }

        private void DrawAllocators(List<AllocatorNode> groups)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < groups.Count; i++)
            {
                DrawNode(groups[i]);
                EditorGUILayout.Space(8f);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawNode(AllocatorNode node)
        {
            long usageBytes = GetDisplayAllocated(node.TotalAllocated, node.TotalPeakAllocated);
            float groupUsage = node.TotalCapacity > 0 ? (float)usageBytes / node.TotalCapacity : 0f;
            groupUsage = Mathf.Clamp01(groupUsage);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (node.Children.Count == 0 && node.Entry != null)
                {
                    AllocatorEntry single = node.Entry;
                    EditorGUILayout.LabelField(single.DisplayName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Allocated: {FormatBytes(GetDisplayAllocated(single.Allocated, single.PeakAllocated))} / {FormatBytes(single.Capacity)}");
                    EditorGUILayout.LabelField($"Handles: {GetDisplayHandles(single.HandleCount, single.PeakHandleCount)} / {single.HandleCapacity}");
                    DrawUsageBar(groupUsage, GetUsageBarLabel(GetDisplayAllocated(single.Allocated, single.PeakAllocated), single.Capacity, groupUsage));
                    return;
                }

                bool expanded = GetFoldoutState(node.PathKey);
                expanded = EditorGUILayout.Foldout(expanded, $"{node.Name} ({GetLeafEntryCount(node)})", true);
                SetFoldoutState(node.PathKey, expanded);

                EditorGUILayout.LabelField($"Allocated: {FormatBytes(usageBytes)} / {FormatBytes(node.TotalCapacity)}");
                EditorGUILayout.LabelField($"Handles: {GetDisplayHandles(node.TotalHandleCount, node.TotalPeakHandleCount)} / {node.TotalHandleCapacity}");
                DrawUsageBar(
                    groupUsage,
                    GetUsageBarLabel(usageBytes, node.TotalCapacity, groupUsage));

                if (!expanded)
                {
                    return;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    int startIndex = 0;
                    int endIndex = node.Children.Count;
                    bool usePaging = ShouldPageChildren(node.Children);

                    if (usePaging)
                    {
                        int pageIndex = GetGroupPage(node.PathKey, node.Children.Count, NumericGroupPageSize);
                        startIndex = GetPageStartIndex(pageIndex, node.Children.Count, NumericGroupPageSize);
                        endIndex = Math.Min(startIndex + NumericGroupPageSize, node.Children.Count);
                    }

                    for (int i = startIndex; i < endIndex; i++)
                    {
                        DrawNode(node.Children[i]);
                    }

                    if (usePaging)
                    {
                        DrawGroupPaging(node.PathKey, GetGroupPage(node.PathKey, node.Children.Count, NumericGroupPageSize), startIndex, endIndex, node.Children.Count, NumericGroupPageSize);
                    }
                }
            }
        }

        private void DrawAllocatorRow(AllocatorEntry entry)
        {
            long usageBytes = GetDisplayAllocated(entry.Allocated, entry.PeakAllocated);
            float usage = entry.Capacity > 0 ? (float)usageBytes / entry.Capacity : 0f;
            usage = Mathf.Clamp01(usage);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(entry.DisplayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Allocated: {FormatBytes(usageBytes)} / {FormatBytes(entry.Capacity)}");
                EditorGUILayout.LabelField($"Handles: {GetDisplayHandles(entry.HandleCount, entry.PeakHandleCount)} / {entry.HandleCapacity}");
                DrawUsageBar(usage, GetUsageBarLabel(usageBytes, entry.Capacity, usage));
            }
        }

        private void DrawGroupPaging(string pageKey, int pageIndex, int startIndex, int endIndex, int totalEntries, int pageSize)
        {
            if (totalEntries <= pageSize)
            {
                return;
            }

            int pageCount = GetPageCount(totalEntries, pageSize);

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(pageIndex <= 0))
            {
                if (GUILayout.Button("Prev", GUILayout.Width(60f)))
                {
                    SetGroupPage(pageKey, pageIndex - 1);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{startIndex + 1}-{endIndex} / {totalEntries} (Page {pageIndex + 1}/{pageCount})", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(pageIndex >= pageCount - 1))
            {
                if (GUILayout.Button("Next", GUILayout.Width(60f)))
                {
                    SetGroupPage(pageKey, pageIndex + 1);
                }
            }

            EditorGUILayout.EndHorizontal();
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
            IReadOnlyCollection<IAllocator> allocators = GetAllocators();
            List<AllocatorNode> groups = BuildGroups(
                allocators,
                out long totalAllocated,
                out long totalPeakAllocated,
                out long totalCapacity,
                out long totalHandleCount,
                out long totalPeakHandleCount,
                out long totalHandleCapacity,
                out int allocatorCount);

            snapshot = new AnalyticsSnapshot
            {
                Groups = groups,
                TotalAllocated = totalAllocated,
                TotalPeakAllocated = totalPeakAllocated,
                TotalCapacity = totalCapacity,
                TotalHandleCount = totalHandleCount,
                TotalPeakHandleCount = totalPeakHandleCount,
                TotalHandleCapacity = totalHandleCapacity,
                AllocatorCount = allocatorCount,
                CapturedAt = DateTime.Now,
            };
        }

        private static IReadOnlyCollection<IAllocator> GetAllocators()
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
            return result as IReadOnlyCollection<IAllocator>;
        }

        private List<AllocatorNode> BuildGroups(
            IReadOnlyCollection<IAllocator> allocators,
            out long totalAllocated,
            out long totalPeakAllocated,
            out long totalCapacity,
            out long totalHandleCount,
            out long totalPeakHandleCount,
            out long totalHandleCapacity,
            out int allocatorCount)
        {
            var groups = new List<AllocatorNode>();
            var rootNodes = new Dictionary<string, AllocatorNode>();

            totalAllocated = 0;
            totalPeakAllocated = 0;
            totalCapacity = 0;
            totalHandleCount = 0;
            totalPeakHandleCount = 0;
            totalHandleCapacity = 0;
            allocatorCount = 0;

            if (allocators == null)
            {
                return groups;
            }

            foreach (IAllocator allocator in allocators)
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

                long allocated = Math.Max(allocator.Count, 0);
                long peakAllocated = Math.Max(allocator.PeakCount, 0);
                long capacity = Math.Max(allocator.Capacity, 0);
                long handleCount = Math.Max(allocator.HandleCount, 0);
                long peakHandleCount = Math.Max(allocator.PeakHandleCount, 0);
                long handleCapacity = Math.Max(allocator.HandleCapacity, 0);

                var entry = new AllocatorEntry
                {
                    DisplayName = displayName,
                    Allocated = allocated,
                    PeakAllocated = peakAllocated,
                    Capacity = capacity,
                    HandleCount = handleCount,
                    PeakHandleCount = peakHandleCount,
                    HandleCapacity = handleCapacity,
                };

                string[] parts = SplitNameParts(baseName);
                AllocatorNode node = GetOrCreateNode(rootNodes, groups, parts);
                node.Entry = entry;

                for (AllocatorNode current = node; current != null; current = current.Parent)
                {
                    current.TotalAllocated += allocated;
                    current.TotalPeakAllocated += peakAllocated;
                    current.TotalCapacity += capacity;
                    current.TotalHandleCount += handleCount;
                    current.TotalPeakHandleCount += peakHandleCount;
                    current.TotalHandleCapacity += handleCapacity;
                }

                totalAllocated += allocated;
                totalPeakAllocated += peakAllocated;
                totalCapacity += capacity;
                totalHandleCount += handleCount;
                totalPeakHandleCount += peakHandleCount;
                totalHandleCapacity += handleCapacity;
                allocatorCount++;
            }

            SortNodes(groups);

            return groups;
        }

        private static void ResetAllocatorPeaks()
        {
            Type analyticsType = Type.GetType("Glai.Analytics.MemoryAnalytics, Glai.Analytics");
            if (analyticsType == null)
            {
                return;
            }

            MethodInfo method = analyticsType.GetMethod("ResetPeaks", BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, null);
        }

        private static string[] SplitNameParts(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                return new[] { "Unnamed" };
            }

            string[] parts = displayName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? new[] { "Unnamed" } : parts;
        }

        private static bool ShouldPageChildren(List<AllocatorNode> children)
        {
            if (children.Count <= NumericGroupPageSize)
            {
                return false;
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (!int.TryParse(children[i].Name, out _))
                {
                    return false;
                }
            }

            return true;
        }

        private static AllocatorNode GetOrCreateNode(
            Dictionary<string, AllocatorNode> rootNodes,
            List<AllocatorNode> groups,
            string[] parts)
        {
            string pathKey = parts[0];
            if (!rootNodes.TryGetValue(pathKey, out AllocatorNode current))
            {
                current = new AllocatorNode
                {
                    Name = parts[0],
                    PathKey = pathKey,
                };
                rootNodes.Add(pathKey, current);
                groups.Add(current);
            }

            for (int i = 1; i < parts.Length; i++)
            {
                pathKey = $"{pathKey}_{parts[i]}";
                AllocatorNode child = current.Children.Find(node => node.Name == parts[i]);
                if (child == null)
                {
                    child = new AllocatorNode
                    {
                        Name = parts[i],
                        PathKey = pathKey,
                        Parent = current,
                    };
                    current.Children.Add(child);
                }

                current = child;
            }

            return current;
        }

        private void SortNodes(List<AllocatorNode> nodes)
        {
            nodes.Sort(CompareNodesForDisplayMode);

            for (int i = 0; i < nodes.Count; i++)
            {
                SortNodes(nodes[i].Children);
            }
        }

        private int CompareNodesForDisplayMode(AllocatorNode left, AllocatorNode right)
        {
            long leftAllocated = GetDisplayAllocated(left.TotalAllocated, left.TotalPeakAllocated);
            long rightAllocated = GetDisplayAllocated(right.TotalAllocated, right.TotalPeakAllocated);
            float leftUsage = left.TotalCapacity > 0 ? (float)leftAllocated / left.TotalCapacity : 0f;
            float rightUsage = right.TotalCapacity > 0 ? (float)rightAllocated / right.TotalCapacity : 0f;

            int usageComparison = rightUsage.CompareTo(leftUsage);
            if (usageComparison != 0)
            {
                return usageComparison;
            }

            if (int.TryParse(left.Name, out int leftIndex) &&
                int.TryParse(right.Name, out int rightIndex) &&
                leftIndex != rightIndex)
            {
                return leftIndex.CompareTo(rightIndex);
            }

            return string.Compare(left.Name, right.Name, StringComparison.Ordinal);
        }

        private int GetGroupPage(string pageKey, int totalEntries, int pageSize)
        {
            int maxPage = Math.Max(GetPageCount(totalEntries, pageSize) - 1, 0);
            int pageIndex = 0;
            if (groupPageStates.TryGetValue(pageKey, out int currentPage))
            {
                pageIndex = Mathf.Clamp(currentPage, 0, maxPage);
            }

            groupPageStates[pageKey] = pageIndex;
            return pageIndex;
        }

        private void SetGroupPage(string pageKey, int pageIndex)
        {
            groupPageStates[pageKey] = Math.Max(pageIndex, 0);
        }

        private static int GetPageCount(int totalEntries, int pageSize)
        {
            if (pageSize <= 0 || totalEntries <= 0)
            {
                return 1;
            }

            return (totalEntries + pageSize - 1) / pageSize;
        }

        private static int GetPageStartIndex(int pageIndex, int totalEntries, int pageSize)
        {
            if (totalEntries <= 0 || pageSize <= 0)
            {
                return 0;
            }

            int maxPage = Math.Max(GetPageCount(totalEntries, pageSize) - 1, 0);
            int clampedPage = Mathf.Clamp(pageIndex, 0, maxPage);
            return clampedPage * pageSize;
        }

        private static int GetLeafEntryCount(AllocatorNode node)
        {
            int count = node.Entry != null ? 1 : 0;

            for (int i = 0; i < node.Children.Count; i++)
            {
                count += GetLeafEntryCount(node.Children[i]);
            }

            return count;
        }

        private long GetDisplayAllocated(long currentAllocated, long peakAllocated)
        {
            return usageDisplayMode == UsageDisplayMode.Peak ? peakAllocated : currentAllocated;
        }

        private long GetDisplayHandles(long currentHandles, long peakHandles)
        {
            return usageDisplayMode == UsageDisplayMode.Peak ? peakHandles : currentHandles;
        }

        private string GetUsageBarLabel(long allocated, long capacity, float usage)
        {
            return $"{FormatBytes(allocated)} / {FormatBytes(capacity)} ({usage * 100f:0.00}%)";
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

        private static string FormatBytes(long bytes)
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
