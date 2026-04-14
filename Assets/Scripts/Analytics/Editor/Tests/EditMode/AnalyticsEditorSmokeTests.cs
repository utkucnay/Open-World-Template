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
            Glai.Core.Logger.EnableLog = false;
            Glai.Core.Logger.EnableWarning = false;
        }

        private static void ResetLoggerChannels()
        {
            Glai.Core.Logger.ResetChannels();
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

            string formatted = (string)method.Invoke(null, new object[] { 2048L });

            Assert.AreEqual("2 KB", formatted);
        }

        [Test]
        public void FormatBytes_FormatsValuesLargerThanInt32()
        {
            MethodInfo method = typeof(MemoryAnalyticsWindow).GetMethod(
                "FormatBytes",
                BindingFlags.NonPublic | BindingFlags.Static);

            string formatted = (string)method.Invoke(null, new object[] { 3221225472L });

            Assert.AreEqual("3 GB", formatted);
        }
    }
}
