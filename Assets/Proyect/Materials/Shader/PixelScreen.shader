Shader "Custom/Pixel Screen UI"
{
    Properties
    {
        _MainTex ("Render Texture", 2D) = "white" {}

        _DitherStrength ("Dither Strength", Range(0, 1)) = 0

        _Contrast ("Contrast", Range(0.5, 2)) = 1

        _Brightness ("Brightness", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off
        ZTest Always

        Cull Off

        Pass
        {
            Name "PixelScreen"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            // ========================================================
            // TEXTURA
            // ========================================================

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            // ========================================================
            // MATERIAL
            // ========================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                float _DitherStrength;

                float _Contrast;

                float _Brightness;

            CBUFFER_END


            // ========================================================
            // STRUCTURES
            // ========================================================

            struct Attributes
            {
                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;

                float4 color : COLOR;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;

                float4 color : COLOR;
            };


            // ========================================================
            // VERTEX
            // ========================================================

            Varyings Vert(
                Attributes input
            )
            {
                Varyings output;


                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );


                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _MainTex
                    );


                output.color =
                    input.color;


                return output;
            }


            // ========================================================
            // BAYER 4x4
            //
            // Sin arrays.
            // Sin cosas raras.
            // Solo condiciones, porque el compilador quiere sufrir
            // pero aparentemente no demasiado.
            // ========================================================

            float Bayer4x4(float2 position)
            {
                int x =
                    (int)fmod(
                        floor(position.x),
                        4.0
                    );


                int y =
                    (int)fmod(
                        floor(position.y),
                        4.0
                    );


                int index =
                    y * 4 + x;


                if (index == 0)  return 0.0;
                if (index == 1)  return 8.0;
                if (index == 2)  return 2.0;
                if (index == 3)  return 10.0;

                if (index == 4)  return 12.0;
                if (index == 5)  return 4.0;
                if (index == 6)  return 14.0;
                if (index == 7)  return 6.0;

                if (index == 8)  return 3.0;
                if (index == 9)  return 11.0;
                if (index == 10) return 1.0;
                if (index == 11) return 9.0;

                if (index == 12) return 15.0;
                if (index == 13) return 7.0;
                if (index == 14) return 13.0;

                return 5.0;
            }


            // ========================================================
            // FRAGMENT
            // ========================================================

            half4 Frag(
                Varyings input
            ) : SV_Target
            {
                // ----------------------------------------------------
                // TEXTURA
                //
                // La RenderTexture debe tener:
                //
                // Filter Mode = Point
                //
                // De esta forma SAMPLE_TEXTURE2D utiliza nearest.
                // ----------------------------------------------------

                float4 color =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );


                // ----------------------------------------------------
                // BRILLO
                // ----------------------------------------------------

                color.rgb *=
                    _Brightness;


                // ----------------------------------------------------
                // CONTRASTE
                // ----------------------------------------------------

                color.rgb =
                    (
                        color.rgb -
                        0.5
                    )
                    *
                    _Contrast
                    +
                    0.5;


                // ----------------------------------------------------
                // DITHER
                // ----------------------------------------------------

                float2 screenPosition =
                    input.positionCS.xy;


                float bayer =
                    Bayer4x4(
                        screenPosition
                    );


                float threshold =
                    bayer / 16.0;


                float dither =
                    (
                        threshold -
                        0.5
                    )
                    *
                    _DitherStrength;


                color.rgb +=
                    dither *
                    0.05;


                // ----------------------------------------------------
                // COLOR DEL RAW IMAGE
                // ----------------------------------------------------

                color *=
                    input.color;


                return color;
            }

            ENDHLSL
        }
    }

    FallBack Off
}