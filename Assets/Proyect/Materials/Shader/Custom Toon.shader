Shader "Rii/Toon Lit Shadows"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2

        [Header(Toon Lighting)]
        _ShadowColor ("Dark Band Tint", Color) = (0.18,0.19,0.26,1)
        _MidColor ("Middle Band Tint", Color) = (0.55,0.57,0.68,1)
        _ShadowThreshold ("Dark to Middle", Range(0,1)) = 0.3
        _LightThreshold ("Middle to Light", Range(0,1)) = 0.65
        _BandSmoothness ("Band Softness", Range(0.001,0.25)) = 0.025
        _LightStrength ("Direct Light Strength", Range(0,4)) = 1
        [Toggle] _ReceiveShadows ("Receive Shadows", Float) = 1

        [Header(Ambient)]
        _AmbientColor ("Ambient Color", Color) = (0.12,0.13,0.16,1)
        _AmbientStrength ("Ambient Strength", Range(0,2)) = 0.25
        _EnvironmentStrength ("Environment Light", Range(0,2)) = 0.15

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 0
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,2)) = 1

        [Header(Specular)]
        [HDR] _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularStrength ("Specular Strength", Range(0,2)) = 0.15
        _SpecularPower ("Specular Sharpness", Range(1,256)) = 64
        _SpecularThreshold ("Specular Cutoff", Range(0,1)) = 0.5
        _SpecularSmoothness ("Specular Softness", Range(0.001,0.25)) = 0.03

        [Header(Emission)]
        [Toggle(_EMISSION)] _UseEmission ("Use Emission", Float) = 0
        _EmissionMap ("Emission Texture", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionStrength ("Emission Strength", Range(0,10)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_NormalMap);
        SAMPLER(sampler_NormalMap);
        TEXTURE2D(_EmissionMap);
        SAMPLER(sampler_EmissionMap);

        // The same UnityPerMaterial layout is shared by ALL passes (SRP Batcher).
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            half4 _MidColor;
            half4 _AmbientColor;
            half4 _SpecularColor;
            half4 _EmissionColor;
            half _Cutoff;
            half _ShadowThreshold;
            half _LightThreshold;
            half _BandSmoothness;
            half _LightStrength;
            half _ReceiveShadows;
            half _AmbientStrength;
            half _EnvironmentStrength;
            half _NormalStrength;
            half _SpecularStrength;
            half _SpecularPower;
            half _SpecularThreshold;
            half _SpecularSmoothness;
            half _EmissionStrength;
            half _AlphaClip;
            half _UseNormalMap;
            half _UseEmission;
            half _Cull;
        CBUFFER_END

        half GetSurfaceAlpha(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
        }

        void ApplyAlphaClip(float2 uv)
        {
            #if defined(_ALPHATEST_ON)
                clip(GetSurfaceAlpha(uv) - _Cutoff);
            #endif
        }
        ENDHLSL

        // Main surface: reacts to the Directional Light and receives real shadows.
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonVert
            #pragma fragment ToonFrag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 tangentWS : TEXCOORD2;
                half3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ToonVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);

                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.tangentWS = n.tangentWS;
                o.bitangentWS = n.bitangentWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half3 ToonBands(half lightAmount)
            {
                // Keep thresholds in order even if the inspector values cross.
                half first = min(_ShadowThreshold, _LightThreshold);
                half second = max(_ShadowThreshold, _LightThreshold);
                half mid = smoothstep(first - _BandSmoothness, first + _BandSmoothness, lightAmount);
                half high = smoothstep(second - _BandSmoothness, second + _BandSmoothness, lightAmount);
                return lerp(lerp(_ShadowColor.rgb, _MidColor.rgb, mid), half3(1,1,1), high);
            }

            half3 SpecularToon(half3 normalWS, half3 viewWS, Light light)
            {
                half3 H = SafeNormalize(viewWS + light.direction);
                half spec = pow(saturate(dot(normalWS, H)), max(_SpecularPower, 1.0h));
                spec = smoothstep(_SpecularThreshold - _SpecularSmoothness,
                                  _SpecularThreshold + _SpecularSmoothness, spec);
                return _SpecularColor.rgb * spec * _SpecularStrength * light.color
                       * light.distanceAttenuation * light.shadowAttenuation;
            }

            half4 ToonFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                ApplyAlphaClip(i.uv);

                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 albedo = texel.rgb * _BaseColor.rgb;
                half3 N = normalize(i.normalWS);
                half3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);

                #if defined(_NORMALMAP)
                    half3 normalTS = UnpackNormalScale(
                        SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv), _NormalStrength);
                    N = normalize(normalTS.x * normalize(i.tangentWS)
                                + normalTS.y * normalize(i.bitangentWS)
                                + normalTS.z * N);
                #endif

                // IMPORTANT: shadow coords + GetMainLight(shadowCoord), not GetMainLight().
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half mainVisibility = lerp(1.0h, mainLight.shadowAttenuation, _ReceiveShadows);
                half NdotL = saturate(dot(N, mainLight.direction));
                half mainAmount = NdotL * mainVisibility;

                // Fully shaded pixels receive the dark color band rather than full light.
                half3 mainDiffuse = ToonBands(mainAmount) * mainLight.color
                                    * mainLight.distanceAttenuation * _LightStrength;

                half3 ambient = _AmbientColor.rgb * _AmbientStrength
                              + max(SampleSH(N), 0.0h) * _EnvironmentStrength;
                half3 diffuse = mainDiffuse;
                mainLight.shadowAttenuation = mainVisibility;
                half3 specular = SpecularToon(N, V, mainLight);

                // Required for additional lights in Forward / Forward+.
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                #if defined(_ADDITIONAL_LIGHTS)
                    // Extra directional lights in Forward+ are NOT returned by GetAdditionalLightsCount().
                    #if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
                        UNITY_LOOP for (uint lightIndex = 0;
                                        lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
                                        lightIndex++)
                        {
                            Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
                            half visibility = lerp(1.0h, l.shadowAttenuation, _ReceiveShadows);
                            l.shadowAttenuation = visibility;
                            half amount = saturate(dot(N, l.direction)) * visibility;
                            half gate = smoothstep(_ShadowThreshold - _BandSmoothness,
                                                   _ShadowThreshold + _BandSmoothness, amount);
                            diffuse += ToonBands(amount) * gate * l.color
                                       * l.distanceAttenuation * _LightStrength;
                            specular += SpecularToon(N, V, l);
                        }
                    #endif

                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
                        half visibility = lerp(1.0h, l.shadowAttenuation, _ReceiveShadows);
                        l.shadowAttenuation = visibility;
                        half amount = saturate(dot(N, l.direction)) * visibility;
                        half gate = smoothstep(_ShadowThreshold - _BandSmoothness,
                                               _ShadowThreshold + _BandSmoothness, amount);
                        diffuse += ToonBands(amount) * gate * l.color
                                   * l.distanceAttenuation * _LightStrength;
                        specular += SpecularToon(N, V, l);
                    LIGHT_LOOP_END
                #endif

                half3 rgb = albedo * (ambient + diffuse) + specular;

                #if defined(_EMISSION)
                    rgb += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb
                           * _EmissionColor.rgb * _EmissionStrength;
                #endif

                rgb = MixFog(rgb, i.fogFactor);
                return half4(rgb, texel.a * _BaseColor.a);
            }
            ENDHLSL
        }

        // Writes the REAL object silhouette into URP's shadow maps.
        // Does not extrude geometry; no outline can cast a fake shadow.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVert(ShadowAttributes v)
            {
                ShadowVaryings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirWS = _LightDirection;
                #endif

                float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirWS));
                #if UNITY_REVERSED_Z
                    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = clipPos;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                ApplyAlphaClip(i.uv);
                return 0;
            }
            ENDHLSL
        }

        // Depth-only pass for depth priming, camera depth and cutout geometry.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            DepthVaryings DepthVert(DepthAttributes v)
            {
                DepthVaryings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half4 DepthFrag(DepthVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                ApplyAlphaClip(i.uv);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
