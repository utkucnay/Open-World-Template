using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Glai.Gameplay.Tests.EditMode
{
    public class IndirectInstancedShaderBindingTests
    {
        static readonly Regex s_VertexReadBufferPattern = new Regex(@"StructuredBuffer<\s*TransformData\s*>\s+(_\w+)\s*;", RegexOptions.Compiled);

        [Test]
        public void IndirectShader_ReadsPackedTransformBufferDirectly()
        {
            string drawShaderSource = ReadAssetText("Resources/Shaders/IndirectInstancedLit.shader");

            string drawBufferName = GetRequiredBufferName(s_VertexReadBufferPattern, drawShaderSource, "vertex shader input");

            Assert.That(drawBufferName, Is.EqualTo("_PackedTransformBuffer"));
            StringAssert.Contains("half4 UnpackRGBA16Snorm(uint xy, uint zw)", drawShaderSource);
            StringAssert.Contains("return _PackedTransformBuffer[transformIndex];", drawShaderSource);
        }

        [Test]
        public void IndirectShader_UsesBuiltInForwardPipelineAndMetallicWorkflowProperties()
        {
            string drawShaderSource = ReadAssetText("Resources/Shaders/IndirectInstancedLit.shader");

            StringAssert.Contains("Shader \"Glai/IndirectInstancedLit\"", drawShaderSource);
            StringAssert.DoesNotContain("\"RenderPipeline\" = \"UniversalPipeline\"", drawShaderSource);
            StringAssert.Contains("\"LightMode\" = \"ForwardBase\"", drawShaderSource);
            StringAssert.Contains("\"LightMode\" = \"ForwardAdd\"", drawShaderSource);
            StringAssert.Contains("\"LightMode\" = \"ShadowCaster\"", drawShaderSource);
            StringAssert.Contains("#include \"Lighting.cginc\"", drawShaderSource);
            StringAssert.Contains("#include \"AutoLight.cginc\"", drawShaderSource);
            StringAssert.Contains("_Metallic(\"Metallic\"", drawShaderSource);
            StringAssert.Contains("_Smoothness(\"Smoothness\"", drawShaderSource);
            StringAssert.Contains("_SpecGlossMap", drawShaderSource);
            StringAssert.Contains("_MetallicGlossMap", drawShaderSource);
        }

        [Test]
        public void IndirectShader_SupportsClusteredCubeMeshIndexing()
        {
            string drawShaderSource = ReadAssetText("Resources/Shaders/IndirectInstancedLit.shader");

            StringAssert.Contains("uint _ClusterSize;", drawShaderSource);
            StringAssert.Contains("float2 clusterCoord : TEXCOORD1;", drawShaderSource);
            StringAssert.Contains("uint cubeIndex = (uint)input.clusterCoord.x;", drawShaderSource);
            StringAssert.Contains("uint transformIndex = instanceID * _ClusterSize + cubeIndex;", drawShaderSource);
        }

        static string ReadAssetText(string relativeAssetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, relativeAssetPath);
            Assert.That(File.Exists(fullPath), Is.True, $"Expected asset at '{fullPath}'.");
            return File.ReadAllText(fullPath);
        }

        static string GetRequiredBufferName(Regex pattern, string source, string description)
        {
            Match match = pattern.Match(source);
            Assert.That(match.Success, Is.True, $"Could not find {description} declaration.");
            return match.Groups[1].Value;
        }
    }
}
