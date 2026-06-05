Shader "Hidden/Dilate"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float2 _TexelSize;
            int _Radius;

            Varyings vert(Attributes v) {
                Varyings o;
                o.pos = TransformObjectToHClip(v.pos.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (col.a > 0) return col;
                half4 best = 0;
                [loop]   
                for (int x = -_Radius; x <= _Radius; x++) {
                    [loop]
                    for (int y = -_Radius; y <= _Radius; y++) {
                        half4 samp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(x, y) * _TexelSize);
                        if (samp.a > best.a) best = samp;
                    }
                }
                return best;
            }
            ENDHLSL
        }
    }
}