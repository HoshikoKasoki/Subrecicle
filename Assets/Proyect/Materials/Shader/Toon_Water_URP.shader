Shader "Rii/Toon Water URP (Foam Diagnostic)"
{
    Properties
    {
        [Header(WaterColors)]
        [MainColor] _ShallowColor ("Shallow Water", Color) = (0.13, 0.68, 0.87, 1)
        _DeepColor ("Deep Water", Color) = (0.02, 0.20, 0.48, 1)
        _PatternColor ("Surface Pattern", Color) = (0.43, 0.86, 0.96, 1)
        _FoamColor ("Foam Color", Color) = (0.88, 0.98, 1, 1)
        _DepthColorDistance ("Depth Color Distance", Range(0.1, 30)) = 3.5

        [Header(Waves)]
        _WaveAmplitude ("Wave Height", Range(0, 2)) = 0.18
        _WaveFrequency ("Wave Frequency", Range(0.05, 5)) = 1.1
        _WaveSpeed ("Wave Speed", Range(0, 6)) = 1.15
        _WaveDetail ("Secondary Wave Strength", Range(0, 1)) = 0.25

        [Header(AnimatedSurface)]
        _PatternScale ("Pattern Scale per World Unit", Range(0.1, 5)) = 0.85
        _PatternSpeed ("Pattern Movement", Range(0, 3)) = 0.22
        _PatternStrength ("Pattern Opacity", Range(0, 1)) = 0.55
        _PatternWidth ("Pattern Line Width", Range(0.005, 0.15)) = 0.042

        [Header(ShoreFoam)]
        [Toggle(_DEPTH_FOAM_ON)] _UseDepthFoam ("Use Depth and Shore Foam", Float) = 1
        _ShoreWidth ("Shore Foam Width", Range(0.01, 8)) = 1.6
        _ShoreNoiseScale ("Shore Foam Noise Scale", Range(0.1, 12)) = 2.8
        _ShoreFoamStrength ("Shore Foam Strength", Range(0, 1)) = 1
        _CrestFoamStrength ("Wave Crest Foam", Range(0, 1)) = 0.15
        [Enum(Off,0,DepthNearSurface,1,IntersectionFoam,2,DepthTextureValid,3,MaterialCheckMagenta,4,RawDepth,5)] _FoamDebug ("Debug Foam", Float) = 0

        [Header(Transparency)]
        _ShallowOpacity ("Shallow Opacity", Range(0, 1)) = 0.55
        _DeepOpacity ("Deep Opacity", Range(0, 1)) = 0.93

        [Header(ToonLighting)]
        _DarkBandColor ("Dark Band Tint", Color) = (0.27, 0.45, 0.63, 1)
        _MidBandColor ("Middle Band Tint", Color) = (0.70, 0.84, 0.93, 1)
        _BandThreshold1 ("Dark to Middle", Range(0, 1)) = 0.25
        _BandThreshold2 ("Middle to Light", Range(0, 1)) = 0.62
        _BandSoftness ("Band Softness", Range(0.001, 0.2)) = 0.035
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 0.27
        _DirectStrength ("Directional Light Strength", Range(0, 3)) = 1
        _AdditionalStrength ("Point and Spot Light Strength", Range(0, 3)) = 1
        [Toggle] _ReceiveShadows ("Receive Shadows", Float) = 1
        _SpecularStrength ("Toon Specular Strength", Range(0, 2)) = 0.32
        _SpecularPower ("Specular Sharpness", Range(8, 256)) = 75
        _FresnelStrength ("Edge Glow Strength", Range(0, 1)) = 0.25
        _FresnelPower ("Edge Glow Power", Range(0.5, 10)) = 4

        [Header(ShadowCasting)]
        [Toggle] _CastWaterShadows ("Cast Shadows (Opaque Shadow)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // Shared by Forward and ShadowCaster so the moving shadows match the moving waves.
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _ShallowColor;
            float4 _DeepColor;
            float4 _PatternColor;
            float4 _FoamColor;
            float4 _DarkBandColor;
            float4 _MidBandColor;
            float _DepthColorDistance;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _WaveDetail;
            float _PatternScale;
            float _PatternSpeed;
            float _PatternStrength;
            float _PatternWidth;
            float _UseDepthFoam;
            float _ShoreWidth;
            float _ShoreNoiseScale;
            float _ShoreFoamStrength;
            float _CrestFoamStrength;
            float _FoamDebug;
            float _ShallowOpacity;
            float _DeepOpacity;
            float _BandThreshold1;
            float _BandThreshold2;
            float _BandSoftness;
            float _AmbientStrength;
            float _DirectStrength;
            float _AdditionalStrength;
            float _ReceiveShadows;
            float _SpecularStrength;
            float _SpecularPower;
            float _FresnelStrength;
            float _FresnelPower;
            float _CastWaterShadows;
        CBUFFER_END

        float WaterHeight(float2 worldXZ)
        {
            float t = _Time.y * _WaveSpeed;
            float x = worldXZ.x * _WaveFrequency;
            float z = worldXZ.y * _WaveFrequency;
            float primary = (sin(x + t) + cos(z + t * 0.83)) * 0.5;
            float secondary = sin(x * 1.7 + z * 1.2 - t * 1.31) * _WaveDetail;
            return _WaveAmplitude * (primary + secondary);
        }

        float3 WaterNormal(float2 worldXZ)
        {
            float t = _Time.y * _WaveSpeed;
            float x = worldXZ.x * _WaveFrequency;
            float z = worldXZ.y * _WaveFrequency;
            float common = _WaveAmplitude * _WaveFrequency;
            float phase2 = x * 1.7 + z * 1.2 - t * 1.31;
            float dx = common * (0.5 * cos(x + t) + 1.7 * _WaveDetail * cos(phase2));
            float dz = common * (-0.5 * sin(z + t * 0.83) + 1.2 * _WaveDetail * cos(phase2));
            return normalize(float3(-dx, 1.0, -dz));
        }

        float3 DisplaceWorld(float3 positionWS)
        {
            positionWS.y += WaterHeight(positionWS.xz);
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ToonWaterForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _DEPTH_FOAM_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = DisplaceWorld(TransformObjectToWorld(input.positionOS.xyz));
                output.positionWS = positionWS;
                output.normalWS = WaterNormal(positionWS.xz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                return float2(Hash21(p), Hash21(p + 17.81));
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 w = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), w.x),
                            lerp(Hash21(i + float2(0, 1)), Hash21(i + 1.0), w.x), w.y);
            }

            // Procedural, animated, irregular broken circles inspired by cel-shaded water.
            float WaterPattern(float2 worldXZ)
            {
                float t = _Time.y * _PatternSpeed;
                float2 p = worldXZ * _PatternScale + float2(t * 0.57, -t * 0.37);
                float2 cell = floor(p);
                float result = 0.0;

                [unroll] for (int y = -1; y <= 1; y++)
                {
                    [unroll] for (int x = -1; x <= 1; x++)
                    {
                        float2 id = cell + float2(x, y);
                        float2 rnd = Hash22(id);
                        float2 center = id + 0.5 + (rnd - 0.5) * 0.5;
                        float2 delta = p - center;
                        float angle = atan2(delta.y, delta.x);
                        float distanceToCenter = length(delta);
                        float wobble = 0.035 * sin(angle * 5.0 + t + rnd.x * 6.28318);
                        float radius = 0.23 + rnd.y * 0.15;
                        float ring = 1.0 - smoothstep(_PatternWidth,
                                                       _PatternWidth + 0.035,
                                                       abs(distanceToCenter + wobble - radius));
                        // Break up rings so they look like foam streaks instead of bubbles.
                        float arc = smoothstep(-0.25, 0.25,
                                               sin(angle * 2.4 + rnd.x * 6.28 + t * 0.7));
                        result = max(result, ring * arc);
                    }
                }

                float2 p2 = worldXZ * (_PatternScale * 0.7) + float2(-t * 0.24, t * 0.33);
                float swirls = sin(p2.x * 3.5 + sin(p2.y * 2.5 + t)) *
                               cos(p2.y * 2.8 - t * 0.9);
                float streaks = smoothstep(0.65, 0.88, swirls);
                return saturate(max(result, streaks * 0.35));
            }

            float3 ToonLighting(float ndotl, float shadow)
            {
                float amount = saturate(ndotl * shadow);
                float band1 = smoothstep(_BandThreshold1 - _BandSoftness,
                                         _BandThreshold1 + _BandSoftness, amount);
                float band2 = smoothstep(_BandThreshold2 - _BandSoftness,
                                         _BandThreshold2 + _BandSoftness, amount);
                float3 tint = lerp(_DarkBandColor.rgb, _MidBandColor.rgb, band1);
                return lerp(tint, float3(1, 1, 1), band2);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 n = normalize(input.normalWS);
                float3 viewDir = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float mainShadow = lerp(1.0, mainLight.shadowAttenuation, saturate(_ReceiveShadows));
                float ndotl = saturate(dot(n, mainLight.direction));

                // The depth texture stores opaque geometry. We sample it even when
                // the foam toggle is disabled so that diagnostic modes always work.
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth = SampleSceneDepth(screenUV);
                #if UNITY_REVERSED_Z
                    float validDepth = step(0.00001, rawDepth);
                #else
                    float validDepth = 1.0 - step(0.99999, rawDepth);
                #endif

                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                // SV_POSITION.z in the fragment stage is the depth-buffer value.
                float waterEyeDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDifference = 10000.0;
                if (validDepth > 0.5)
                    depthDifference = max(0.0, sceneEyeDepth - waterEyeDepth);

                float deepFactor = saturate(depthDifference / max(0.01, _DepthColorDistance));
                float3 baseColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, deepFactor);
                float pattern = WaterPattern(input.positionWS.xz);
                baseColor = lerp(baseColor, _PatternColor.rgb,
                                 pattern * saturate(_PatternStrength));

                // Main Directional Light, with proper URP realtime shadow attenuation.
                float3 lighting = float3(_AmbientStrength, _AmbientStrength, _AmbientStrength) +
                    ToonLighting(ndotl, mainShadow) * mainLight.color *
                    (mainLight.distanceAttenuation * _DirectStrength);

                // Extra point/spot lights in the Forward renderer.
                #if defined(_ADDITIONAL_LIGHTS)
                    uint count = GetAdditionalLightsCount();
                    for (uint i = 0u; i < count; i++)
                    {
                        Light li = GetAdditionalLight(i, input.positionWS);
                        float lightDot = saturate(dot(n, li.direction));
                        float lightShadow = lerp(1.0, li.shadowAttenuation,
                                                 saturate(_ReceiveShadows));
                        lighting += ToonLighting(lightDot, lightShadow) * li.color *
                                    li.distanceAttenuation * lightDot * lightShadow *
                                    _AdditionalStrength;
                    }
                #endif

                float3 color = baseColor * lighting;
                float3 halfDir = SafeNormalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(n, halfDir)), _SpecularPower);
                spec = smoothstep(0.40, 0.65, spec);
                color += spec * _SpecularStrength * mainShadow * mainLight.color;

                float fresnel = pow(1.0 - saturate(dot(n, viewDir)),
                                    max(_FresnelPower, 0.01));
                color = lerp(color, _PatternColor.rgb,
                             saturate(fresnel * _FresnelStrength));

                // Visible, smoothly feathered intersection band. Animated noise breaks
                // the line without removing the contact foam completely.
                float shoreFoam = 0.0;
                #if defined(_DEPTH_FOAM_ON)
                    float width = max(_ShoreWidth, 0.001);
                    float noise = ValueNoise(input.positionWS.xz * _ShoreNoiseScale +
                                             _Time.y * float2(0.26, -0.18));
                    float noisyDepth = max(0.0, depthDifference + (noise - 0.5) * width * 0.25);
                    float contactBand = 1.0 - smoothstep(0.0, width, noisyDepth);
                    shoreFoam = saturate(pow(saturate(contactBand), 0.55) *
                                         (0.85 + 0.3 * noise) * _ShoreFoamStrength);
                    shoreFoam *= validDepth;
                #endif

                // Debug 4: forced magenta. If this does not change the water,
                // the visible object is not using this shader/material.
                if (_FoamDebug > 3.5 && _FoamDebug < 4.5)
                    return half4(1, 0, 1, 1);
                // Debug 5: grayscale of the raw sampled scene depth.
                if (_FoamDebug > 4.5)
                    return half4(rawDepth.xxx, 1);
                // Debug 3: opaque depth exists = green; no depth = red.
                if (_FoamDebug > 2.5)
                    return half4(validDepth > 0.5 ? half3(0, 1, 0) : half3(1, 0, 0), 1);
                if (_FoamDebug > 1.5)
                    return half4(shoreFoam.xxx, 1);
                if (_FoamDebug > 0.5)
                {
                    float nearSurface = validDepth * (1.0 - saturate(depthDifference / max(_ShoreWidth, 0.001)));
                    return half4(nearSurface.xxx, 1);
                }

                float normalizedHeight = WaterHeight(input.positionWS.xz) /
                                         max(_WaveAmplitude, 0.001);
                float crestNoise = ValueNoise(input.positionWS.xz * 3.0 +
                                              float2(_Time.y * 0.3, 0));
                float crestFoam = smoothstep(0.68, 1.05, normalizedHeight) *
                                  smoothstep(0.35, 0.72, crestNoise) *
                                  _CrestFoamStrength;
                float foam = saturate(max(shoreFoam, crestFoam));
                color = lerp(color, _FoamColor.rgb, foam);

                float opacity = lerp(_ShallowOpacity, _DeepOpacity, deepFactor);
                opacity = lerp(opacity, 1.0, foam);
                color = MixFog(color, input.fogFactor);
                return half4(color, saturate(opacity));
            }
            ENDHLSL
        }

        // Optional, opaque shadow cast by the displaced water surface.
        // Shadow maps do not support physically accurate translucent colored shadows here.
        Pass
        {
            Name "WaterShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 ws = DisplaceWorld(TransformObjectToWorld(input.positionOS.xyz));
                float3 n = WaterNormal(ws.xz);
                float3 lightDir = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    lightDir = normalize(_LightPosition - ws);
                #endif
                float4 clipPos = TransformWorldToHClip(ApplyShadowBias(ws, n, lightDir));
                #if UNITY_REVERSED_Z
                    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = clipPos;
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                clip(_CastWaterShadows - 0.5);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
