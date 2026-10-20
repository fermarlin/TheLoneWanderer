Shader "Custom/2D/SpriteMagicoBuiltIn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tinte general", Color) = (1,1,1,1)
        [HDR] _MagicColor ("Color magico (sustituye verdes)", Color) = (0.25,0.65,1,1)
        _GreenThreshold ("Deteccion de verde", Range(0,0.5)) = 0.09
        _GreenFeather ("Suavidad seleccion", Range(0.005,0.3)) = 0.06
        _FloatHeight ("Altura flotacion (unidades)", Range(0,0.5)) = 0.075
        _FloatSpeed ("Velocidad flotacion", Range(0,10)) = 2
        _PulseSpeed ("Velocidad pulso", Range(0,10)) = 3
        _GlowIntensity ("Luz interior", Range(0,3)) = 0.45
        _ShimmerIntensity ("Ondas magicas", Range(0,3)) = 0.35
        _ShimmerSpeed ("Velocidad ondas", Range(0,10)) = 2.5
        _SparkleDensity ("Densidad destellos", Range(1,50)) = 14
        _SparkleIntensity ("Intensidad destellos", Range(0,4)) = 1
        _SparkleSpeed ("Velocidad destellos", Range(0,12)) = 3
        _AuraSize ("Ancho aura (pixeles)", Range(0,6)) = 2
        _AuraStrength ("Intensidad aura", Range(0,1)) = 0.65
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _MagicColor;
            float _GreenThreshold, _GreenFeather, _FloatHeight, _FloatSpeed;
            float _PulseSpeed, _GlowIntensity, _ShimmerIntensity, _ShimmerSpeed;
            float _SparkleDensity, _SparkleIntensity, _SparkleSpeed;
            float _AuraSize, _AuraStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 localPos : TEXCOORD1;
                fixed4 color : COLOR;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.localPos = v.vertex.xy;
                v.vertex.y += sin(_Time.y * _FloatSpeed) * _FloatHeight;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                float gDiff = tex.g - max(tex.r, tex.b);
                float gMask = smoothstep(_GreenThreshold - _GreenFeather,
                                         _GreenThreshold + _GreenFeather, gDiff);
                float value = max(tex.r, max(tex.g, tex.b));
                float3 rgb = lerp(tex.rgb, _MagicColor.rgb * value, gMask);
                float t = _Time.y;
                float pulse = 0.5 + 0.5 * sin(t * _PulseSpeed);
                float wave = sin((i.localPos.x + i.localPos.y * 0.65) * 11 - t * _ShimmerSpeed);
                float shimmer = pow(saturate(wave * 0.5 + 0.5), 18);

                float2 p = i.localPos * _SparkleDensity;
                float2 cell = floor(p);
                float seed = Hash21(cell);
                float2 starPos = float2(Hash21(cell + 19.7), Hash21(cell + 73.3));
                float2 d = frac(p) - (0.2 + 0.6 * starPos);
                float center = exp(-65 * dot(d, d));
                float armX = (1 - smoothstep(0, 0.045, abs(d.x))) *
                             (1 - smoothstep(0, 0.39, abs(d.y)));
                float armY = (1 - smoothstep(0, 0.045, abs(d.y))) *
                             (1 - smoothstep(0, 0.39, abs(d.x)));
                float twinkle = pow(saturate(0.5 + 0.5 * sin(t * _SparkleSpeed + seed * 6.28318)), 8);
                float sparks = step(0.95, seed) * max(center, max(armX, armY)) * twinkle;
                float lightAmount = gMask * (_GlowIntensity * (0.35 + 0.65 * pulse) +
                                            _ShimmerIntensity * shimmer +
                                            _SparkleIntensity * sparks);
                rgb += _MagicColor.rgb * lightAmount;

                float2 dx = float2(_MainTex_TexelSize.x * _AuraSize, 0);
                float2 dy = float2(0, _MainTex_TexelSize.y * _AuraSize);
                float al = tex2D(_MainTex, i.uv - dx).a;
                float ar = tex2D(_MainTex, i.uv + dx).a;
                float ad = tex2D(_MainTex, i.uv - dy).a;
                float au = tex2D(_MainTex, i.uv + dy).a;
                float outside = saturate(max(max(al, ar), max(ad, au)) - tex.a) * _AuraStrength;
                float edge = tex.a * (1 - min(min(al, ar), min(ad, au))) * _AuraStrength;
                rgb += _MagicColor.rgb * (outside + edge * 0.6);
                return fixed4(rgb * i.color.rgb, saturate(max(tex.a, outside) * i.color.a));
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
