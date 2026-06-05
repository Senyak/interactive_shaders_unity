Shader "Hidden/ApplyStroke"
{
    Properties
    {
        _MainTex ("Paint Texture", 2D) = "white" {}
        _StrokeTex ("Stroke Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_StrokeTex);
            SAMPLER(sampler_StrokeTex);
            int _IsErasing;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 paint = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 stroke = SAMPLE_TEXTURE2D(_StrokeTex, sampler_StrokeTex, i.uv);
                if (_IsErasing > 0.5)
                {
                    half newAlpha = paint.a * (1.0 - stroke.a);
                    return half4(paint.rgb, newAlpha);
                }
                else
                {
                    half3 newColor = lerp(paint.rgb, stroke.rgb, stroke.a);
                    half newAlpha = min(paint.a + stroke.a, 1.0);
                    return half4(newColor, newAlpha);
                }
            }
            ENDHLSL
        }
    }
}