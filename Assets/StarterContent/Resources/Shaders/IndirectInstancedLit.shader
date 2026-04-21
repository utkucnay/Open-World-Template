Shader "Glai/IndirectInstancedLit"
{
    Properties
    {
        _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2, 1.0)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Normal Scale", Float) = 1.0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}

        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }
        LOD 300

        CGINCLUDE
        #pragma enable_d3d11_debug_symbols
        #pragma multi_compile _ UNITY_DEVICE_SUPPORTS_NATIVE_16BIT
        #pragma multi_compile_instancing
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
        #include "UnityIndirect.cginc"

        struct TransformData
        {
            float3 position;
            uint2 packedQuaternion;
        };

        StructuredBuffer<TransformData> _PackedTransformBuffer;
        uint _ClusterSize;

        sampler2D _BaseMap;
        float4 _BaseMap_ST;
        fixed4 _BaseColor;
        half _Cutoff;

        sampler2D _MetallicGlossMap;
        half _Metallic;
        half _Smoothness;
        half _SmoothnessTextureChannel;

        sampler2D _BumpMap;
        half _BumpScale;

        sampler2D _OcclusionMap;
        half _OcclusionStrength;

        sampler2D _EmissionMap;
        fixed4 _EmissionColor;

        half _SpecularHighlights;
        half _GlossyReflections;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 texcoord : TEXCOORD0;
            float2 clusterCoord : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            half3 normalWS : TEXCOORD2;
            half4 tangentWS : TEXCOORD3;
            half3 bitangentWS : TEXCOORD4;
            UNITY_LIGHTING_COORDS(5, 6)
            UNITY_FOG_COORDS(7)
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct ShadowVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct SurfaceData
        {
            fixed3 albedo;
            half alpha;
            half3 normalWS;
            half metallic;
            half smoothness;
            half occlusion;
            fixed3 emission;
        };

        TransformData GetInstanceTransform(uint svInstanceID, uint cubeIndex)
        {
            InitIndirectDrawArgs(0);
            uint instanceID = GetIndirectInstanceID(svInstanceID);
            uint transformIndex = instanceID * _ClusterSize + cubeIndex;
            return _PackedTransformBuffer[transformIndex];
        }

        half4 UnpackRGBA16Snorm(uint xy, uint zw)
        {
            int4 raw;
            raw.x = int(xy << 16u) >> 16;
            raw.y = int(xy) >> 16;
            raw.z = int(zw << 16u) >> 16;
            raw.w = int(zw) >> 16;
            return half4(raw) / 32767.0h;
        }

        float3 RotateByQuaternion(float3 value, half4 rotation)
        {
            float3 q = rotation.xyz;
            float3 t = 2.0f * cross(q, value);
            return value + rotation.w * t + cross(q, t);
        }

        half4 GetWorldRotation(TransformData transformData)
        {
            return normalize(UnpackRGBA16Snorm(transformData.packedQuaternion.x, transformData.packedQuaternion.y));
        }

        float3 GetWorldPosition(float3 positionOS, TransformData transformData)
        {
            return RotateByQuaternion(positionOS, GetWorldRotation(transformData)) + transformData.position;
        }

        half3 GetWorldDirection(float3 directionOS, TransformData transformData)
        {
            return normalize((half3)RotateByQuaternion(directionOS, GetWorldRotation(transformData)));
        }

        inline half3 SampleNormalWS(float2 uv, half3 normalWS, half3 tangentWS, half tangentSign)
        {
            half3 tangentNormal = UnpackNormal(tex2D(_BumpMap, uv));
            tangentNormal.xy *= _BumpScale;
            half3 bitangentWS = normalize(cross(normalWS, tangentWS) * tangentSign);
            half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, normalWS);
            return normalize(mul(tangentNormal, tangentToWorld));
        }

        inline half GetPerceptualRoughness(half smoothness)
        {
            return saturate(1.0h - smoothness);
        }

        inline half3 GetF0(fixed3 albedo, half metallic)
        {
            return lerp(half3(0.04h, 0.04h, 0.04h), albedo, metallic);
        }

        inline half DistributionGGX(half NdotH, half roughness)
        {
            half a = roughness * roughness;
            half a2 = a * a;
            half denom = NdotH * NdotH * (a2 - 1.0h) + 1.0h;
            return a2 / max(UNITY_PI * denom * denom, 0.0001h);
        }

        inline half GeometrySchlickGGX(half NdotV, half roughness)
        {
            half k = ((roughness + 1.0h) * (roughness + 1.0h)) * 0.125h;
            return NdotV / lerp(k, 1.0h, NdotV);
        }

        inline half GeometrySmith(half NdotV, half NdotL, half roughness)
        {
            return GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);
        }

        inline half3 FresnelSchlick(half cosTheta, half3 f0)
        {
            return f0 + (1.0h - f0) * pow(1.0h - cosTheta, 5.0h);
        }

        inline SurfaceData InitializeSurfaceData(Varyings input)
        {
            SurfaceData surface;
            fixed4 albedoSample = tex2D(_BaseMap, input.uv) * _BaseColor;
            surface.albedo = albedoSample.rgb;
            surface.alpha = albedoSample.a;

            #if defined(_ALPHATEST_ON)
                clip(surface.alpha - _Cutoff);
            #endif

            #if defined(_NORMALMAP)
                surface.normalWS = SampleNormalWS(input.uv, normalize(input.normalWS), normalize(input.tangentWS.xyz), input.tangentWS.w);
            #else
                surface.normalWS = normalize(input.normalWS);
            #endif

            fixed4 metallicSample = tex2D(_MetallicGlossMap, input.uv);
            surface.metallic = saturate(_Metallic * metallicSample.r);
            surface.smoothness = saturate(lerp(_Smoothness, metallicSample.a, step(0.5h, _SmoothnessTextureChannel)));

            fixed occlusionSample = tex2D(_OcclusionMap, input.uv).g;
            surface.occlusion = lerp(1.0h, occlusionSample, _OcclusionStrength);
            surface.emission = tex2D(_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
            return surface;
        }

        inline half3 EvaluateDirectLighting(SurfaceData surface, half3 viewDirWS, half3 lightDirWS, fixed3 lightColor, half attenuation)
        {
            half3 normalWS = surface.normalWS;
            half3 halfVector = normalize(lightDirWS + viewDirWS);
            half NdotL = saturate(dot(normalWS, lightDirWS));
            half NdotV = saturate(dot(normalWS, viewDirWS));
            half NdotH = saturate(dot(normalWS, halfVector));
            half VdotH = saturate(dot(viewDirWS, halfVector));
            half roughness = max(GetPerceptualRoughness(surface.smoothness), 0.04h);
            half3 f0 = GetF0(surface.albedo, surface.metallic);
            half3 F = FresnelSchlick(VdotH, f0);
            half D = DistributionGGX(NdotH, roughness);
            half G = GeometrySmith(NdotV, NdotL, roughness);
            half3 specular = (D * G * F) / max(4.0h * NdotV * NdotL, 0.0001h);
            half3 kd = (1.0h - F) * (1.0h - surface.metallic);
            half3 diffuse = kd * surface.albedo / UNITY_PI;
            half3 direct = (diffuse + specular * _SpecularHighlights) * lightColor * (NdotL * attenuation);
            return direct;
        }

        inline fixed3 EvaluateIndirectLighting(SurfaceData surface, half3 viewDirWS)
        {
            half3 normalWS = surface.normalWS;
            half3 sh = ShadeSH9(float4(normalWS, 1.0f));
            half3 diffuse = sh * surface.albedo * (1.0h - surface.metallic);

            if (_GlossyReflections <= 0.0h)
                return diffuse * surface.occlusion;

            half3 reflected = reflect(-viewDirWS, normalWS);
            half mip = GetPerceptualRoughness(surface.smoothness) * UNITY_SPECCUBE_LOD_STEPS;
            half4 encoded = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflected, mip);
            half3 specularProbe = DecodeHDR(encoded, unity_SpecCube0_HDR);
            half3 fresnel = FresnelSchlick(saturate(dot(normalWS, viewDirWS)), GetF0(surface.albedo, surface.metallic));
            return (diffuse + specularProbe * fresnel * surface.smoothness) * surface.occlusion;
        }

        inline Varyings BuildVertexData(Attributes input, uint svInstanceID)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);

            uint cubeIndex = (uint)input.clusterCoord.x;
            TransformData transformData = GetInstanceTransform(svInstanceID, cubeIndex);
            float3 positionWS = GetWorldPosition(input.positionOS.xyz, transformData);
            half3 normalWS = GetWorldDirection(input.normalOS, transformData);
            half3 tangentWS = GetWorldDirection(input.tangentOS.xyz, transformData);

            output.positionCS = UnityWorldToClipPos(positionWS);
            output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
            output.positionWS = positionWS;
            output.normalWS = normalWS;
            output.tangentWS = half4(tangentWS, input.tangentOS.w);
            TRANSFER_VERTEX_TO_FRAGMENT(output);
            UNITY_TRANSFER_FOG(output, output.positionCS);
            return output;
        }

        Varyings ForwardBasePassVertex(Attributes input, uint svInstanceID : SV_InstanceID)
        {
            return BuildVertexData(input, svInstanceID);
        }

        fixed4 ForwardBasePassFragment(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);

            SurfaceData surface = InitializeSurfaceData(input);
            half3 viewDirWS = normalize(UnityWorldSpaceViewDir(input.positionWS));
            half shadow = SHADOW_ATTENUATION(input);
            half3 lightDirWS = normalize(UnityWorldSpaceLightDir(input.positionWS));

            fixed3 color = EvaluateIndirectLighting(surface, viewDirWS);
            color += EvaluateDirectLighting(surface, viewDirWS, lightDirWS, _LightColor0.rgb, shadow);
            color += surface.emission;

            UNITY_APPLY_FOG(input.fogCoord, color);
            return fixed4(color, surface.alpha);
        }

        Varyings ForwardAddPassVertex(Attributes input, uint svInstanceID : SV_InstanceID)
        {
            return BuildVertexData(input, svInstanceID);
        }

        fixed4 ForwardAddPassFragment(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);

            SurfaceData surface = InitializeSurfaceData(input);
            half3 viewDirWS = normalize(UnityWorldSpaceViewDir(input.positionWS));
            half3 lightDirWS = normalize(UnityWorldSpaceLightDir(input.positionWS));
            UNITY_LIGHT_ATTENUATION(attenuation, input, input.positionWS);

            fixed3 color = EvaluateDirectLighting(surface, viewDirWS, lightDirWS, _LightColor0.rgb, attenuation);
            return fixed4(color, 0.0h);
        }

        ShadowVaryings ShadowCasterPassVertex(Attributes input, uint svInstanceID : SV_InstanceID)
        {
            ShadowVaryings output = (ShadowVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);

            uint cubeIndex = (uint)input.clusterCoord.x;
            TransformData transformData = GetInstanceTransform(svInstanceID, cubeIndex);
            float3 positionWS = GetWorldPosition(input.positionOS.xyz, transformData);

            output.positionCS = UnityWorldToClipPos(positionWS);
            output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
            return output;
        }

        fixed4 ShadowCasterPassFragment(ShadowVaryings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);

            #if defined(_ALPHATEST_ON)
                fixed alpha = (tex2D(_BaseMap, input.uv) * _BaseColor).a;
                clip(alpha - _Cutoff);
            #endif

            return 0;
        }
        ENDCG

        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode" = "ForwardBase" }

            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile _ UNITY_DEVICE_SUPPORTS_NATIVE_16BIT
            #pragma vertex ForwardBasePassVertex
            #pragma fragment ForwardBasePassFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            ENDCG
        }

        Pass
        {
            Name "ForwardAdd"
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            Cull Back
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile _ UNITY_DEVICE_SUPPORTS_NATIVE_16BIT
            #pragma vertex ForwardAddPassVertex
            #pragma fragment ForwardAddPassFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_fwdadd_fullshadows
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Back
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile _ UNITY_DEVICE_SUPPORTS_NATIVE_16BIT
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_shadowcaster
            ENDCG
        }
    }

    FallBack "VertexLit"
}
