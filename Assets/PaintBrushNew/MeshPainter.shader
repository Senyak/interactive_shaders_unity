Shader "Custom/MeshPainter"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"
        _PaintTex("Paint Layer", 2D) = "white" {}
        _StrokeTex("Stroke Layer", 2D) = "white" {}
        _IgnoreBaseForPaint("Ignore Base for Paint", Float) = 0  
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PaintTex);
            SAMPLER(sampler_PaintTex);
            TEXTURE2D(_StrokeTex);
            SAMPLER(sampler_StrokeTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                int _IsErasing;
                float _IgnoreBaseForPaint;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                half4 paintColor = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, IN.uv);
                half4 strokeColor = SAMPLE_TEXTURE2D(_StrokeTex, sampler_StrokeTex, IN.uv);

                half3 finalPaint;
                if (_IgnoreBaseForPaint > 0.5)
                {
                    finalPaint = lerp(baseColor.rgb, paintColor.rgb, paintColor.a);
                }
                else
                {
                    finalPaint = lerp(baseColor.rgb, paintColor.rgb, paintColor.a);
                }

                half3 finalColor;
                if (_IsErasing > 0.5)
                {
                    finalColor = lerp(finalPaint, baseColor.rgb, strokeColor.a);
                }
                else
                {
                    finalColor = lerp(finalPaint, strokeColor.rgb, strokeColor.a);
                }
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}