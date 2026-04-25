using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Glai.Gameplay.Tests.EditMode
{
    public class IndirectInstancedShaderBindingTests
    {
        static readonly Regex s_VertexReadBufferPattern = new Regex(
            @"StructuredBuffer<\s*TransformData\s*>\s+(_\w+)\s*;", RegexOptions.Compiled);

        [Test]
        public void IndirectShader_ReadsPackedTransformBufferDirectly()
        {
            string src = ReadAssetText("StarterContent/Resources/Shaders/IndirectInstancedLit.shader");

            string bufferName = GetRequiredBufferName(s_VertexReadBufferPattern, src, "TransformData buffer");
            Assert.That(bufferName, Is.EqualTo("_PackedTransformBuffer"));

            StringAssert.Contains("#pragma shader_feature_local _GLAI_Y_AXIS_ROTATION", src);
            StringAssert.Contains("half4 UnpackRGBA16Snorm(uint xy, uint zw)", src);
            StringAssert.Contains("half2 UnpackYAxis16Snorm(uint xy, uint zw)", src);
            StringAssert.Contains("return _PackedTransformBuffer[transformIndex];", src);
        }

        [Test]
        public void IndirectShader_UsesBuiltInForwardPipelineAndShadowPasses()
        {
            string src = ReadAssetText("StarterContent/Resources/Shaders/IndirectInstancedLit.shader");

            StringAssert.Contains("Shader \"Glai/IndirectInstancedLit\"", src);
            StringAssert.DoesNotContain("\"RenderPipeline\" = \"UniversalPipeline\"", src);

            StringAssert.Contains("\"LightMode\" = \"ForwardBase\"", src);
            StringAssert.Contains("\"LightMode\" = \"ForwardAdd\"", src);
            StringAssert.Contains("\"LightMode\" = \"ShadowCaster\"", src);

            // Standard PBR includes used in forward passes
            StringAssert.Contains("#include \"UnityStandardCore.cginc\"", src);
            // AutoLight is pulled in by UnityStandardCore, not explicitly listed here
            // (UnityStandardCore.cginc includes AutoLight.cginc internally)
        }

        [Test]
        public void IndirectShader_SupportsClusteredCubeMeshIndexing()
        {
            string src = ReadAssetText("StarterContent/Resources/Shaders/IndirectInstancedLit.shader");

            StringAssert.Contains("uint _ClusterSize;", src);
            StringAssert.Contains("float2 clusterCoord : TEXCOORD3;", src);
            StringAssert.Contains("uint cubeIndex      = (uint)v.clusterCoord.x;", src);
            StringAssert.Contains("uint transformIndex = instanceID * _ClusterSize + cubeIndex;", src);
        }

        [Test]
        public void IndirectShader_IsStandardPBRWithIndirectVertex()
        {
            string src = ReadAssetText("StarterContent/Resources/Shaders/IndirectInstancedLit.shader");

            // Standard properties present
            StringAssert.Contains("_Color", src);
            StringAssert.Contains("_MainTex", src);
            StringAssert.Contains("_Metallic", src);
            StringAssert.Contains("_Glossiness", src);
            StringAssert.Contains("_BumpMap", src);

            // Standard CustomEditor
            StringAssert.Contains("CustomEditor \"StandardShaderGUI\"", src);

            // Our custom vertex functions call fragForwardBaseInternal / fragForwardAddInternal
            StringAssert.Contains("fragForwardBaseInternal(i)", src);
            StringAssert.Contains("fragForwardAddInternal(i)", src);

            // Shadow pass has normal-bias and linear shadow bias
            StringAssert.Contains("UnityApplyLinearShadowBias(", src);
            StringAssert.Contains("unity_LightShadowBias.z", src);
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
