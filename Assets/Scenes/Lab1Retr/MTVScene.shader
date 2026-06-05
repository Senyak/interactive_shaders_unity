Shader "Custom/MagicTVScene"
{
    Properties
    {
        _MainTex ("Feedback Texture", 2D) = "white" {}

        _Gamma ("Gamma", Range(0.1, 0.8)) = 0.8
        _HueShift ("Hue Shift", Range(-0.5, 0.5)) = 0.0
        _Saturation ("Saturation", Range(1.3, 3.0)) = 1.0

        _Contrast ("Contrast", Range(0.9, 1.3)) = 1.0
        _Brightness ("Brightness", Range(0.0, 0.5)) = 0.0

        _ColorDriftX ("ColorDriftX", Range(-0.1, 0.1)) = 0.0
        _ColorDriftY ("ColorDriftY", Range(-0.1, 0.1)) = 0.0

        _ChromaNR ("Chroma NR", Range(0.0, 1.0)) = 0.3

        [Toggle] _Reverse ("Reverse (Nega/Posi)", Float) = 0

        _CRTWarp ("CRT Warp", Range(-0.2, 0.2)) = 0.05
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.3
        _ScanlineScale ("Scanline Scale", Float) = 300.0
        _Vignette ("Vignette", Range(0, 1)) = 0.5
        _NoiseAmount ("Noise Amount", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "MagicTVPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Gamma;
                float _HueShift;
                float _Saturation;
                float _Contrast;
                float _Brightness;
                float _ColorDriftX;
                float _ColorDriftY;
                float2 _ColorDrift;
                float _ChromaNR;
                float _Reverse;
                float _CRTWarp;
                float _ScanlineStrength;
                float _ScanlineScale;
                float _Vignette;
                float _NoiseAmount;
            CBUFFER_END

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

            float3 rgb2yuv(float3 rgb)
            {
                float3 yuv;
                yuv.x = dot(rgb, float3(0.299, 0.587, 0.114));
                yuv.y = dot(rgb, float3(-0.14713, -0.28886, 0.436));
                yuv.z = dot(rgb, float3(0.615, -0.51499, -0.10001));
                return yuv;
            }

            float3 yuv2rgb(float3 yuv)
            {
                float3 rgb;
                rgb.r = yuv.x + 1.13983 * yuv.z;
                rgb.g = yuv.x - 0.39465 * yuv.y - 0.58060 * yuv.z;
                rgb.b = yuv.x + 2.03211 * yuv.y;
                return rgb;
            }

            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float3 applyContrastBrightness(float3 color, float contrast, float brightness)
            {
                color = (color - 0.5) * contrast + 0.5;
                color += brightness;
                return saturate(color);
            }

            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 warpedUV = uv * 2.0 - 1.0;
                float warpFactor = 1.0 - _CRTWarp * dot(warpedUV, warpedUV);
                warpedUV *= warpFactor;
                warpedUV = warpedUV * 0.5 + 0.5;

                if (warpedUV.x < 0.0 || warpedUV.x > 1.0 || warpedUV.y < 0.0 || warpedUV.y > 1.0)
                    return half4(0, 0, 0, 1);

                float3 color;
                float4 colorDrift = float4(_ColorDriftX, _ColorDriftY, 0.0, 0.0);
                color.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, warpedUV + colorDrift).r;
                color.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, warpedUV).g;
                color.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, warpedUV - colorDrift).b;

                float3 yuv = rgb2yuv(color);
                float Y = yuv.x;
                float2 UV = yuv.yz;

                Y = pow(saturate(Y), _Gamma);

                
                UV = lerp(UV, float2(0.0, 0.0), _ChromaNR);

                float3 rgb = yuv2rgb(float3(Y, UV));
                float3 hsv = rgb2hsv(rgb);
                hsv.x = frac(hsv.x + _HueShift);
                hsv.y *= _Saturation;
                rgb = hsv2rgb(hsv);

                rgb = applyContrastBrightness(rgb, _Contrast, _Brightness);

                if (_Reverse > 0.5) rgb = 1.0 - rgb;

                float scanline = sin(warpedUV.y * _ScanlineScale) * 0.5 + 0.5;
                scanline = lerp(1.0, scanline, _ScanlineStrength);
                rgb *= scanline;

                float2 vignetteUV = warpedUV * (1.0 - warpedUV.yx);
                float vignette = vignetteUV.x * vignetteUV.y * 15.0;
                vignette = pow(vignette, _Vignette);
                rgb *= vignette;

                float noise = random(warpedUV + _Time.y) * 2.0 - 1.0;
                rgb += noise * _NoiseAmount;

                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }
}