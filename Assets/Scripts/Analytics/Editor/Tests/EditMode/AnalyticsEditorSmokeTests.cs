using System;
using System.Reflection;
using Glai.Analytics.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Glai.Analytics.Editor.Tests.EditMode
{
    public class AnalyticsEditorSmokeTests
    {
        [SetUp]
        public void SetUp()
        {
            DisableLogAndWarning();
        }

        [TearDown]
        public void TearDown()
        {
            ResetLoggerChannels();
        }

        private static void DisableLogAndWarning()
        {
            var loggerType = Type.GetType("Glai.Core.Logger, Glai.Core");
            if (loggerType == null) return;

            loggerType.GetProperty("EnableLog")?.SetValue(null, false);
            loggerType.GetProperty("EnableWarning")?.SetValue(null, false);
        }

        private static void ResetLoggerChannels()
        {
            var loggerType = Type.GetType("Glai.Core.Logger, Glai.Core");
            if (loggerType == null) return;

            loggerType.GetMethod("ResetChannels")?.Invoke(null, null);
        }

        [Test]
        public void Open_CreatesMemoryAnalyticsWindow()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Pass("Editor window creation is not available in -nographics mode.");
            }

            MemoryAnalyticsWindow.Open();
            var window = EditorWindow.GetWindow<MemoryAnalyticsWindow>();

            Assert.IsNotNull(window);
            window.Close();
        }

        [Test]
        public void FormatBytes_FormatsKilobytesCorrectly()
        {
            MethodInfo method = typeof(MemoryAnalyticsWindow).GetMethod(
                "FormatBytes",
                BindingFlags.NonPublic | BindingFlags.Static);

            string formatted = (string)method.Invoke(null, new object[] { 2048 });

            Assert.AreEqual("2 KB", formatted);
        }
    }
}
