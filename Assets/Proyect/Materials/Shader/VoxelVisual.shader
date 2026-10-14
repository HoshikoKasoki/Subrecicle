Shader "Custom/Toon Professional"
{
    Properties
    {
        // ============================================================
        // BASE
        // ============================================================

        [MainTexture]
        _BaseMap ("Base Map", 2D) = "white" {}

        [MainColor]
        _BaseColor ("Base Color", Color) = (1,1,1,1)


        // ============================================================
        // NORMAL MAP
        // ============================================================

        [Toggle(_TOON_NORMALMAP)]
        _UseNormalMap ("Use Normal Map", Float) = 0

        _NormalMap ("Normal Map", 2D) = "bump" {}

        _NormalScale ("Normal Strength", Range(0,2)) = 1


        // ============================================================
        // TOON SHADING
        // ============================================================

        _ShadowColor1 ("Shadow Color 1", Color) = (0.65,0.65,0.7,1)

        _ShadowColor2 ("Shadow Color 2", Color) = (0.35,0.35,0.42,1)

        _ShadowThreshold1 ("Shadow Threshold 1", Range(0,1)) = 0.35

        _ShadowThreshold2 ("Shadow Threshold 2", Range(0,1)) = 0.70

        _ShadowFeather ("Shadow Feather", Range(0,0.25)) = 0.02

        _ShadowOffset ("Shadow Offset", Range(-0.5,0.5)) = 0


        // ============================================================
        // AMBIENT / ENVIRONMENT
        // ============================================================

        _AmbientColor ("Ambient Color", Color) = (0.18,0.18,0.22,1)

        _AmbientStrength ("Ambient Strength", Range(0,2)) = 0.35

        _EnvironmentStrength ("Environment Strength", Range(0,2)) = 0.5


        // ============================================================
        // SPECULAR
        // ============================================================

        [HDR]
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)

        _SpecularStrength ("Specular Strength", Range(0,2)) = 0.8

        _SpecularPower ("Specular Power", Range(1,256)) = 64

        _SpecularThreshold ("Specular Threshold", Range(0,1)) = 0.5

        _SpecularFeather ("Specular Feather", Range(0,0.25)) = 0.02


        // ============================================================
        // RIM LIGHT
        // ============================================================

        [HDR]
        _RimColor ("Rim Color", Color) = (0.35,0.65,1,1)

        _RimStrength ("Rim Strength", Range(0,2)) = 0.5

        _RimPower ("Rim Power", Range(0.1,10)) = 4

        _RimThreshold ("Rim Threshold", Range(0,1)) = 0.65

        _RimFeather ("Rim Feather", Range(0,0.25)) = 0.03

        _RimLightInfluence ("Rim Light Influence", Range(0,1)) = 0.5


        // ============================================================
        // EMISSION
        // ============================================================

        [Toggle(_TOON_EMISSION)]
        _UseEmission ("Use Emission", Float) = 0

        _EmissionMap ("Emission Map", 2D) = "black" {}

        [HDR]
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)

        _EmissionStrength ("Emission Strength", Range(0,10)) = 1


        // ============================================================
        // OUTLINE
        // ============================================================

        _OutlineColor ("Outline Color", Color) = (0.01,0.01,0.015,1)

        _OutlineWidth ("Outline Width (Pixels)", Range(0,10)) = 2


        // ============================================================
        // ALPHA CLIPPING
        // ============================================================

        [Toggle(_ALPHATEST_ON)]
        _AlphaClip ("Alpha Clipping", Float) = 0

        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5


        // ============================================================
        // RENDERING
        // ============================================================

        _ReceiveShadows ("Receive Shadows", Range(0,1)) = 1
    }


    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        LOD 300


        // ============================================================
        // OUTLINE PASS
        // ============================================================

        Pass
        {
            Name "Outline"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            // --------------------------------------------------------
            // TEXTURE
            // --------------------------------------------------------

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


            // --------------------------------------------------------
            // MATERIAL
            // --------------------------------------------------------

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float4 _OutlineColor;

                float _OutlineWidth;

                float _Cutoff;

            CBUFFER_END


            struct OutlineAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };


            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            // --------------------------------------------------------
            // OUTLINE VERTEX
            //
            // Extrusión en clip-space.
            //
            // El ancho se controla en píxeles de pantalla.
            // --------------------------------------------------------

            OutlineVaryings OutlineVertex(
                OutlineAttributes input
            )
            {
                OutlineVaryings output;


                // Posición original.
                float4 positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );


                // Normal en World Space.
                float3 normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS
                    );


                // Normal transformada a clip-space.
                float3 normalCS =
                    mul(
                        (float3x3)UNITY_MATRIX_VP,
                        normalWS
                    );


                // Evitar problemas cuando XY sea casi cero.
                float2 direction =
                    normalize(
                        normalCS.xy +
                        float2(0.00001, 0.00001)
                    );


                // Extrusión compensada por perspectiva.
                positionCS.xy +=
                    direction /
                    _ScreenParams.xy *
                    positionCS.w *
                    (_OutlineWidth * 2.0);


                output.positionCS =
                    positionCS;


                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _BaseMap
                    );


                return output;
            }


            // --------------------------------------------------------
            // OUTLINE FRAGMENT
            // --------------------------------------------------------

            half4 OutlineFragment(
                OutlineVaryings input
            ) : SV_Target
            {

                #if defined(_ALPHATEST_ON)

                    float alpha =
                        SAMPLE_TEXTURE2D(
                            _BaseMap,
                            sampler_BaseMap,
                            input.uv
                        ).a *
                        _BaseColor.a;

                    clip(alpha - _Cutoff);

                #endif


                return _OutlineColor;
            }

            ENDHLSL
        }


        // ============================================================
        // MAIN TOON PASS
        // ============================================================

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual


            HLSLPROGRAM

            #pragma vertex ToonVertex
            #pragma fragment ToonFragment


            // --------------------------------------------------------
            // SHADER FEATURES
            // --------------------------------------------------------

            #pragma shader_feature_local_fragment _TOON_NORMALMAP
            #pragma shader_feature_local_fragment _TOON_EMISSION
            #pragma shader_feature_local_fragment _ALPHATEST_ON


            // --------------------------------------------------------
            // MAIN LIGHT
            // --------------------------------------------------------

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN


            // --------------------------------------------------------
            // ADDITIONAL LIGHTS
            // --------------------------------------------------------

            #pragma multi_compile _ _ADDITIONAL_LIGHTS


            // --------------------------------------------------------
            // ADDITIONAL LIGHT SHADOWS
            // --------------------------------------------------------

            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS


            // --------------------------------------------------------
            // SOFT SHADOWS
            // --------------------------------------------------------

            #pragma multi_compile_fragment _ _SHADOWS_SOFT


            // --------------------------------------------------------
            // FOG
            // --------------------------------------------------------

            #pragma multi_compile_fog


            // --------------------------------------------------------
            // INCLUDES
            // --------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            // ========================================================
            // TEXTURES
            // ========================================================

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);


            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);


            // ========================================================
            // MATERIAL CONSTANTS
            // ========================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;

                float4 _BaseColor;


                // Normal
                float _NormalScale;


                // Toon
                float4 _ShadowColor1;
                float4 _ShadowColor2;

                float _ShadowThreshold1;
                float _ShadowThreshold2;
                float _ShadowFeather;
                float _ShadowOffset;


                // Ambient
                float4 _AmbientColor;
                float _AmbientStrength;
                float _EnvironmentStrength;


                // Specular
                float4 _SpecularColor;
                float _SpecularStrength;
                float _SpecularPower;
                float _SpecularThreshold;
                float _SpecularFeather;


                // Rim
                float4 _RimColor;
                float _RimStrength;
                float _RimPower;
                float _RimThreshold;
                float _RimFeather;
                float _RimLightInfluence;


                // Emission
                float4 _EmissionColor;
                float _EmissionStrength;


                // Outline
                float4 _OutlineColor;
                float _OutlineWidth;


                // Alpha
                float _Cutoff;


                // Shadows
                float _ReceiveShadows;

            CBUFFER_END


            // ========================================================
            // STRUCTURES
            // ========================================================

            struct ToonAttributes
            {
                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                float4 tangentOS : TANGENT;

                float2 uv : TEXCOORD0;
            };


            struct ToonVaryings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float3 normalWS : TEXCOORD1;

                float3 tangentWS : TEXCOORD2;

                float3 bitangentWS : TEXCOORD3;

                float2 uv : TEXCOORD4;

                float fogFactor : TEXCOORD5;
            };


            // ========================================================
            // TOON BAND
            // ========================================================

            float ToonStep(
                float value,
                float threshold,
                float feather
            )
            {
                float lower =
                    threshold - feather;

                float upper =
                    threshold + feather;

                return smoothstep(
                    lower,
                    upper,
                    value
                );
            }


            // ========================================================
            // TOON DIFFUSE
            // ========================================================

            float3 EvaluateToonDiffuse(
                float NdotL,
                float attenuation
            )
            {
                // Shadow attenuation mueve la superficie
                // hacia las bandas oscuras.

                float lightValue =
                    saturate(
                        NdotL *
                        attenuation +
                        _ShadowOffset
                    );


                // Primera transición:
                // Shadow 2 -> Shadow 1.

                float band1 =
                    ToonStep(
                        lightValue,
                        _ShadowThreshold1,
                        _ShadowFeather
                    );


                // Segunda transición:
                // Shadow 1 -> Base.

                float band2 =
                    ToonStep(
                        lightValue,
                        _ShadowThreshold2,
                        _ShadowFeather
                    );


                float3 color =
                    lerp(
                        _ShadowColor2.rgb,
                        _ShadowColor1.rgb,
                        band1
                    );


                color =
                    lerp(
                        color,
                        float3(1,1,1),
                        band2
                    );


                return color;
            }


            // ========================================================
            // TOON SPECULAR
            // ========================================================

            float EvaluateToonSpecular(
                float3 normalWS,
                float3 viewWS,
                float3 lightDirWS,
                float lightVisibility
            )
            {
                float3 halfVector =
                    normalize(
                        viewWS +
                        lightDirWS
                    );


                float NdotH =
                    saturate(
                        dot(
                            normalWS,
                            halfVector
                        )
                    );


                float specular =
                    pow(
                        NdotH,
                        max(
                            _SpecularPower,
                            1.0
                        )
                    );


                specular =
                    ToonStep(
                        specular,
                        _SpecularThreshold,
                        _SpecularFeather
                    );


                specular *=
                    lightVisibility;


                return specular;
            }


            // ========================================================
            // NORMAL MAP
            // ========================================================

            float3 ApplyNormalMap(
                float2 uv,
                float3 normalWS,
                float3 tangentWS,
                float3 bitangentWS
            )
            {
                float4 normalSample =
                    SAMPLE_TEXTURE2D(
                        _NormalMap,
                        sampler_NormalMap,
                        uv
                    );


                float3 normalTS =
                    normalSample.xyz *
                    2.0 -
                    1.0;


                normalTS.xy *=
                    _NormalScale;


                normalTS =
                    normalize(
                        normalTS
                    );


                float3 mappedNormal =
                    normalize(
                        tangentWS *
                            normalTS.x +

                        bitangentWS *
                            normalTS.y +

                        normalWS *
                            normalTS.z
                    );


                return mappedNormal;
            }


            // ========================================================
            // VERTEX
            // ========================================================

            ToonVaryings ToonVertex(
                ToonAttributes input
            )
            {
                ToonVaryings output;


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS,
                        input.tangentOS
                    );


                output.positionCS =
                    positionInputs.positionCS;


                output.positionWS =
                    positionInputs.positionWS;


                output.normalWS =
                    normalize(
                        normalInputs.normalWS
                    );


                output.tangentWS =
                    normalize(
                        normalInputs.tangentWS
                    );


                output.bitangentWS =
                    normalize(
                        normalInputs.bitangentWS
                    );


                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _BaseMap
                    );


                output.fogFactor =
                    ComputeFogFactor(
                        positionInputs.positionCS.z
                    );


                return output;
            }


            // ========================================================
            // FRAGMENT
            // ========================================================

            half4 ToonFragment(
                ToonVaryings input
            ) : SV_Target
            {
                // ----------------------------------------------------
                // NORMAL
                // ----------------------------------------------------

                float3 normalWS =
                    normalize(
                        input.normalWS
                    );


                #if defined(_TOON_NORMALMAP)

                    normalWS =
                        ApplyNormalMap(
                            input.uv,
                            normalWS,
                            normalize(input.tangentWS),
                            normalize(input.bitangentWS)
                        );

                #endif


                // ----------------------------------------------------
                // VIEW
                // ----------------------------------------------------

                float3 viewWS =
                    normalize(
                        GetWorldSpaceViewDir(
                            input.positionWS
                        )
                    );


                // ----------------------------------------------------
                // BASE COLOR
                // ----------------------------------------------------

                float4 baseSample =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );


                float3 baseColor =
                    baseSample.rgb *
                    _BaseColor.rgb;


                float alpha =
                    baseSample.a *
                    _BaseColor.a;


                #if defined(_ALPHATEST_ON)

                    clip(
                        alpha -
                        _Cutoff
                    );

                #endif


                // ----------------------------------------------------
                // MAIN LIGHT
                // ----------------------------------------------------

                Light mainLight;


                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)

                    float4 shadowCoord =
                        TransformWorldToShadowCoord(
                            input.positionWS
                        );


                    mainLight =
                        GetMainLight(
                            shadowCoord
                        );

                #else

                    mainLight =
                        GetMainLight();

                #endif


                // ----------------------------------------------------
                // MAIN LIGHT DIFFUSE
                // ----------------------------------------------------

                float mainNdotL =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction
                        )
                    );


                float mainShadow =
                    lerp(
                        1.0,
                        mainLight.shadowAttenuation,
                        _ReceiveShadows
                    );


                float3 mainToon =
                    EvaluateToonDiffuse(
                        mainNdotL,
                        mainShadow
                    );


                float3 directLighting =
                    mainToon *
                    mainLight.color *
                    mainLight.distanceAttenuation;


                // ----------------------------------------------------
                // MAIN LIGHT SPECULAR
                // ----------------------------------------------------

                float mainSpec =
                    EvaluateToonSpecular(
                        normalWS,
                        viewWS,
                        mainLight.direction,
                        mainShadow
                    );


                float3 specular =
                    mainSpec *
                    _SpecularColor.rgb *
                    _SpecularStrength *
                    mainLight.color;


                // ====================================================
                // ADDITIONAL LIGHTS
                // ====================================================

                #if defined(_ADDITIONAL_LIGHTS)

                    uint lightCount =
                        GetAdditionalLightsCount();


                    for (
                        uint i = 0;
                        i < lightCount;
                        i++
                    )
                    {
                        Light light =
                            GetAdditionalLight(
                                i,
                                input.positionWS
                            );


                        float NdotL =
                            saturate(
                                dot(
                                    normalWS,
                                    light.direction
                                )
                            );


                        float shadow =
                            lerp(
                                1.0,
                                light.shadowAttenuation,
                                _ReceiveShadows
                            );


                        float attenuation =
                            light.distanceAttenuation *
                            shadow;


                        float3 toon =
                            EvaluateToonDiffuse(
                                NdotL,
                                attenuation
                            );


                        directLighting +=
                            toon *
                            light.color *
                            light.distanceAttenuation;


                        float additionalSpec =
                            EvaluateToonSpecular(
                                normalWS,
                                viewWS,
                                light.direction,
                                attenuation
                            );


                        specular +=
                            additionalSpec *
                            _SpecularColor.rgb *
                            _SpecularStrength *
                            light.color;
                    }

                #endif


                // ====================================================
                // AMBIENT
                // ====================================================

                float3 environmentLight =
                    SampleSH(
                        normalWS
                    );


                float3 ambient =
                    _AmbientColor.rgb *
                    _AmbientStrength;


                ambient +=
                    environmentLight *
                    _EnvironmentStrength;


                // ====================================================
                // RIM LIGHT
                // ====================================================

                float NdotV =
                    saturate(
                        dot(
                            normalWS,
                            viewWS
                        )
                    );


                float rim =
                    pow(
                        1.0 - NdotV,
                        max(
                            _RimPower,
                            0.001
                        )
                    );


                rim =
                    ToonStep(
                        rim,
                        _RimThreshold,
                        _RimFeather
                    );


                // Influencia del ángulo de la luz principal.

                float rimLightMask =
                    lerp(
                        1.0,
                        saturate(
                            dot(
                                normalWS,
                                mainLight.direction
                            ) * 0.5 +
                            0.5
                        ),
                        _RimLightInfluence
                    );


                rim *=
                    rimLightMask;


                float3 rimLighting =
                    rim *
                    _RimColor.rgb *
                    _RimStrength *
                    mainLight.color;


                // ====================================================
                // COMBINAR ILUMINACIÓN
                // ====================================================

                float3 finalColor =
                    baseColor *
                    (
                        ambient +
                        directLighting
                    );


                // ====================================================
                // SPECULAR
                // ====================================================

                finalColor +=
                    specular;


                // ====================================================
                // RIM
                // ====================================================

                finalColor +=
                    rimLighting;


                // ====================================================
                // EMISSION
                // ====================================================

                #if defined(_TOON_EMISSION)

                    float3 emission =
                        SAMPLE_TEXTURE2D(
                            _EmissionMap,
                            sampler_EmissionMap,
                            input.uv
                        ).rgb;


                    emission *=
                        _EmissionColor.rgb *
                        _EmissionStrength;


                    finalColor +=
                        emission;

                #endif


                // ====================================================
                // LIMITAR
                // ====================================================

                finalColor =
                    max(
                        finalColor,
                        0.0
                    );


                // ====================================================
                // FOG
                // ====================================================

                finalColor =
                    MixFog(
                        finalColor,
                        input.fogFactor
                    );


                return float4(
                    finalColor,
                    alpha
                );
            }

            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"

        // ============================================================
        // DEPTH ONLY
        // ============================================================

        Pass
        {
            Name "DepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM

            #pragma vertex DepthVertex
            #pragma fragment DepthFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _Cutoff;

            CBUFFER_END


            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };


            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            DepthVaryings DepthVertex(
                DepthAttributes input
            )
            {
                DepthVaryings output;


                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );


                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _BaseMap
                    );


                return output;
            }


            half4 DepthFragment(
                DepthVaryings input
            ) : SV_Target
            {

                #if defined(_ALPHATEST_ON)

                    float alpha =
                        SAMPLE_TEXTURE2D(
                            _BaseMap,
                            sampler_BaseMap,
                            input.uv
                        ).a *
                        _BaseColor.a;


                    clip(
                        alpha -
                        _Cutoff
                    );

                #endif


                return 0;
            }

            ENDHLSL
        }
    }


    FallBack Off
}