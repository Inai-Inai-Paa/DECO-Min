Shader "Hidden/Custom/VolumetricLight"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "VolumetricLight"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            int _SampleCount;
            int _DebugMode;

            float _MaxDistance;
            float _Density;
            float _Intensity;

            float _Anisotropy;
            float _ViewAngleCompensation;

            float4 _LightTint;

            float _ShadowThreshold;
            float _ShadowSoftness;

            float _MaxLightContribution;

            float _NoiseStrength;
            float _NoiseScale;

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);

                p += dot(
                    p,
                    p.yzx + 33.33
                );

                return frac(
                    (p.x + p.y) * p.z
                );
            }

            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0, 0, 0));
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));

                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            float PhaseFunction(
                float cosTheta,
                float g
            )
            {
                g = clamp(g, 0.0, 0.95);

                float g2 = g * g;

                float denominator = max(
                    1.0 + g2 - 2.0 * g * cosTheta,
                    0.0001
                );

                return (1.0 - g2) /
                    (4.0 * PI * pow(denominator, 1.5));
            }

            float GetShadow(float3 positionWS)
            {
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)

                    float4 shadowCoord =
                        TransformWorldToShadowCoord(positionWS);

                    return MainLightRealtimeShadow(
                        shadowCoord
                    );

                #else

                    return 0.0;

                #endif
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV =
                    input.positionCS.xy *
                    rcp(_ScaledScreenParams.xy);

                half4 sceneColor =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        input.texcoord
                    );

                #if UNITY_REVERSED_Z
                    float rawDepth = SampleSceneDepth(screenUV);
                    bool isSky = rawDepth <= 0.00001;
                    float reconstructionDepth =
                        isSky ? 0.00001 : rawDepth;
                #else
                    float rawDepth = SampleSceneDepth(screenUV);
                    bool isSky = rawDepth >= 0.99999;
                    float reconstructionDepth =
                        isSky ? 0.99999 : rawDepth;
                #endif

                float3 cameraWS =
                    GetCameraPositionWS();

                float3 surfaceWS =
                    ComputeWorldSpacePosition(
                        screenUV,
                        reconstructionDepth,
                        UNITY_MATRIX_I_VP
                    );

                float3 rayVector =
                    surfaceWS - cameraWS;

                float surfaceDistance =
                    length(rayVector);

                float3 rayDirection =
                    rayVector /
                    max(surfaceDistance, 0.0001);

                float rayLength =
                    isSky
                    ? _MaxDistance
                    : min(surfaceDistance, _MaxDistance);

                if (_DebugMode == 5)
                {
                    float3 relativePosition =
                        surfaceWS - cameraWS;

                    return half4(
                        frac(abs(relativePosition) * 0.1),
                        1.0
                    );
                }

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float shadowKeyword = 1.0;
                #else
                    float shadowKeyword = 0.0;
                #endif

                if (_DebugMode == 3)
                {
                    return half4(
                        shadowKeyword,
                        shadowKeyword,
                        shadowKeyword,
                        1.0
                    );
                }

                if (shadowKeyword < 0.5)
                {
                    if (_DebugMode == 1 ||
                        _DebugMode == 2 ||
                        _DebugMode == 4)
                    {
                        return half4(0.0, 0.0, 0.0, 1.0);
                    }

                    return sceneColor;
                }

                if (_DebugMode == 4)
                {
                    if (isSky)
                    {
                        return half4(0.0, 0.0, 1.0, 1.0);
                    }

                    float surfaceShadow =
                        GetShadow(surfaceWS);

                    return half4(
                        surfaceShadow,
                        surfaceShadow,
                        surfaceShadow,
                        1.0
                    );
                }

                Light mainLight =
                    GetMainLight();

                float3 lightDirection =
                    normalize(mainLight.direction);

                float cosTheta =
                    dot(rayDirection, lightDirection);

                float phase =
                    PhaseFunction(
                        cosTheta,
                        _Anisotropy
                    );

                float isotropicPhase =
                    1.0 / (4.0 * PI);

                phase = lerp(
                    phase,
                    isotropicPhase,
                    _ViewAngleCompensation
                );

                int count =
                    clamp(_SampleCount, 16, 256);

                float stepSize =
                    rayLength / count;

                float extinction =
                    exp(-_Density * stepSize);

                float transmittance = 1.0;

                float accumulatedLight = 0.0;
                float accumulatedShadow = 0.0;

                float jitter =
                    InterleavedGradientNoise(
                        input.positionCS.xy,
                        0
                    );

                for (int i = 0; i < count; i++)
                {
                    float distanceAlongRay =
                        (i + jitter) * stepSize;

                    float3 samplePosition =
                        cameraWS +
                        rayDirection * distanceAlongRay;

                    float shadow =
                        GetShadow(samplePosition);

                    float lightVisibility =
                        smoothstep(
                            _ShadowThreshold,
                            _ShadowThreshold +
                            max(_ShadowSoftness, 0.001),
                            shadow
                        );

                    float noise =
                        ValueNoise(
                            samplePosition * _NoiseScale
                        );

                    float noiseMultiplier =
                        lerp(
                            1.0,
                            noise * 2.0,
                            _NoiseStrength
                        );

                    float contribution =
                        transmittance *
                        (1.0 - extinction) *
                        lightVisibility *
                        noiseMultiplier;

                    accumulatedLight += contribution;
                    accumulatedShadow += lightVisibility;

                    transmittance *= extinction;
                }

                if (_DebugMode == 2)
                {
                    float visibility =
                        accumulatedShadow / count;

                    return half4(
                        visibility,
                        visibility,
                        visibility,
                        1.0
                    );
                }

                float3 volumetricColor =
                    accumulatedLight *
                    mainLight.color.rgb *
                    _LightTint.rgb *
                    phase *
                    _Intensity;

                volumetricColor = min(
                    volumetricColor,
                    _MaxLightContribution
                );

                if (_DebugMode == 1)
                {
                    return half4(
                        volumetricColor,
                        1.0
                    );
                }

                return half4(
                    sceneColor.rgb + volumetricColor,
                    sceneColor.a
                );
            }

            ENDHLSL
        }
    }

    Fallback Off
}