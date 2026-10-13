
Shader "Custom/PixelLavaTilemap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _PixelSize ("Pixeles por unidad", Range(8, 256)) = 64
        _Speed ("Velocidad", Range(0, 5)) = 0.6
        _Scale ("Escala Lava", Range(1, 20)) = 6
        _Crust ("Cantidad de Costra", Range(0, 1)) = 0.35
        _Glow ("Brillo", Range(0, 2)) = 0.4

        [Toggle] _UseCustomColor ("Usar color personalizado", Float) = 1
        _LavaColor ("Color principal de lava", Color) = (1, 0.26, 0.015, 1)
        _Darkness ("Oscuridad de la costra", Range(0, 1)) = 0.16
        _Highlight ("Intensidad del color caliente", Range(0, 1)) = 0.75

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"=""
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _PixelSize;
            float _Speed;
            float _Scale;
            float _Crust;
            float _Glow;
            float _UseCustomColor;
            fixed4 _LavaColor;
            float _Darkness;
            float _Highlight;

            fixed4 _Color;
            fixed4 _RendererColor;
            float4 _Flip;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                fixed4 color : COLOR;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 localVertex = v.vertex;
                localVertex.xy *= _Flip.xy;

                float4 worldVertex =
                    mul(unity_ObjectToWorld, localVertex);

                o.vertex = UnityObjectToClipPos(localVertex);

                o.worldPos = worldVertex.xy;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.color = v.color * _Color * _RendererColor;

                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Coordenadas globales.
                // Evita cortes entre tiles.
                float2 uv = floor(
                    i.worldPos * _PixelSize
                ) / _PixelSize;

                float t = _Time.y * _Speed;

                float2 p = uv * _Scale;

                // Ondas de magma entrecruzadas.
                float wave1 = sin(
                    p.x * 2.1 + t * 1.8
                    + sin(p.y * 2.4 + t) * 1.3
                );

                float wave2 = sin(
                    p.y * 3.2 - t * 1.2
                    + sin(p.x * 1.8 - t * 0.7)
                );

                float wave3 = sin(
                    (p.x + p.y) * 2.2 + t * 0.8
                );

                float lava =
                    wave1 * 0.45 +
                    wave2 * 0.35 +
                    wave3 * 0.20;

                // Costra de lava.
                float2 cell = floor(
                    p * 2.0 + float2(t * 0.25, 0)
                );

                float noise = hash21(cell);

                lava -= noise * _Crust * 0.65;

                // Paleta de colores.
                float3 darkRed =
                    float3(0.17, 0.015, 0.015);

                float3 red =
                    float3(0.55, 0.045, 0.012);

                float3 orange =
                    float3(1.00, 0.26, 0.015);

                float3 yellow =
                    float3(1.00, 0.72, 0.09);

                float3 hot =
                    float3(1.00, 0.94, 0.38);

                float3 color;

                if (lava < -0.36)
                    color = darkRed;
                else if (lava < -0.12)
                    color = red;
                else if (lava < 0.20)
                    color = orange;
                else if (lava < 0.48)
                    color = yellow;
                else
                    color = hot;

                // Permite usar cualquier color desde el Inspector.
                // Las zonas oscuras y calientes se derivan del mismo color.
                // Con la opcion desactivada se conserva la paleta clasica.
                if (_UseCustomColor > 0.5)
                {
                    float3 baseColor = _LavaColor.rgb;
                    float3 darkColor = baseColor * _Darkness;
                    float3 crustColor = lerp(darkColor, baseColor, 0.45);
                    float3 warmColor = lerp(baseColor, float3(1, 1, 1), _Highlight * 0.45);
                    float3 hotColor = lerp(baseColor, float3(1, 1, 1), _Highlight);

                    if (lava < -0.36)
                        color = darkColor;
                    else if (lava < -0.12)
                        color = crustColor;
                    else if (lava < 0.20)
                        color = baseColor;
                    else if (lava < 0.48)
                        color = warmColor;
                    else
                        color = hotColor;
                }

                // Brillo de zonas calientes.
                float heat = step(0.20, lava);

                color +=
                    color * heat * _Glow * 0.35;

                // Transparencia original del tile.
                fixed4 sprite = tex2D(
                    _MainTex, i.uv
                );

                float alpha =
                    sprite.a * i.color.a;

                // Color premultiplicado.
                return fixed4(
                    color * i.color.rgb * alpha,
                    alpha
                );
            }

            ENDCG
        }
    }

    Fallback Off
}
