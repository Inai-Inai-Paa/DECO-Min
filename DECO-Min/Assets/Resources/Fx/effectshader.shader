Shader "Custom/Effect/Fxshader"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
        [HDR] _Emission ("Emission Color", Color) = (1,1,1,1)

        [Header(Main Texture)]
        _MainTex ("MainTex", 2D) = "white" {}
        _MainScrollX ("Main Scroll X", Float) = 1
        _MainScrollY ("Main Scroll Y", Float) = 0
        _MainRotAngle ("Main Rotation Angle (Deg)", Range(0, 360)) = 0

        [Header(Mask Texture)]
        _MaskTex ("MaskTex", 2D) = "white" {}
        _MaskScrollX ("Mask Scroll X", Float) = 0
        _MaskScrollY ("Mask Scroll Y", Float) = 1
        _MaskRotAngle ("Mask Rotation Angle (Deg)", Range(0, 360)) = 0
        _MaskPow ("Mask Contrast (Power)", Range(0.1, 5.0)) = 1.0
        _MaskMin ("Mask Offset (Cutout)", Range(-1.0, 1.0)) = 0.0

        [Header(Dissolve)]
        _DissolveTex ("Dissolve Noise (2D)", 2D) = "white" {}
        _DissolveScrollX ("Dissolve Scroll X", Float) = 0
        _DissolveScrollY ("Dissolve Scroll Y", Float) = 0
        _DissolveRotAngle ("Dissolve Rotation Angle (Deg)", Range(0, 360)) = 0
        [HDR] _EdgeColor ("Edge Light Color", Color) = (0,0,0,1)
        _EdgeWidth ("Edge Width", Range(0.0, 0.5)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Cull [_Cull]
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            /*
            -------------------------------------------------------
            カスタムデータ割り当て
            - custom1.xy     (メインテクスチャ U, V Offset)
            - custom1.zw     (ディゾルブ U, V Offset)
            - custom2.x      (光度 - Intensity)
            - custom2.y      (ディゾルブ強度 - Dissolve Amount 0~1)
            -------------------------------------------------------
            */

            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _DissolveTex;
            float4 _MainTex_ST;
            float4 _MaskTex_ST;
            float4 _DissolveTex_ST;

            float _MainScrollX;
            float _MainScrollY;
            float _MainRotAngle;
            float _MaskScrollX;
            float _MaskScrollY;
            float _MaskRotAngle;
            float _DissolveScrollX;
            float _DissolveScrollY;
            float _DissolveRotAngle;

            fixed4 _Emission;
            float _MaskPow;
            float _MaskMin;

            fixed4 _EdgeColor;
            float _EdgeWidth;


            float2 RotateUV(float2 uv, float deg)
            {
                float rad = deg * 0.01745329251; // Deg to Rad (PI / 180)
                float s = sin(rad);
                float c = cos(rad);
                float2x2 rotMatrix = float2x2(c, -s, s, c);
                return mul(uv - 0.5, rotMatrix) + 0.5;
            }


            struct appdata
            {
                float4 vertex        : POSITION;
                float3 normal        : NORMAL;
                float4 color         : COLOR;
                float4 uvCustom1a    : TEXCOORD0; // xy: Standard UV, zw: Custom1.xy (Main Offset)
                float4 custom1bAnd2  : TEXCOORD1; // xy: Custom1.zw (Dissolve Offset), zw: Custom2.xy (Intensity, Dissolve Amount)
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                float4 uvMainMask   : TEXCOORD0; // xy: Main, zw: Mask
                float2 uvDissolve   : TEXCOORD1;
                float2 customData   : TEXCOORD2; // x: Intensity, y: Dissolve Amount
                fixed4 particleColor: COLOR0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.particleColor = v.color;

                float2 uv             = v.uvCustom1a.xy;   // Standard UV
                float2 mainOffset     = v.uvCustom1a.zw;   // Custom1.xy -> Main Offset
                float2 dissolveOffset = v.custom1bAnd2.xy; // Custom1.zw -> Dissolve Offset
                float2 customData     = v.custom1bAnd2.zw; // Custom2.xy -> Intensity, Dissolve Amount

                // 光度とディゾルブ強度
                o.customData.x = max(0.0, customData.x);
                o.customData.y = saturate(customData.y);

                // メインテクスチャ UV 計算 (回転 -> スクロール & オフセット)
                float2 uvMainBase = TRANSFORM_TEX(uv, _MainTex);
                uvMainBase = RotateUV(uvMainBase, _MainRotAngle);
                uvMainBase.x += (_MainScrollX * _Time.y) + mainOffset.x;
                uvMainBase.y += (_MainScrollY * _Time.y) + mainOffset.y;

                // マスクテクスチャ UV 計算 (回転 -> スクロール)
                float2 uvMaskBase = TRANSFORM_TEX(uv, _MaskTex);
                uvMaskBase = RotateUV(uvMaskBase, _MaskRotAngle);
                uvMaskBase.x += _MaskScrollX * _Time.y;
                uvMaskBase.y += _MaskScrollY * _Time.y;

                o.uvMainMask = float4(uvMainBase, uvMaskBase);

                // ディゾルブテクスチャ UV 計算 (回転 -> スクロール & オフセット, Custom1.zw)
                float2 uvDissolveBase = TRANSFORM_TEX(uv, _DissolveTex);
                uvDissolveBase = RotateUV(uvDissolveBase, _DissolveRotAngle);
                uvDissolveBase.x += (_DissolveScrollX * _Time.y) + dissolveOffset.x;
                uvDissolveBase.y += (_DissolveScrollY * _Time.y) + dissolveOffset.y;
                o.uvDissolve = uvDissolveBase;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 main = tex2D(_MainTex, i.uvMainMask.xy);
                fixed4 mask = tex2D(_MaskTex, i.uvMainMask.zw);
                float dissolveNoise = tex2D(_DissolveTex, i.uvDissolve).r;

                float intensity = i.customData.x;
                float customDissolve = i.customData.y;

                // マスク
                float maskAlpha = mask.r * main.r;
                maskAlpha = pow(saturate(maskAlpha + _MaskMin), _MaskPow);
                maskAlpha *= i.particleColor.a;

                // ディゾルブ判定
                float dissolveFactor = dissolveNoise - customDissolve;
                float dissolveMask = step(0.0, dissolveFactor);
                maskAlpha *= dissolveMask;

                // ディゾルブが進行している場合のみエッジ発光
                float edgeMask = step(dissolveFactor, _EdgeWidth) * dissolveMask * step(0.0001, customDissolve);


                fixed3 baseColor = main.rgb * _Emission.rgb * maskAlpha * i.particleColor.rgb * intensity;
                fixed3 edgeColor = _EdgeColor.rgb * edgeMask * maskAlpha * intensity;
                fixed3 finalColor = baseColor + edgeColor;

                float finalAlpha = saturate(maskAlpha * intensity);

                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
