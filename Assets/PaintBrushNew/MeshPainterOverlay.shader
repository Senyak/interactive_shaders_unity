Shader "Custom/MeshPainterOverlay"
{
    Properties
    {
        _PaintTex("Paint Layer", 2D) = "white" {}
        _StrokeTex("Stroke Layer", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

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

            TEXTURE2D(_PaintTex);
            SAMPLER(sampler_PaintTex);
            TEXTURE2D(_StrokeTex);
            SAMPLER(sampler_StrokeTex);

            CBUFFER_START(UnityPerMaterial)
                int _IsErasing;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 paintColor = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, IN.uv);
                half4 strokeColor = SAMPLE_TEXTURE2D(_StrokeTex, sampler_StrokeTex, IN.uv);

                half3 finalColor = 0;
                half alpha = 0;

                if (_IsErasing > 0.5)
                {
                    alpha = paintColor.a * (1.0 - strokeColor.a);
                    finalColor = paintColor.rgb;
                }
                else
                {
                    half3 withPaint = lerp(paintColor.rgb, strokeColor.rgb, strokeColor.a);
                    alpha = min(paintColor.a + strokeColor.a, 1.0);
                    finalColor = withPaint;
                }

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}