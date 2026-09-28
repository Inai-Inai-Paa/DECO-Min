Shader "Custom/VolumetricSticker"
{
    Properties
    {
        [NoScaleOffset][MainTexture]
        _MainTex ("Sticker Texture", 2D) = "white" {}

        [MainColor]
        _Tint ("Tint", Color) = (1, 1, 1, 1)

        [Header(Shape)]
        _AlphaCutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Thickness ("Thickness", Range(0.001, 0.95)) = 0.12
        _BaseThickness ("Base Thickness", Range(0.0, 0.5)) = 0.01
        _Roundness ("Profile Roundness", Range(0.0, 1.0)) = 0.05
        _ProfilePower ("Profile Power", Range(0.25, 4.0)) = 1.0

        [Header(Normal)]
        _NormalStrength ("Normal Strength", Range(0.0, 4.0)) = 1.0
        _NormalSampleRadius ("Normal Sample Radius", Range(0.5, 4.0)) = 1.5
        _SideBlendHeight ("Side Blend Height", Range(0.001, 0.1)) = 0.02

        [Header(Surface)]
        _AmbientStrength ("Ambient Strength", Range(0.0, 1.0)) = 0.18
        _SpecularStrength ("Specular Strength", Range(0.0, 2.0)) = 0.35
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.82

        [Header(Resin Glass)]
        _ClearCoatStrength ("Clear Coat Strength", Range(0.0, 2.0)) = 0.70
        _FresnelStrength ("Fresnel Strength", Range(0.0, 2.0)) = 0.35
        _FresnelPower ("Fresnel Power", Range(1.0, 8.0)) = 3.5
        _ReflectionStrength ("Reflection Strength", Range(0.0, 2.0)) = 0.25
        _ReflectionBlur ("Reflection Blur", Range(0.0, 1.0)) = 0.25
        _EdgeBrighten ("Edge Brighten", Range(0.0, 1.0)) = 0.08
        _SpecularTint ("Specular Tint", Color) = (1, 1, 1, 1)

        [Header(Quality)]
        [IntRange]
        _RaySteps ("Ray Steps", Range(8, 96)) = 24

        [IntRange]
        _RefineSteps ("Hit Refine Steps", Range(0, 8)) = 4

        [HideInInspector]
        _HeightTex ("Normalized Height Cache", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Forward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

            // =========================================================
            // Textures
            // =========================================================

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_HeightTex);
            SAMPLER(sampler_HeightTex);

            // =========================================================
            // Material parameters
            // =========================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;
                float4 _Tint;
                float4 _SpecularTint;

                float _AlphaCutoff;

                float _Thickness;
                float _BaseThickness;
                float _Roundness;
                float _ProfilePower;

                float _NormalStrength;
                float _NormalSampleRadius;
                float _SideBlendHeight;

                float _AmbientStrength;
                float _SpecularStrength;
                float _Smoothness;

                float _ClearCoatStrength;
                float _FresnelStrength;
                float _FresnelPower;
                float _ReflectionStrength;
                float _ReflectionBlur;
                float _EdgeBrighten;

                float _RaySteps;
                float _RefineSteps;

            CBUFFER_END

            // =========================================================
            // Structures
            // =========================================================

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            struct FragmentOutput
            {
                half4 color : SV_Target;
                float depth : SV_Depth;
            };

            // =========================================================
            // Vertex
            // =========================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionOS =
                    input.positionOS.xyz;

                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );

                return output;
            }

            // =========================================================
            // Coordinates
            //
            // XZ : sticker plane
            // Y  : thickness
            //
            // Cube bounds:
            // [-0.5, +0.5]
            // =========================================================

            float2 PositionToUV(float3 positionOS)
            {
                return positionOS.xz + 0.5;
            }

            bool IsUVInside(float2 uv)
            {
                return
                    uv.x >= 0.0 &&
                    uv.x <= 1.0 &&
                    uv.y >= 0.0 &&
                    uv.y <= 1.0;
            }

            // =========================================================
            // Cached normalized height
            // =========================================================

            float SampleHeight01(float2 uv)
            {
                if (!IsUVInside(uv))
                    return 0.0;

                return SAMPLE_TEXTURE2D_LOD(
                    _HeightTex,
                    sampler_HeightTex,
                    uv,
                    0
                ).r;
            }

            // =========================================================
            // Height shaping
            // =========================================================

            float Height01ToProfile(float height01)
            {
                float x =
                    saturate(height01);

                // Linear distance profile.
                float linearProfile =
                    x;

                // Rounded quarter-circle-like profile.
                float inverse =
                    1.0 - x;

                float roundedProfile =
                    sqrt(
                        saturate(
                            1.0 -
                            inverse * inverse
                        )
                    );

                float shaped =
                    lerp(
                        linearProfile,
                        roundedProfile,
                        saturate(_Roundness)
                    );

                shaped =
                    max(
                        shaped,
                        0.00001
                    );

                return pow(
                    shaped,
                    max(
                        _ProfilePower,
                        0.00001
                    )
                );
            }

            float Height01ToHeight(float height01)
            {
                if (height01 <= 0.0)
                    return 0.0;

                float topHeight =
                    min(
                        _Thickness,
                        0.999
                    );

                float baseHeight =
                    min(
                        _BaseThickness,
                        topHeight
                    );

                float profile =
                    Height01ToProfile(
                        height01
                    );

                return lerp(
                    baseHeight,
                    topHeight,
                    profile
                );
            }

            float SampleHeight(float2 uv)
            {
                return Height01ToHeight(
                    SampleHeight01(uv)
                );
            }

            // =========================================================
            // Volume test
            // =========================================================

            bool IsInsideSticker(float3 positionOS)
            {
                float2 uv =
                    PositionToUV(
                        positionOS
                    );

                if (!IsUVInside(uv))
                    return false;

                float height01 =
                    SampleHeight01(uv);

                if (height01 <= 0.0)
                    return false;

                float height =
                    Height01ToHeight(
                        height01
                    );

                float localHeight =
                    positionOS.y + 0.5;

                return
                    localHeight >= 0.0 &&
                    localHeight <= height;
            }

            // =========================================================
            // Normal helpers
            // =========================================================

            float2 GetHeightTexelSize()
            {
                uint textureWidth;
                uint textureHeight;

                _HeightTex.GetDimensions(
                    textureWidth,
                    textureHeight
                );

                float2 textureSize =
                    max(
                        float2(
                            textureWidth,
                            textureHeight
                        ),
                        float2(
                            1.0,
                            1.0
                        )
                    );

                return
                    1.0 /
                    textureSize;
            }

            // Actual height-field position.
            float3 SurfacePositionOS(float2 uv)
            {
                float height =
                    SampleHeight(uv);

                return float3(
                    uv.x - 0.5,
                    -0.5 + height,
                    uv.y - 0.5
                );
            }

            // Same surface but with vertical displacement scaled only for
            // normal calculation.
            //
            // NormalStrength = 0 -> flat top normal.
            // NormalStrength = 1 -> geometric height-field normal.
            float3 NormalSurfacePositionOS(float2 uv)
            {
                float height =
                    SampleHeight(uv);

                height *=
                    _NormalStrength;

                return float3(
                    uv.x - 0.5,
                    -0.5 + height,
                    uv.y - 0.5
                );
            }

            // =========================================================
            // Top surface normal
            //
            // Build actual neighbouring surface positions in world space.
            // This naturally handles non-uniform object scale.
            // =========================================================

            float3 EstimateTopNormalWS(float2 uv)
            {
                float2 texelSize =
                    GetHeightTexelSize() *
                    max(
                        _NormalSampleRadius,
                        0.5
                    );

                float2 uvLeft =
                    saturate(
                        uv -
                        float2(
                            texelSize.x,
                            0.0
                        )
                    );

                float2 uvRight =
                    saturate(
                        uv +
                        float2(
                            texelSize.x,
                            0.0
                        )
                    );

                float2 uvDown =
                    saturate(
                        uv -
                        float2(
                            0.0,
                            texelSize.y
                        )
                    );

                float2 uvUp =
                    saturate(
                        uv +
                        float2(
                            0.0,
                            texelSize.y
                        )
                    );

                float3 positionLeftWS =
                    TransformObjectToWorld(
                        NormalSurfacePositionOS(
                            uvLeft
                        )
                    );

                float3 positionRightWS =
                    TransformObjectToWorld(
                        NormalSurfacePositionOS(
                            uvRight
                        )
                    );

                float3 positionDownWS =
                    TransformObjectToWorld(
                        NormalSurfacePositionOS(
                            uvDown
                        )
                    );

                float3 positionUpWS =
                    TransformObjectToWorld(
                        NormalSurfacePositionOS(
                            uvUp
                        )
                    );

                float3 tangentX =
                    positionRightWS -
                    positionLeftWS;

                float3 tangentZ =
                    positionUpWS -
                    positionDownWS;

                float3 normalWS =
                    normalize(
                        cross(
                            tangentZ,
                            tangentX
                        )
                    );

                float3 upWS =
                    normalize(
                        TransformObjectToWorldNormal(
                            float3(
                                0.0,
                                1.0,
                                0.0
                            )
                        )
                    );

                // Guard against degenerate samples.
                if (
                    dot(
                        normalWS,
                        normalWS
                    ) <
                    0.000001
                )
                {
                    return upWS;
                }

                // Ensure top surface points outward.
                if (
                    dot(
                        normalWS,
                        upWS
                    ) <
                    0.0
                )
                {
                    normalWS =
                        -normalWS;
                }

                return normalWS;
            }

            // =========================================================
            // Side surface normal
            //
            // Height gradient points toward the interior.
            // Negative gradient therefore points outward.
            // =========================================================

            float3 EstimateSideNormalWS(float2 uv)
            {
                float2 texelSize =
                    GetHeightTexelSize() *
                    max(
                        _NormalSampleRadius,
                        0.5
                    );

                float2 uvLeft =
                    saturate(
                        uv -
                        float2(
                            texelSize.x,
                            0.0
                        )
                    );

                float2 uvRight =
                    saturate(
                        uv +
                        float2(
                            texelSize.x,
                            0.0
                        )
                    );

                float2 uvDown =
                    saturate(
                        uv -
                        float2(
                            0.0,
                            texelSize.y
                        )
                    );

                float2 uvUp =
                    saturate(
                        uv +
                        float2(
                            0.0,
                            texelSize.y
                        )
                    );

                float heightLeft =
                    SampleHeight01(
                        uvLeft
                    );

                float heightRight =
                    SampleHeight01(
                        uvRight
                    );

                float heightDown =
                    SampleHeight01(
                        uvDown
                    );

                float heightUp =
                    SampleHeight01(
                        uvUp
                    );

                float deltaX =
                    max(
                        uvRight.x -
                        uvLeft.x,
                        0.000001
                    );

                float deltaY =
                    max(
                        uvUp.y -
                        uvDown.y,
                        0.000001
                    );

                float derivativeX =
                    (
                        heightRight -
                        heightLeft
                    ) /
                    deltaX;

                float derivativeZ =
                    (
                        heightUp -
                        heightDown
                    ) /
                    deltaY;

                float2 gradient =
                    float2(
                        derivativeX,
                        derivativeZ
                    );

                float gradientLengthSquared =
                    dot(
                        gradient,
                        gradient
                    );

                if (
                    gradientLengthSquared <
                    0.000001
                )
                {
                    return normalize(
                        TransformObjectToWorldNormal(
                            float3(
                                0.0,
                                1.0,
                                0.0
                            )
                        )
                    );
                }

                float3 sideNormalOS =
                    normalize(
                        float3(
                            -gradient.x,
                            0.0,
                            -gradient.y
                        )
                    );

                return normalize(
                    TransformObjectToWorldNormal(
                        sideNormalOS
                    )
                );
            }

            // =========================================================
            // Combined top / side / bottom normal
            // =========================================================

            float3 EstimateSurfaceNormalWS(
                float3 hitPositionOS
            )
            {
                float2 uv =
                    PositionToUV(
                        hitPositionOS
                    );

                float surfaceHeight =
                    SampleHeight(uv);

                float localHeight =
                    hitPositionOS.y +
                    0.5;

                float blendWidth =
                    max(
                        _SideBlendHeight,
                        0.000001
                    );

                float3 topNormalWS =
                    EstimateTopNormalWS(
                        uv
                    );

                float3 sideNormalWS =
                    EstimateSideNormalWS(
                        uv
                    );

                float3 bottomNormalWS =
                    normalize(
                        TransformObjectToWorldNormal(
                            float3(
                                0.0,
                                -1.0,
                                0.0
                            )
                        )
                    );

                // Distance from ray hit to top height surface.
                float topGap =
                    abs(
                        surfaceHeight -
                        localHeight
                    );

                // Distance from ray hit to base plane.
                float bottomGap =
                    abs(
                        localHeight
                    );

                float topWeight =
                    1.0 -
                    smoothstep(
                        0.0,
                        blendWidth,
                        topGap
                    );

                float bottomWeight =
                    1.0 -
                    smoothstep(
                        0.0,
                        blendWidth,
                        bottomGap
                    );

                // Prevent both top and bottom from becoming >1 together
                // on extremely thin regions.
                float surfaceWeightSum =
                    topWeight +
                    bottomWeight;

                if (
                    surfaceWeightSum >
                    1.0
                )
                {
                    topWeight /=
                        surfaceWeightSum;

                    bottomWeight /=
                        surfaceWeightSum;
                }

                float sideWeight =
                    saturate(
                        1.0 -
                        topWeight -
                        bottomWeight
                    );

                float3 normalWS =
                    topNormalWS *
                    topWeight +
                    sideNormalWS *
                    sideWeight +
                    bottomNormalWS *
                    bottomWeight;

                float lengthSquared =
                    dot(
                        normalWS,
                        normalWS
                    );

                if (
                    lengthSquared <
                    0.000001
                )
                {
                    return topNormalWS;
                }

                return normalize(
                    normalWS
                );
            }

            // =========================================================
            // Ray / Box intersection
            // =========================================================

            float SafeDirection(float value)
            {
                const float epsilon =
                    0.000001;

                if (
                    abs(value) >=
                    epsilon
                )
                {
                    return value;
                }

                return
                    value < 0.0
                    ? -epsilon
                    : epsilon;
            }

            bool RayBox(
                float3 origin,
                float3 direction,
                out float tEnter,
                out float tExit
            )
            {
                float3 safeDirection =
                    float3(
                        SafeDirection(
                            direction.x
                        ),
                        SafeDirection(
                            direction.y
                        ),
                        SafeDirection(
                            direction.z
                        )
                    );

                float3 inverseDirection =
                    rcp(
                        safeDirection
                    );

                float3 t0 =
                    (
                        -0.5 -
                        origin
                    ) *
                    inverseDirection;

                float3 t1 =
                    (
                        0.5 -
                        origin
                    ) *
                    inverseDirection;

                float3 nearT =
                    min(
                        t0,
                        t1
                    );

                float3 farT =
                    max(
                        t0,
                        t1
                    );

                tEnter =
                    max(
                        max(
                            nearT.x,
                            nearT.y
                        ),
                        nearT.z
                    );

                tExit =
                    min(
                        min(
                            farT.x,
                            farT.y
                        ),
                        farT.z
                    );

                return
                    tExit >=
                    max(
                        tEnter,
                        0.0
                    );
            }

            // =========================================================
            // Fragment
            // =========================================================

            FragmentOutput Frag(Varyings input)
            {
                FragmentOutput output;

                float3 cameraPositionWS =
                    GetCameraPositionWS();

                float3 cameraPositionOS =
                    TransformWorldToObject(
                        cameraPositionWS
                    );

                float3 rayDirectionOS =
                    normalize(
                        input.positionOS -
                        cameraPositionOS
                    );

                // -----------------------------------------------------
                // Unit cube intersection
                // -----------------------------------------------------

                float tEnter;
                float tExit;

                if (
                    !RayBox(
                        cameraPositionOS,
                        rayDirectionOS,
                        tEnter,
                        tExit
                    )
                )
                {
                    discard;
                }

                tEnter =
                    max(
                        tEnter,
                        0.0
                    ) +
                    0.0001;

                tExit =
                    max(
                        tExit,
                        tEnter
                    );

                // -----------------------------------------------------
                // Ray march
                // -----------------------------------------------------

                int raySteps =
                    clamp(
                        (int)round(
                            _RaySteps
                        ),
                        1,
                        96
                    );

                int refineSteps =
                    clamp(
                        (int)round(
                            _RefineSteps
                        ),
                        0,
                        8
                    );

                float rayLength =
                    tExit -
                    tEnter;

                float stepLength =
                    rayLength /
                    max(
                        (float)raySteps,
                        1.0
                    );

                float previousT =
                    tEnter;

                bool previousInside =
                    IsInsideSticker(
                        cameraPositionOS +
                        rayDirectionOS *
                        previousT
                    );

                bool found =
                    false;

                float hitT =
                    previousT;

                if (previousInside)
                {
                    found =
                        true;
                }
                else
                {
                    [loop]
                    for (
                        int i = 0;
                        i < 96;
                        ++i
                    )
                    {
                        if (
                            i >=
                            raySteps
                        )
                        {
                            break;
                        }

                        float currentT =
                            tEnter +
                            stepLength *
                            (
                                i +
                                1
                            );

                        float3 currentPositionOS =
                            cameraPositionOS +
                            rayDirectionOS *
                            currentT;

                        bool currentInside =
                            IsInsideSticker(
                                currentPositionOS
                            );

                        if (
                            currentInside &&
                            !previousInside
                        )
                        {
                            float outsideT =
                                previousT;

                            float insideT =
                                currentT;

                            // Binary refinement
                            [loop]
                            for (
                                int j = 0;
                                j < 8;
                                ++j
                            )
                            {
                                if (
                                    j >=
                                    refineSteps
                                )
                                {
                                    break;
                                }

                                float middleT =
                                    0.5 *
                                    (
                                        outsideT +
                                        insideT
                                    );

                                float3 middlePositionOS =
                                    cameraPositionOS +
                                    rayDirectionOS *
                                    middleT;

                                if (
                                    IsInsideSticker(
                                        middlePositionOS
                                    )
                                )
                                {
                                    insideT =
                                        middleT;
                                }
                                else
                                {
                                    outsideT =
                                        middleT;
                                }
                            }

                            hitT =
                                insideT;

                            found =
                                true;

                            break;
                        }

                        previousT =
                            currentT;

                        previousInside =
                            currentInside;
                    }
                }

                if (!found)
                    discard;

                // -----------------------------------------------------
                // Hit position
                // -----------------------------------------------------

                float3 hitPositionOS =
                    cameraPositionOS +
                    rayDirectionOS *
                    hitT;

                float2 uv =
                    PositionToUV(
                        hitPositionOS
                    );

                // -----------------------------------------------------
                // Albedo
                // -----------------------------------------------------

                half4 albedoSample =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv
                    );

                if (
                    albedoSample.a <
                    _AlphaCutoff
                )
                {
                    discard;
                }

                half3 albedo =
                    albedoSample.rgb *
                    _Tint.rgb;

                // -----------------------------------------------------
                // Proper surface normal
                // -----------------------------------------------------

                float3 normalWS =
                    EstimateSurfaceNormalWS(
                        hitPositionOS
                    );

                float3 hitPositionWS =
                    TransformObjectToWorld(
                        hitPositionOS
                    );

                // -----------------------------------------------------
                // Main light
                // -----------------------------------------------------

                Light mainLight =
                    GetMainLight();

                float3 lightDirectionWS =
                    normalize(
                        mainLight.direction
                    );

                float3 viewDirectionWS =
                    normalize(
                        cameraPositionWS -
                        hitPositionWS
                    );

                float3 halfDirectionWS =
                    normalize(
                        lightDirectionWS +
                        viewDirectionWS
                    );

                float3 reflectDirectionWS =
                    reflect(
                        -viewDirectionWS,
                        normalWS
                    );

                float NdotL =
                    saturate(
                        dot(
                            normalWS,
                            lightDirectionWS
                        )
                    );

                float NdotH =
                    saturate(
                        dot(
                            normalWS,
                            halfDirectionWS
                        )
                    );

                float NdotV =
                    saturate(
                        dot(
                            normalWS,
                            viewDirectionWS
                        )
                    );

                float attenuation =
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                // -----------------------------------------------------
                // Diffuse
                // -----------------------------------------------------

                float diffuse =
                    NdotL *
                    attenuation;

                // -----------------------------------------------------
                // Base specular
                // -----------------------------------------------------

                float baseSpecularPower =
                    lerp(
                        8.0,
                        256.0,
                        _Smoothness *
                        _Smoothness
                    );

                float baseSpecular =
                    pow(
                        NdotH,
                        baseSpecularPower
                    ) *
                    _SpecularStrength *
                    attenuation;

                // -----------------------------------------------------
                // Clear coat
                //
                // Narrow second highlight.
                // -----------------------------------------------------

                float clearCoatPower =
                    lerp(
                        32.0,
                        1024.0,
                        _Smoothness
                    );

                float clearCoatSpecular =
                    pow(
                        NdotH,
                        clearCoatPower
                    ) *
                    _ClearCoatStrength *
                    attenuation;

                // -----------------------------------------------------
                // Fresnel
                // -----------------------------------------------------

                float fresnel =
                    pow(
                        saturate(
                            1.0 -
                            NdotV
                        ),
                        max(
                            _FresnelPower,
                            0.0001
                        )
                    ) *
                    _FresnelStrength;

                // -----------------------------------------------------
                // Environment reflection
                // -----------------------------------------------------

                half perceptualRoughness =
                    saturate(
                        (
                            1.0 -
                            _Smoothness
                        ) +
                        _ReflectionBlur *
                        0.5
                    );

                half3 environmentReflection =
                    GlossyEnvironmentReflection(
                        reflectDirectionWS,
                        perceptualRoughness,
                        1.0h
                    );

                // -----------------------------------------------------
                // Ambient
                // -----------------------------------------------------

                half3 ambient =
                    SampleSH(
                        normalWS
                    ) *
                    _AmbientStrength;

                // -----------------------------------------------------
                // Edge brighten
                // -----------------------------------------------------

                half edgeFactor =
                    saturate(
                        fresnel *
                        _EdgeBrighten
                    );

                half3 edgeTintedAlbedo =
                    lerp(
                        albedo,
                        lerp(
                            albedo,
                            half3(
                                1.0,
                                1.0,
                                1.0
                            ),
                            0.5
                        ),
                        edgeFactor
                    );

                // -----------------------------------------------------
                // Lighting composition
                // -----------------------------------------------------

                half3 diffuseColor =
                    edgeTintedAlbedo *
                    mainLight.color *
                    diffuse;

                half3 ambientColor =
                    edgeTintedAlbedo *
                    ambient;

                half3 baseSpecularColor =
                    _SpecularTint.rgb *
                    mainLight.color *
                    baseSpecular;

                half3 clearCoatColor =
                    _SpecularTint.rgb *
                    mainLight.color *
                    clearCoatSpecular;

                half3 reflectionColor =
                    environmentReflection *
                    (
                        _ReflectionStrength +
                        fresnel
                    );

                half3 finalColor =
                    ambientColor +
                    diffuseColor +
                    baseSpecularColor +
                    clearCoatColor +
                    reflectionColor;

                output.color =
                    half4(
                        finalColor,
                        1.0
                    );

                // -----------------------------------------------------
                // Correct ray-marched depth
                // -----------------------------------------------------

                float4 hitPositionCS =
                    TransformWorldToHClip(
                        hitPositionWS
                    );

                output.depth =
                    hitPositionCS.z /
                    hitPositionCS.w;

                return output;
            }

            ENDHLSL
        }
    }
}