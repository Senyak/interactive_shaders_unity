Shader "Hidden/PaintBrushWorld"
{
    Properties
    {
        _MainTex ("Current Stroke Texture", 2D) = "black" {}
        _BrushColor ("Brush Color", Color) = (1,1,1,1)
        _BrushOpacity ("Opacity", Range(0,1)) = 0.8
        _BrushRadius ("Radius (world)", Float) = 0.1
        _BrushCenterWorld ("Center (world)", Vector) = (0,0,0,0)
        _IsErasing ("Is Erasing", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
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
                float3 worldPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BrushColor;
                float _BrushOpacity;
                float _BrushRadius;
                float3 _BrushCenterWorld;
                float _IsErasing;
                float4x4 _ObjectToWorld;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 worldPos = mul(_ObjectToWorld, v.positionOS);
                o.worldPos = worldPos.xyz;

                float2 uvRemapped = v.uv;
                uvRemapped.y = 1.0 - uvRemapped.y;
                uvRemapped = uvRemapped * 2.0 - 1.0;
                o.positionCS = float4(uvRemapped, 0.0, 1.0);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 current = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float dist = distance(_BrushCenterWorld, i.worldPos);
                float influence = 1.0 - smoothstep(_BrushRadius * 0.5, _BrushRadius, dist);
                influence = saturate(influence * _BrushOpacity);

                half4 output = current;

                if (_IsErasing > 0.5)
                {
                    float newAlpha = min(current.a + influence, 1.0);
                    output = half4(0, 0, 0, newAlpha);
                }
                else
                {
                    half3 newColor = lerp(current.rgb, _BrushColor.rgb, influence * _BrushColor.a);
                    float newAlpha = max(current.a, influence * _BrushColor.a);
                    output = half4(newColor, newAlpha);
                }
                return output;
            }
            ENDHLSL
        }
    }
}