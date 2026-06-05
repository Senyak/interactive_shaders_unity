Shader "Custom/InfiniteMirrorLayers"
{
    Properties
    {
        _MainTex ("Основная текстура", 2D) = "white" {}
        _GlassColor ("Цвет стекла", Color) = (0.8, 0.9, 1.0, 0.6)
        _GlassVisibility ("Прозрачность стекла", Range(0,1)) = 0.6
        _Smoothness ("Smoothness", Range(0,1)) = 0.8
        _Metallic ("Metallic", Range(0,1)) = 0.9
        _ReflectionStrength ("Сила отражений", Range(0,3)) = 1.0
        
        _LayerTex ("Текстура слоя", 2D) = "white" {}
        _GlowColor ("Цвет свечения слоёв", Color) = (0.2, 0.6, 1, 1)
        _LayerStartColor ("Цвет первого слоя", Color) = (0.0, 0.2, 0.6, 1)
        _LayerEndColor ("Цвет последнего слоя", Color) = (0.6, 0.0, 1.0, 1)
        _LayerCount ("Количество слоёв", Range(1, 40)) = 8
        _LayerStep ("Шаг смещения слоя", Range(-0.5, 0.5)) = 0.1
        _FresnelWidth ("Ширина границы", Range(0, 0.5)) = 0.2
        _TextureScale ("Масштаб текстуры слоя", Float) = 4.0
        _Intensity ("Яркость слоёв", Range(0, 2)) = 1.0
        _DepthFalloff ("Ослабление глубины", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Back
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float3 localNormal : TEXCOORD4;
                float3 localPos : TEXCOORD5;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_LayerTex);
            SAMPLER(sampler_LayerTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _GlassColor;
                float _GlassVisibility;
                float _Smoothness;
                float _Metallic;
                float _ReflectionStrength;
                float4 _GlowColor;
                float4 _LayerStartColor;
                float4 _LayerEndColor;
                int _LayerCount;
                float _LayerStep;
                float _FresnelWidth;
                float _TextureScale;
                float _Intensity;
                float _DepthFalloff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                output.localNormal = input.normalOS;
                output.localPos = input.positionOS.xyz;
                return output;
            }

            float2 TriplanarUV_Local(float3 localPos, float3 localNormal)
            {
                float3 absNormal = abs(localNormal);
                if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
                    return localPos.yz;
                else if (absNormal.y > absNormal.x && absNormal.y > absNormal.z)
                    return localPos.xz;
                else
                    return localPos.xy;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                float borderMask = smoothstep(0, _FresnelWidth, fresnel);
                
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(input.positionWS - rayOrigin);
                half3 layersColor = 0;
                float3 localNormal = normalize(input.localNormal);
                
                [loop]
                for (int i = 1; i <= _LayerCount; i++)
                {
                    float offset = i * _LayerStep;
                    float3 planePoint = input.positionWS + normalWS * offset;
                    float3 planeNormal = normalWS;
                    
                    float denominator = dot(rayDir, planeNormal);
                    if (abs(denominator) < 0.0001) continue;
                    float t = dot(planePoint - rayOrigin, planeNormal) / denominator;
                    if (t < 0) continue;
                    
                    float3 hitPointWorld = rayOrigin + rayDir * t;
                    float3 hitPointLocal = TransformWorldToObject(hitPointWorld);
                    
                    float2 uvLayer = TriplanarUV_Local(hitPointLocal, localNormal) * _TextureScale + 0.5;
                    half4 layerTex = SAMPLE_TEXTURE2D(_LayerTex, sampler_LayerTex, uvLayer);
                    
                    float t_layer = (float)i / _LayerCount;
                    half3 layerGradient = lerp(_LayerStartColor.rgb, _LayerEndColor.rgb, t_layer);
                    half3 layerColor = layerTex.rgb * _GlowColor.rgb * layerGradient * _Intensity;
                    
                    float depthFade = (_DepthFalloff <= 0.01) ? (1.0 - t_layer) : exp(-_DepthFalloff * t_layer);
                    layerColor *= depthFade;
                    layersColor += layerColor;
                }
                
                Light mainLight = GetMainLight();
                half3 diffuse = saturate(dot(normalWS, mainLight.direction)) * mainLight.color * albedo.rgb;
                
                half3 reflectVector = reflect(-viewDirWS, normalWS);
                half roughness = 1.0 - _Smoothness;
                half3 reflections = GlossyEnvironmentReflection(reflectVector, roughness, 1.0);
                reflections *= _ReflectionStrength;

                
                half fresnelGlass = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 2.0);
                reflections *= lerp(1.0, fresnelGlass, _Smoothness);
                
                half3 baseColor = lerp(diffuse, reflections, _Metallic);
                baseColor *= _GlassColor.rgb;
                
                half3 finalColor = baseColor + layersColor * borderMask;
                
                return half4(finalColor, _GlassVisibility);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}