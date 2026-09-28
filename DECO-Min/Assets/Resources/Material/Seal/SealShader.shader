Shader "Custom/URP/PuffyStickerRaymarch"
{
    Properties
    {
        [MainTexture] _BaseMap("Sticker RGBA (A = shape mask)", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)

        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _BaseLevel("Base Level (Object Y)", Range(-0.5, 0.5)) = -0.5
        _MaxHeight("Max Height", Range(0.0, 1.0)) = 0.2

        _BevelSampleDistance("Bevel Sample Distance (Texels)", Range(0.5, 8.0)) = 2.0

        _MaxSteps("Ray March Steps", Range(8, 96)) = 32
        _RefineSteps("Hit Refine Steps", Range(0, 8)) = 4

        _AlphaFieldScale("Alpha Field Scale", Range(0.01, 2.0)) = 0.25

        _AmbientStrength("Ambient Strength", Range(0.0, 1.0)) = 0.18
        _SpecularStrength("Specular Strength", Range(0.0, 1.0)) = 0.25
        _Smoothness("Smoothness", Range(0.01, 1.0)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_TexelSize; // x=1/width, y=1/height, z=width, w=height

                float _AlphaCutoff;
                float _BaseLevel;
                float _MaxHeight;
                float _BevelSampleDistance;

                float _MaxSteps;
                float _RefineSteps;
                float _AlphaFieldScale;

                float _AmbientStrength;
                float _SpecularStrength;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            struct FragOutput
            {
                half4 color : SV_Target;
                float depth : SV_Depth;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionWS = posInputs.positionWS;
                return OUT;
            }

            float2 PositionToUV(float3 pOS)
            {
                // Unit cube [-0.5, 0.5] を前提
                return pOS.xz + 0.5;
            }

            float SampleAlphaMask(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0))
                    return 0.0;

                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a;
            }

            float SampleBinaryMask(float2 uv)
            {
                return SampleAlphaMask(uv) >= _AlphaCutoff ? 1.0 : 0.0;
            }

            // 周辺サンプル数をかなり絞った厚み推定:
            // 中心 + 8近傍を1リングだけ見る
            float EstimateCoverage(float2 uv)
            {
                float2 o = _BaseMap_TexelSize.xy * _BevelSampleDistance;

                float sum = 0.0;
                sum += SampleBinaryMask(uv); // center

                sum += SampleBinaryMask(uv + float2( o.x,  0.0));
                sum += SampleBinaryMask(uv + float2(-o.x,  0.0));
                sum += SampleBinaryMask(uv + float2( 0.0,  o.y));
                sum += SampleBinaryMask(uv + float2( 0.0, -o.y));

                sum += SampleBinaryMask(uv + float2( o.x,  o.y));
                sum += SampleBinaryMask(uv + float2(-o.x,  o.y));
                sum += SampleBinaryMask(uv + float2( o.x, -o.y));
                sum += SampleBinaryMask(uv + float2(-o.x, -o.y));

                return sum / 9.0;
            }

            float PuffyProfile(float x)
            {
                x = saturate(x);
                // ふくらみ感を強めるプロファイル
                return sqrt(saturate(1.0 - (1.0 - x) * (1.0 - x)));
            }

            float EvaluateHeight(float2 uv)
            {
                float center = SampleBinaryMask(uv);
                if (center < 0.5)
                    return 0.0;

                float coverage = EstimateCoverage(uv);
                float profile = PuffyProfile(coverage);
                float _center = max(abs(uv.x - 0.5f), abs(uv.y - 0.5f));

                return _MaxHeight * profile * cos(_center * 3.1415f);
            }

            // field <= 0 なら内部
            float EvaluateField(float3 pOS)
            {
                float2 uv = PositionToUV(pOS);
                float alpha = SampleAlphaMask(uv);

                // alpha による側面定義
                float alphaTerm = (_AlphaCutoff - alpha) * _AlphaFieldScale;

                // 高さ
                float height = EvaluateHeight(uv);
                float topY = min(_BaseLevel + height, 0.5);

                // baseY <= y <= topY を満たすなら verticalTerm <= 0
                float verticalTerm = max(_BaseLevel - pOS.y, pOS.y - topY);

                return max(alphaTerm, verticalTerm);
            }

            bool RayBoxIntersection(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax, out float tEnter, out float tExit)
            {
                float3 invDir = 1.0 / rayDir;

                float3 t0 = (boxMin - rayOrigin) * invDir;
                float3 t1 = (boxMax - rayOrigin) * invDir;

                float3 tsmaller = min(t0, t1);
                float3 tbigger  = max(t0, t1);

                tEnter = max(max(tsmaller.x, tsmaller.y), tsmaller.z);
                tExit  = min(min(tbigger.x, tbigger.y), tbigger.z);

                return tExit >= max(tEnter, 0.0);
            }

            float3 EstimateNormalOS(float3 pOS)
            {
                float epsX = max(_BaseMap_TexelSize.x, 1e-4);
                float epsZ = max(_BaseMap_TexelSize.y, 1e-4);
                float epsY = max(_MaxHeight * 0.02, 1e-4);

                float dx = EvaluateField(pOS + float3(epsX, 0, 0)) - EvaluateField(pOS - float3(epsX, 0, 0));
                float dy = EvaluateField(pOS + float3(0, epsY, 0)) - EvaluateField(pOS - float3(0, epsY, 0));
                float dz = EvaluateField(pOS + float3(0, 0, epsZ)) - EvaluateField(pOS - float3(0, 0, epsZ));

                return normalize(float3(dx, dy, dz));
            }

            FragOutput frag(Varyings IN)
            {
                FragOutput OUT;

                float3 camWS = GetCameraPositionWS();
                float3 rayOriginOS = mul(unity_WorldToObject, float4(camWS, 1.0)).xyz;

                // 現在描画中の cube 表面点へ向かうレイ
                float3 rayDirOS = normalize(IN.positionOS - rayOriginOS);

                float tEnter, tExit;
                bool hitBox = RayBoxIntersection(
                    rayOriginOS,
                    rayDirOS,
                    float3(-0.5, -0.5, -0.5),
                    float3( 0.5,  0.5,  0.5),
                    tEnter,
                    tExit
                );

                if (!hitBox)
                    clip(-1.0);

                tEnter = max(tEnter, 0.0) + 1e-4;
                tExit  = max(tExit,  tEnter);

                int steps = (int)round(_MaxSteps);
                int refineSteps = (int)round(_RefineSteps);

                float totalLen = tExit - tEnter;
                float dt = totalLen / max(steps, 1);

                float prevT = tEnter;
                float prevField = EvaluateField(rayOriginOS + rayDirOS * prevT);
                bool prevInside = (prevField <= 0.0);

                bool found = false;
                float hitT = tEnter;

                if (prevInside)
                {
                    found = true;
                    hitT = prevT;
                }
                else
                {
                    [loop]
                    for (int i = 0; i < 96; ++i)
                    {
                        if (i >= steps)
                            break;

                        float currT = tEnter + dt * (i + 1);
                        float3 pOS = rayOriginOS + rayDirOS * currT;
                        float field = EvaluateField(pOS);
                        bool inside = (field <= 0.0);

                        if (inside && !prevInside)
                        {
                            // 2分探索で少しだけヒット位置を詰める
                            float a = prevT;
                            float b = currT;

                            [loop]
                            for (int j = 0; j < 8; ++j)
                            {
                                if (j >= refineSteps)
                                    break;

                                float m = 0.5 * (a + b);
                                float mf = EvaluateField(rayOriginOS + rayDirOS * m);

                                if (mf <= 0.0)
                                    b = m;
                                else
                                    a = m;
                            }

                            found = true;
                            hitT = b;
                            break;
                        }

                        prevT = currT;
                        prevInside = inside;
                    }
                }

                if (!found)
                    clip(-1.0);

                float3 hitOS = rayOriginOS + rayDirOS * hitT;
                float3 hitWS = TransformObjectToWorld(hitOS);

                float2 uv = PositionToUV(hitOS);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;

                float3 normalOS = EstimateNormalOS(hitOS);
                float3 normalWS = normalize(TransformObjectToWorldNormal(normalOS));

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(camWS - hitWS);
                float3 H = normalize(L + V);

                float NdotL = saturate(dot(normalWS, L));
                float NdotH = saturate(dot(normalWS, H));

                float diff = NdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                float specPower = lerp(8.0, 128.0, _Smoothness);
                float spec = pow(NdotH, specPower) * _SpecularStrength * diff;

                half3 color =
                    albedo * (_AmbientStrength + diff * mainLight.color) +
                    spec * mainLight.color;

                float4 hitCS = TransformWorldToHClip(hitWS);

                OUT.color = half4(color, 1.0);
                OUT.depth = hitCS.z / hitCS.w;
                return OUT;
            }

            ENDHLSL
        }
    }
}