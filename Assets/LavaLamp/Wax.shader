Shader "LavaLamp/Wax"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.4, 0.2, 1)
        _TopColor ("Top Gradient Color", Color) = (0.9, 0.2, 0.1, 1)
        _BottomColor ("Bottom Gradient Color", Color) = (1, 0.6, 0.3, 1)
        _GradientStrength ("Gradient Strength", Range(0,1)) = 0.5
        
        _EmissionColor ("Emission Color", Color) = (1, 0.3, 0.1, 1)
        _EmissionStrength ("Emission Strength", Range(0,2)) = 0.3
        
        _SubsurfaceColor ("Subsurface Color", Color) = (1, 0.5, 0.2, 1)
        _SubsurfaceStrength ("Subsurface Strength", Range(0,1)) = 0.6
        _SubsurfacePower ("Subsurface Power", Range(0.1,10)) = 3.0
        
        _RimStrength ("Rim Strength", Range(0,1)) = 0.4
        _RimPower ("Rim Power", Range(0.1,8)) = 4.0
        
        _Smoothness ("Smoothness", Range(0,1)) = 0.4
        _StepSize ("Step Size", Range(0.002, 0.05)) = 0.015
        _MaxSteps ("Max Steps", Range(16, 256)) = 128
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "WaxVolume"
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata { float4 vertex : POSITION; };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            struct BlobData
            {
                float3 position;
                float4 rotation;
                float3 scale;
                float blend;
            };

            float3 _CylinderCenter;
            float3 _CylinderUp;
            float  _CylinderRadius;
            float  _CylinderHeight;

            StructuredBuffer<BlobData> _BlobBuffer;
            int _BlobsCount;

            half4 _BaseColor, _TopColor, _BottomColor, _EmissionColor, _SubsurfaceColor;
            half  _GradientStrength, _EmissionStrength, _SubsurfaceStrength, _SubsurfacePower;
            half  _RimStrength, _RimPower, _Smoothness;
            float _StepSize;
            int   _MaxSteps;

            float smin(float a, float b, float k)
            {
                float h = max(k - abs(a - b), 0.0) / k;
                return min(a, b) - h * h * k * 0.25;
            }
            
            float4 quat_conjugate(float4 q) { return float4(-q.x, -q.y, -q.z, q.w); }
 
            float4 quat_mul(float4 q, float4 r)
            {
                return float4(
                    q.w * r.x + q.x * r.w + q.y * r.z - q.z * r.y,
                    q.w * r.y - q.x * r.z + q.y * r.w + q.z * r.x,
                    q.w * r.z + q.x * r.y - q.y * r.x + q.z * r.w,
                    q.w * r.w - q.x * r.x - q.y * r.y - q.z * r.z
                );
            }
 
            float3 quat_rotate_vector(float3 v, float4 q)
            {
                float4 qv = float4(v, 0);
                float4 q_conj = quat_conjugate(q);
                float4 rotated = quat_mul(quat_mul(q, qv), q_conj);
                return rotated.xyz;
            }

            float blobSDF(float3 p, BlobData blob)
            {
                float3 localPos = p - blob.position;
                localPos = quat_rotate_vector(localPos, quat_conjugate(blob.rotation));
                float3 s = localPos / blob.scale;
                return length(s) - 1.0;
            }

            float sceneSDF(float3 p)
            {
                float d = 1e10;
                for (int i = 0; i < _BlobsCount; i++)
                {
                    float bd = blobSDF(p, _BlobBuffer[i]);
                    d = smin(d, bd, _BlobBuffer[i].blend);
                }
                return d;
            }

            float3 calcNormal(float3 p)
            {
                const float eps = 0.002;
                return normalize(float3(
                    sceneSDF(p + float3(eps,0,0)) - sceneSDF(p - float3(eps,0,0)),
                    sceneSDF(p + float3(0,eps,0)) - sceneSDF(p - float3(0,eps,0)),
                    sceneSDF(p + float3(0,0,eps)) - sceneSDF(p - float3(0,0,eps))
                ));
            }

            float3x3 buildCylinderToWorldRot(float3 up)
            {
                float3 wUp = normalize(up);
                float3 wRight = abs(wUp.y) < 0.999 ? normalize(cross(float3(0,1,0), wUp)) : float3(1,0,0);
                float3 wForward = normalize(cross(wRight, wUp));
                return float3x3(wRight, wUp, wForward);
            }

            bool intersectCylinder(float3 ro, float3 rd, float r, float halfH, out float tEnter, out float tExit)
            {
                float a = rd.x*rd.x + rd.z*rd.z;
                float b = 2.0 * (ro.x*rd.x + ro.z*rd.z);
                float c = ro.x*ro.x + ro.z*ro.z - r*r;
                float disc = b*b - 4*a*c;
                if (disc < 0) { tEnter = tExit = 0; return false; }

                float sqrtDisc = sqrt(disc);
                float t1 = (-b - sqrtDisc) / (2*a);
                float t2 = (-b + sqrtDisc) / (2*a);
                if (t1 > t2) { float t = t1; t1 = t2; t2 = t; }

                float y1 = ro.y + t1*rd.y;
                float y2 = ro.y + t2*rd.y;

                float tCap1 = (-halfH - ro.y) / rd.y;
                float tCap2 = ( halfH - ro.y) / rd.y;
                float tCapNear = min(tCap1, tCap2);
                float tCapFar  = max(tCap1, tCap2);

                tEnter = max(t1, tCapNear);
                tExit  = min(t2, tCapFar);
                return tEnter < tExit;
            }

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs vpi = GetVertexPositionInputs(v.vertex.xyz);
                o.pos = vpi.positionCS;
                o.worldPos = vpi.positionWS;
                return o;
            }

            float4 frag (v2f i, out float outDepth : SV_Depth) : SV_Target
            {
                outDepth = 0;
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(i.worldPos - rayOrigin);

                float3x3 cylRot = buildCylinderToWorldRot(_CylinderUp);
                float3x3 worldToCylRot = transpose(cylRot); 

                float3 ro = mul(worldToCylRot, rayOrigin - _CylinderCenter);
                float3 rd = mul(worldToCylRot, rayDir);
                float halfH = _CylinderHeight * 0.5;

                float tEnter, tExit;
                if (!intersectCylinder(ro, rd, _CylinderRadius, halfH, tEnter, tExit))
                    discard;

                float t = tEnter;
                bool hit = false;
                float hitT = 0;

                for (int step = 0; step < _MaxSteps; step++)
                {
                    float3 p = ro + t * rd;
                    float d = sceneSDF(p);
                    if (d <= 0.0)
                    {
                        hit = true;
                        hitT = t;
                        break;
                    }
                    t += _StepSize;
                    if (t >= tExit) break;
                }

                if (!hit) discard;

                float3 hitPosLocal = ro + hitT * rd;
                float3 Nlocal = calcNormal(hitPosLocal);

                float3 worldN = mul(cylRot, Nlocal);
                float3 worldP = _CylinderCenter + mul(cylRot, hitPosLocal);

              
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(_WorldSpaceCameraPos - worldP);
                float3 H = normalize(L + V);
                
               
                float heightNorm = saturate((hitPosLocal.y + halfH) / _CylinderHeight);
                float3 gradientColor = lerp(_BottomColor.rgb, _TopColor.rgb, heightNorm);
                float3 baseCol = lerp(_BaseColor.rgb, gradientColor, _GradientStrength);
                
                float NdotL = saturate(dot(worldN, L));
                float backLight = saturate(-dot(worldN, L));
                float sss = pow(backLight, _SubsurfacePower) * _SubsurfaceStrength;
                float3 subsurface = _SubsurfaceColor.rgb * sss;
                
                float NdotV = saturate(dot(worldN, V));
                float fresnel = pow(1.0 - NdotV, _RimPower) * _RimStrength;
                float3 rim = baseCol * fresnel;
                
                float spec = pow(saturate(dot(worldN, H)), exp2(1.0 + _Smoothness * 6.0)) * 0.3;
                
                float3 emission = _EmissionColor.rgb * _EmissionStrength;
                
                float3 ambient = SampleSH(worldN) * 0.5;
                
                float3 lighting = ambient + mainLight.color * NdotL * 0.7;
                float3 finalColor = baseCol * lighting;
                finalColor += subsurface;
                finalColor += rim; 
                finalColor += spec; 
                finalColor += emission;
                
                finalColor = pow(finalColor, 0.9); 
                
                finalColor = LinearToSRGB(finalColor);
                
                float4 clipPos = TransformWorldToHClip(worldP);
                outDepth = clipPos.z / clipPos.w;

                return float4(finalColor, 1);
            }
            ENDHLSL
        }
    }
    
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
}

