// Tek shader ile tüm masa çizimi: daire, halka, kapsül, şerit, radyal degrade, parıltı, yuvarlak kutu.
// Şekiller GPU'da mesafe alanı (SDF) olarak keskin çizilir → her çözünürlükte net, doku yok, tek draw call.
Shader "NeonPinball/Shapes"
{
    Properties
    {
        [HideInInspector] _StencilRef ("Stencil Ref", Float) = 0
        [HideInInspector] _StencilComp ("Stencil Comp", Float) = 8
        [HideInInspector] _StencilOp ("Stencil Op", Float) = 0
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Stencil
        {
            Ref [_StencilRef]
            Comp [_StencilComp]
            Pass [_StencilOp]
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float3 pos : POSITION;
            float4 c0 : COLOR;
            float2 uv : TEXCOORD0;
            float4 prm : TEXCOORD1;
            float4 c1 : TEXCOORD2;
            float4 c2 : TEXCOORD3;
            float4 c3 : TEXCOORD4;
            float4 stops : TEXCOORD5;
        };

        struct Varyings
        {
            float4 pos : SV_POSITION;
            float4 c0 : COLOR;
            float2 uv : TEXCOORD0;
            float4 prm : TEXCOORD1;
            float4 c1 : TEXCOORD2;
            float4 c2 : TEXCOORD3;
            float4 c3 : TEXCOORD4;
            float4 stops : TEXCOORD5;
        };

        Varyings Vert(Attributes i)
        {
            Varyings o;
            o.pos = TransformObjectToHClip(i.pos);
            o.c0 = i.c0; o.uv = i.uv; o.prm = i.prm;
            o.c1 = i.c1; o.c2 = i.c2; o.c3 = i.c3; o.stops = i.stops;
            return o;
        }

        float4 Ramp(float t, float4 a, float4 b, float4 c, float4 d, float s1, float s2)
        {
            if (t <= 0) return a;
            if (t < s1) return lerp(a, b, t / max(s1, 1e-4));
            if (t < s2) return lerp(b, c, (t - s1) / max(s2 - s1, 1e-4));
            if (t < 1) return lerp(c, d, (t - s2) / max(1 - s2, 1e-4));
            return d;
        }

        half4 Frag(Varyings i) : SV_Target
        {
            float shape = i.prm.x;
            bool add = shape >= 100;
            if (add) shape -= 100;
            float4 col = i.c0;
            float d = -1;
            bool soft = false;

            if (shape < 0.5)            // 0: dolu daire (r = prm.y)
            {
                d = length(i.uv) - i.prm.y;
            }
            else if (shape < 1.5)       // 1: halka (r = prm.y, kalınlık = prm.z)
            {
                d = abs(length(i.uv) - i.prm.y) - i.prm.z * 0.5;
            }
            else if (shape < 2.5)       // 2: kapsül, uçlarda farklı yarıçap (yarı uzunluk h = prm.y, r0 = prm.z, r1 = prm.w)
            {
                float h = max(i.prm.y, 1e-5);
                float t = saturate((i.uv.x + h) / (2.0 * h));
                float r = lerp(i.prm.z, i.prm.w, t);
                float2 q = i.uv - float2(-h + 2.0 * h * t, 0.0);
                d = length(q) - r;
            }
            else if (shape < 3.5)       // 3: şerit (yarı kalınlık = prm.y; uv.y = merkez çizgisine işaretli uzaklık)
            {
                d = abs(i.uv.y) - i.prm.y;
            }
            else if (shape < 4.5)       // 4: düz dolgu (kenar yumuşatma yok; köşe renkleri enterpole olur)
            {
                soft = true;
            }
            else if (shape < 5.5)       // 5: radyal degrade disk (R = prm.y, odak = prm.zw * R, degrade yarıçapı = R * stops.w)
            {
                float R = max(i.prm.y, 1e-5);
                float2 f = i.prm.zw * R;
                float t = length(i.uv - f) / (R * max(i.stops.w, 1e-3));
                col = Ramp(t, i.c0, i.c1, i.c2, i.c3, i.stops.x, i.stops.y);
                d = length(i.uv) - R;
                if (i.stops.z > 0.5) soft = true;   // kırpma yok (Skia'daki 'clamp' kipi)
            }
            else if (shape < 6.5)       // 6: parıltı (kenara doğru karesel sönüm)
            {
                float t = saturate(length(i.uv) / max(i.prm.y, 1e-5));
                float k = 1.0 - t;
                col.a *= k * k;
                soft = true;
            }
            else                        // 7: yuvarlak kutu (yarı boyut = prm.yz, köşe = prm.w)
            {
                float2 q = abs(i.uv) - i.prm.yz + i.prm.w;
                d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - i.prm.w;
            }

            float cov = 1.0;
            if (!soft)
            {
                float aa = max(fwidth(d), 1e-6);
                cov = saturate(0.5 - d / aa);
            }
            float a = cov * col.a;
            return half4(col.rgb * a, add ? 0.0 : a);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
