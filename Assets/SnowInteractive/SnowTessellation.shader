Shader "Custom/SnowTessellation"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _DarkColor ("Dark Color (Trampled)", Color) = (0.2,0.2,0.2,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _SnowOffsetTex ("Snow Offset", 2D) = "white" {}
        _MaxSnowHeight ("Max Snow Height", Float) = 0.5
        _TessAmount ("Tessellation", Range(1,64)) = 8
        _ColorSmoothness ("Color Smoothness", Range(0.5, 5)) = 2.0 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct TessControlPoint
            {
                float4 positionOS : INTERNALTESSPOS;
                float2 uv : TEXCOORD0;
            };

            struct TessFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SnowOffsetTex);
            SAMPLER(sampler_SnowOffsetTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DarkColor;
                float4 _MainTex_ST;
                float _MaxSnowHeight;
                float _TessAmount;
                float _ColorSmoothness;          
            CBUFFER_END

            TessControlPoint vert(Attributes v)
            {
                TessControlPoint o;
                o.positionOS = v.positionOS;
                o.uv = v.uv;
                return o;
            }

            TessFactors PatchConstantFunc(InputPatch<TessControlPoint, 3> patch)
            {
                TessFactors f;
                f.edge[0] = _TessAmount;
                f.edge[1] = _TessAmount;
                f.edge[2] = _TessAmount;
                f.inside = _TessAmount;
                return f;
            }

            [domain("tri")]
            [outputcontrolpoints(3)]
            [outputtopology("triangle_cw")]
            [partitioning("integer")]
            [patchconstantfunc("PatchConstantFunc")]
            TessControlPoint hull(InputPatch<TessControlPoint, 3> patch, uint id : SV_OutputControlPointID)
            {
                return patch[id];
            }

            [domain("tri")]
            Varyings domain(TessFactors factors, OutputPatch<TessControlPoint, 3> patch, float3 bary : SV_DomainLocation)
            {
                Attributes v;
                v.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
                v.uv = patch[0].uv * bary.x + patch[1].uv * bary.y + patch[2].uv * bary.z;

                float height = SAMPLE_TEXTURE2D_LOD(_SnowOffsetTex, sampler_SnowOffsetTex, v.uv, 0).r;
                float4 positionWS = mul(GetObjectToWorldMatrix(), v.positionOS);
                positionWS.y += lerp(0, _MaxSnowHeight, height);
                Varyings o;
                o.positionCS = TransformWorldToHClip(positionWS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half height = SAMPLE_TEXTURE2D(_SnowOffsetTex, sampler_SnowOffsetTex, i.uv).r;
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                float t = saturate(height);
                t = pow(t, _ColorSmoothness);
                
                half4 color = lerp(_DarkColor, albedo * _BaseColor, t);
                return color;
            }
            ENDHLSL
        }
    }
}