// Glai/IndirectInstancedLit
// Standard PBR lighting (built-in pipeline) with indirect instanced transforms.
// Fragment shading is 100% from Unity Standard shader includes.
// Only vertex stage is custom: position/normal come from _PackedTransformBuffer.

Shader "Glai/IndirectInstancedLit"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        [Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Enum(UV0,0,UV1,1)] _UVSec("UV Set for secondary textures", Float) = 0

        [HideInInspector] _Mode("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
    }

    CGINCLUDE
        #define UNITY_SETUP_BRDF_INPUT MetallicSetup
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        // ------------------------------------------------------------------
        // Shared indirect helpers — included as a plain text block inside each
        // CGPROGRAM via the GlaiIndirectTransform.cginc file.
        // ------------------------------------------------------------------

        // ------------------------------------------------------------------
        // ForwardBase
        // ------------------------------------------------------------------
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma target 4.5
            #pragma vertex   vertBase
            #pragma fragment fragBase

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature_local _METALLICGLOSSMAP
            #pragma shader_feature_local _DETAIL_MULX2
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature_local _GLAI_Y_AXIS_ROTATION

            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityCG.cginc"
            #include "UnityIndirect.cginc"
            // Standard includes — these define VertexInput, VertexOutputForwardBase,
            // fragForwardBaseInternal, and all PBR helpers.
            #include "UnityStandardCore.cginc"

            // ---- indirect transform buffer ----
            struct TransformData { float3 position; uint2 packedQuaternion; };
            StructuredBuffer<TransformData> _PackedTransformBuffer;
            uint _ClusterSize;

            half4 UnpackRGBA16Snorm(uint xy, uint zw)
            {
                int4 r; r.x=int(xy<<16u)>>16; r.y=int(xy)>>16; r.z=int(zw<<16u)>>16; r.w=int(zw)>>16;
                return half4(r)/32767.0h;
            }
            half2 UnpackYAxis16Snorm(uint xy, uint zw)
            {
                int2 r; r.x=int(xy)>>16; r.y=int(zw)>>16;
                return half2(r)/32767.0h;
            }
            float3 RotQ(float3 v, half4 q) { float3 t=2.0*cross((float3)q.xyz,v); return v+q.w*t+cross((float3)q.xyz,t); }
            float3 RotY(float3 v, half2 r)  { half s=2.0h*r.x*r.y; half c=1.0h-2.0h*r.x*r.x; return float3(c*v.x+s*v.z,v.y,c*v.z-s*v.x); }
            float3 GlaiRotate(float3 v, TransformData td)
            {
                #if defined(_GLAI_Y_AXIS_ROTATION)
                    return RotY(v, UnpackYAxis16Snorm(td.packedQuaternion.x, td.packedQuaternion.y));
                #else
                    return RotQ(v, UnpackRGBA16Snorm(td.packedQuaternion.x, td.packedQuaternion.y));
                #endif
            }

            // Extended vertex input: clusterCoord in TEXCOORD3 to avoid clashing
            // with uv1 (TEXCOORD1) used by Standard for lightmapping.
            struct GlaiVertexInput
            {
                float4 vertex   : POSITION;
                half3  normal   : NORMAL;
                float2 uv0      : TEXCOORD0;
                float2 uv1      : TEXCOORD1;
                #if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
                float2 uv2      : TEXCOORD2;
                #endif
                #ifdef _TANGENT_TO_WORLD
                half4  tangent  : TANGENT;
                #endif
                float2 clusterCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // vertBase: our indirect vertex shader, outputs the same
            // VertexOutputForwardBase that fragForwardBaseInternal expects.
            VertexOutputForwardBase vertBase(GlaiVertexInput v, uint svInstanceID : SV_InstanceID)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                VertexOutputForwardBase o;
                UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                InitIndirectDrawArgs(0);
                uint instanceID     = GetIndirectInstanceID(svInstanceID);
                uint cubeIndex      = (uint)v.clusterCoord.x;
                uint transformIndex = instanceID * _ClusterSize + cubeIndex;
                TransformData td    = _PackedTransformBuffer[transformIndex];

                float3 posWS    = GlaiRotate(v.vertex.xyz, td) + td.position;
                float3 normalWS = normalize(GlaiRotate((float3)v.normal, td));
                float4 posWorld = float4(posWS, 1.0);

                o.pos = mul(UNITY_MATRIX_VP, posWorld);

                // World pos storage (needed by fragment for lighting)
                #if UNITY_REQUIRE_FRAG_WORLDPOS
                    #if UNITY_PACK_WORLDPOS_WITH_TANGENT
                        o.tangentToWorldAndPackedData[0].w = posWS.x;
                        o.tangentToWorldAndPackedData[1].w = posWS.y;
                        o.tangentToWorldAndPackedData[2].w = posWS.z;
                    #else
                        o.posWorld = posWS;
                    #endif
                #endif

                // UVs — forward to Standard's TexCoords() via a temp VertexInput
                VertexInput vi; UNITY_INITIALIZE_OUTPUT(VertexInput, vi);
                vi.uv0 = v.uv0; vi.uv1 = v.uv1;
                #if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
                vi.uv2 = v.uv2;
                #endif
                o.tex = TexCoords(vi);

                o.eyeVec.xyz = NormalizePerVertexNormal(posWS - _WorldSpaceCameraPos);

                // Tangent space
                #ifdef _TANGENT_TO_WORLD
                    float3 tangentWS  = normalize(GlaiRotate(v.tangent.xyz, td));
                    float3 binormalWS = cross(normalWS, tangentWS) * v.tangent.w;
                    o.tangentToWorldAndPackedData[0].xyz = tangentWS;
                    o.tangentToWorldAndPackedData[1].xyz = binormalWS;
                    o.tangentToWorldAndPackedData[2].xyz = normalWS;
                #else
                    o.tangentToWorldAndPackedData[0].xyz = 0;
                    o.tangentToWorldAndPackedData[1].xyz = 0;
                    o.tangentToWorldAndPackedData[2].xyz = normalWS;
                #endif

                // Shadow + lightmap coords
                // UNITY_TRANSFER_LIGHTING needs o and a vertex position.
                // We abuse vi.vertex to pass the world-space pos (already a float4).
                vi.vertex = posWorld;
                UNITY_TRANSFER_LIGHTING(o, v.uv1);

                // Ambient / SH / lightmap
                o.ambientOrLightmapUV = 0;
                #ifdef LIGHTMAP_ON
                    o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                #elif UNITY_SHOULD_SAMPLE_SH
                    o.ambientOrLightmapUV.rgb = ShadeSHPerVertex(normalWS, o.ambientOrLightmapUV.rgb);
                #endif
                #ifdef DYNAMICLIGHTMAP_ON
                    o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif

                UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o, o.pos);
                return o;
            }

            half4 fragBase(VertexOutputForwardBase i) : SV_Target
            {
                return fragForwardBaseInternal(i);
            }
            ENDCG
        }

        // ------------------------------------------------------------------
        // ForwardAdd
        // ------------------------------------------------------------------
        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend [_SrcBlend] One
            Fog { Color (0,0,0,0) }
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 4.5
            #pragma vertex   vertAdd
            #pragma fragment fragAdd

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _METALLICGLOSSMAP
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _DETAIL_MULX2
            #pragma shader_feature_local _GLAI_Y_AXIS_ROTATION

            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityCG.cginc"
            #include "UnityIndirect.cginc"
            #include "UnityStandardCore.cginc"

            struct TransformData { float3 position; uint2 packedQuaternion; };
            StructuredBuffer<TransformData> _PackedTransformBuffer;
            uint _ClusterSize;

            half4 UnpackRGBA16Snorm(uint xy, uint zw)
            {
                int4 r; r.x=int(xy<<16u)>>16; r.y=int(xy)>>16; r.z=int(zw<<16u)>>16; r.w=int(zw)>>16;
                return half4(r)/32767.0h;
            }
            half2 UnpackYAxis16Snorm(uint xy, uint zw)
            {
                int2 r; r.x=int(xy)>>16; r.y=int(zw)>>16;
                return half2(r)/32767.0h;
            }
            float3 RotQ(float3 v, half4 q) { float3 t=2.0*cross((float3)q.xyz,v); return v+q.w*t+cross((float3)q.xyz,t); }
            float3 RotY(float3 v, half2 r)  { half s=2.0h*r.x*r.y; half c=1.0h-2.0h*r.x*r.x; return float3(c*v.x+s*v.z,v.y,c*v.z-s*v.x); }
            float3 GlaiRotate(float3 v, TransformData td)
            {
                #if defined(_GLAI_Y_AXIS_ROTATION)
                    return RotY(v, UnpackYAxis16Snorm(td.packedQuaternion.x, td.packedQuaternion.y));
                #else
                    return RotQ(v, UnpackRGBA16Snorm(td.packedQuaternion.x, td.packedQuaternion.y));
                #endif
            }

            struct GlaiVertexInput
            {
                float4 vertex   : POSITION;
                half3  normal   : NORMAL;
                float2 uv0      : TEXCOORD0;
                float2 uv1      : TEXCOORD1;
                #ifdef _TANGENT_TO_WORLD
                half4  tangent  : TANGENT;
                #endif
                float2 clusterCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            VertexOutputForwardAdd vertAdd(GlaiVertexInput v, uint svInstanceID : SV_InstanceID)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                VertexOutputForwardAdd o;
                UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAdd, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                InitIndirectDrawArgs(0);
                uint instanceID     = GetIndirectInstanceID(svInstanceID);
                uint cubeIndex      = (uint)v.clusterCoord.x;
                uint transformIndex = instanceID * _ClusterSize + cubeIndex;
                TransformData td    = _PackedTransformBuffer[transformIndex];

                float3 posWS    = GlaiRotate(v.vertex.xyz, td) + td.position;
                float3 normalWS = normalize(GlaiRotate((float3)v.normal, td));
                float4 posWorld = float4(posWS, 1.0);

                o.pos      = mul(UNITY_MATRIX_VP, posWorld);
                o.posWorld = posWS;

                VertexInput vi; UNITY_INITIALIZE_OUTPUT(VertexInput, vi);
                vi.uv0 = v.uv0; vi.uv1 = v.uv1;
                o.tex = TexCoords(vi);
                o.eyeVec.xyz = NormalizePerVertexNormal(posWS - _WorldSpaceCameraPos);

                #ifdef _TANGENT_TO_WORLD
                    float3 tangentWS  = normalize(GlaiRotate(v.tangent.xyz, td));
                    float3 binormalWS = cross(normalWS, tangentWS) * v.tangent.w;
                    o.tangentToWorldAndLightDir[0].xyz = tangentWS;
                    o.tangentToWorldAndLightDir[1].xyz = binormalWS;
                    o.tangentToWorldAndLightDir[2].xyz = normalWS;
                #else
                    o.tangentToWorldAndLightDir[0].xyz = 0;
                    o.tangentToWorldAndLightDir[1].xyz = 0;
                    o.tangentToWorldAndLightDir[2].xyz = normalWS;
                #endif

                vi.vertex = posWorld;
                UNITY_TRANSFER_LIGHTING(o, v.uv1);

                float3 lightDir = _WorldSpaceLightPos0.xyz - posWS * _WorldSpaceLightPos0.w;
                #ifndef USING_DIRECTIONAL_LIGHT
                    lightDir = NormalizePerVertexNormal(lightDir);
                #endif
                o.tangentToWorldAndLightDir[0].w = lightDir.x;
                o.tangentToWorldAndLightDir[1].w = lightDir.y;
                o.tangentToWorldAndLightDir[2].w = lightDir.z;

                UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o, o.pos);
                return o;
            }

            half4 fragAdd(VertexOutputForwardAdd i) : SV_Target
            {
                return fragForwardAddInternal(i);
            }
            ENDCG
        }

        // ------------------------------------------------------------------
        // ShadowCaster
        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 4.5
            #pragma vertex   vertShadowCaster
            #pragma fragment fragShadowCaster

            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _METALLICGLOSSMAP
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _GLAI_Y_AXIS_ROTATION
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityCG.cginc"
            #include "UnityIndirect.cginc"

            struct TransformData { float3 position; uint2 packedQuaternion; };
            StructuredBuffer<TransformData> _PackedTransformBuffer;
            uint _ClusterSize;

            half4 UnpackRGBA16Snorm(uint xy, uint zw)
            {
                int4 r; r.x=int(xy<<16u)>>16; r.y=int(xy)>>16; r.z=int(zw<<16u)>>16; r.w=int(zw)>>16;
                return half4(r)/32767.0h;
            }
            half2 UnpackYAxis16Snorm(uint xy, uint zw)
            {
                int2 r; r.x=int(xy)>>16; r.y=int(zw)>>16;
                return half2(r)/32767.0h;
            }
            float3 RotQ(float3 v, half4 q) { float3 t=2.0*cross((float3)q.xyz,v); return v+q.w*t+cross((float3)q.xyz,t); }
            float3 RotY(float3 v, half2 r)  { half s=2.0h*r.x*r.y; half c=1.0h-2.0h*r.x*r.x; return float3(c*v.x+s*v.z,v.y,c*v.z-s*v.x); }
            float3 GlaiRotate(float3 v, TransformData td)
            {
                #if defined(_GLAI_Y_AXIS_ROTATION)
                    return RotY(v, UnpackYAxis16Snorm(td.packedQuaternion.x, td.packedQuaternion.y));
                #else
                    return RotQ(v, UnpackRGBA16Snorm(td.packedQuaternion.x, td.packedQuaternion.y));
                #endif
            }

            struct ShadowVertexInput
            {
                float4 vertex : POSITION;
                half3  normal : NORMAL;
                float2 uv0    : TEXCOORD0;
                float2 clusterCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 posCS : SV_POSITION;
                #if defined(_ALPHATEST_ON) || defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
                float2 uv    : TEXCOORD0;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            half4     _Color;
            half      _Cutoff;

            ShadowVaryings vertShadowCaster(ShadowVertexInput v, uint svInstanceID : SV_InstanceID)
            {
                ShadowVaryings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                InitIndirectDrawArgs(0);
                uint instanceID     = GetIndirectInstanceID(svInstanceID);
                uint cubeIndex      = (uint)v.clusterCoord.x;
                uint transformIndex = instanceID * _ClusterSize + cubeIndex;
                TransformData td    = _PackedTransformBuffer[transformIndex];

                float3 posWS    = GlaiRotate(v.vertex.xyz, td) + td.position;
                float3 normalWS = normalize(GlaiRotate((float3)v.normal, td));

                float3 lightDir  = normalize(UnityWorldSpaceLightDir(posWS));
                float  shadowCos = saturate(dot(normalWS, lightDir));
                float  shadowSin = sqrt(1.0 - shadowCos * shadowCos);
                posWS -= normalWS * (shadowSin * unity_LightShadowBias.z);

                o.posCS = UnityApplyLinearShadowBias(mul(UNITY_MATRIX_VP, float4(posWS, 1.0)));

                #if defined(_ALPHATEST_ON) || defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
                o.uv = TRANSFORM_TEX(v.uv0, _MainTex);
                #endif
                return o;
            }

            fixed4 fragShadowCaster(ShadowVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                #if defined(_ALPHATEST_ON)
                    clip(tex2D(_MainTex, i.uv).a * _Color.a - _Cutoff);
                #endif
                return 0;
            }
            ENDCG
        }
    }

    FallBack "VertexLit"
    CustomEditor "StandardShaderGUI"
}
