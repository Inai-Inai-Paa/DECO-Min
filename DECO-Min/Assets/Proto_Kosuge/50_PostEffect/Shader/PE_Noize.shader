Shader "Hidden/Custom/PE_Noize"
{
    Properties
    {
        _Intensity("Intensity", Range(0, 1)) = 1
        _NoiseScale("Noise Scale", Range(1, 500)) = 120
        _NoiseSpeed("Noise Speed", Range(0, 100)) = 15

        _DistortionStrength("Distortion Strength", Range(0, 5)) = 1
        _WaveStrength("Wave Strength", Range(0, 5)) = 1
        _RowJitterStrength("Row Jitter Strength", Range(0, 5)) = 1
        _BandStrength("Band Strength", Range(0, 5)) = 1

        _RgbShiftStrength("RGB Shift Strength", Range(0, 5)) = 1
        _GrainStrength("Grain Strength", Range(0, 5)) = 0.5
        _ScanlineStrength("Scanline Strength", Range(0, 5)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "PE_Noize"

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // C#側から SetGlobalTexture("_MainTexture", src) で渡される入力画面。
            TEXTURE2D_X(_MainTexture);

            float4 _MainTexture_TexelSize;

            float _Intensity;
            float _NoiseScale;
            float _NoiseSpeed;

            float _DistortionStrength;
            float _WaveStrength;
            float _RowJitterStrength;
            float _BandStrength;

            float _RgbShiftStrength;
            float _GrainStrength;
            float _ScanlineStrength;

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;

                output.positionHCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.uv = GetFullScreenTriangleTexCoord(vertexID);

                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float RowRand(float y, float t, float scale)
            {
                float rowId = floor(y * scale);
                float timeId = floor(t * 20.0);
                return Hash21(float2(rowId, timeId));
            }

            float SmoothNoise1D(float x)
            {
                float i = floor(x);
                float f = frac(x);

                float a = Hash21(float2(i, 17.13));
                float b = Hash21(float2(i + 1.0, 17.13));

                f = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, f);
            }

            float2 ClampScreenUV(float2 uv)
            {
                return clamp(uv, float2(0.001, 0.001), float2(0.999, 0.999));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                float power = saturate(_Intensity);
                float t = _Time.y * _NoiseSpeed;

                // 横ライン単位のズレを作るため、Y座標を段階化する。
                float steppedY = floor(uv.y * 220.0) / 220.0;

                // 画面全体がゆっくり曲がるような波。
                float bigWave =
                    sin(uv.y * 15.0 + t * 0.18) * 0.010;

                // 中くらいの波。
                float midWave =
                    sin(uv.y * 48.0 + t * 0.42) * 0.006;

                // 細かい波。
                float smallWave =
                    sin(uv.y * 120.0 + t * 0.90) * 0.0025;

                float waveMove =
                    (bigWave + midWave + smallWave) * _WaveStrength;

                // 横ラインごとのランダムなズレ。
                float rowNoise = RowRand(steppedY, t, _NoiseScale);
                float rowMove =
                    (rowNoise - 0.5) * 0.014 * _RowJitterStrength;

                // 太い帯だけ大きく横にズラす。
                float bandNoise = RowRand(uv.y, t * 0.10, 12.0);
                float bandMask = step(0.82, bandNoise);
                float bandMove =
                    (bandNoise - 0.5) * 0.045 * bandMask * _BandStrength;

                // 上下方向に少しつながった滑らかな歪み。
                float smoothWarp =
                    (SmoothNoise1D(uv.y * 16.0 + t * 0.05) - 0.5) * 0.018 * _WaveStrength;

                // 最終的な横方向の歪み量。
                float xMove =
                    waveMove +
                    rowMove +
                    bandMove +
                    smoothWarp;

                xMove *= power * _DistortionStrength;

                float2 baseUV = uv;
                baseUV.x += xMove;
                baseUV = ClampScreenUV(baseUV);

                // RGBチャンネルごとに少し違うUVで読む。
                float rgbGap = 0.0025 * power * _RgbShiftStrength;

                float2 uvR = ClampScreenUV(baseUV + float2(rgbGap, 0.0));
                float2 uvG = baseUV;
                float2 uvB = ClampScreenUV(baseUV - float2(rgbGap, 0.0));

                half r = SAMPLE_TEXTURE2D_X(_MainTexture, sampler_LinearClamp, uvR).r;
                half g = SAMPLE_TEXTURE2D_X(_MainTexture, sampler_LinearClamp, uvG).g;
                half b = SAMPLE_TEXTURE2D_X(_MainTexture, sampler_LinearClamp, uvB).b;

                half3 color = half3(r, g, b);

                // 粒ノイズ。
                float grain = Hash21(uv * _ScreenParams.xy + t);
                color += (grain - 0.5) * 0.025 * power * _GrainStrength;

                // 走査線。
                float scan = sin(uv.y * _ScreenParams.y * 3.14159);
                scan = scan * 0.5 + 0.5;
                color *= lerp(1.0, scan, 0.08 * power * _ScanlineStrength);

                color = saturate(color);

                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }
}