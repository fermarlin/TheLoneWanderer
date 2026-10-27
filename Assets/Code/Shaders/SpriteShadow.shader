Shader "Sprites/Custom/SpriteShadowOptimized"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [HideInInspector] _FlashAmount ("White Flash", Range(0,1)) = 0

        // Contorno exterior: el ancho se mide en pixeles de pantalla.
        [Toggle] _OutlineEnabled ("Outline Enabled", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width (Pixels)", Range(0,5)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        // Las 8 copias desplazadas dibujan el contorno por fuera de la
        // geometria original, incluso con SpriteRenderer en modo Tight.
        // El sprite principal se renderiza DESPUES, tapando el interior.
        CGINCLUDE
        #include "UnitySprites.cginc"

        half _OutlineEnabled;
        half _OutlineWidth;
        fixed4 _OutlineColor;
        // _Cutoff se declara en cada pase de contorno;
        // el surface shader genera su propia declaracion.

        struct OutlineAppData
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
            fixed4 color : COLOR;
        };

        struct OutlineVaryings
        {
            float4 position : SV_POSITION;
            float2 uv : TEXCOORD0;
            fixed4 color : COLOR;
        };

        OutlineVaryings OutlineVertex(OutlineAppData v, float2 direction)
        {
            OutlineVaryings o;
            v.vertex = UnityFlipSprite(v.vertex, _Flip);
            #if defined(PIXELSNAP_ON)
                v.vertex = UnityPixelSnap(v.vertex);
            #endif

            o.position = UnityObjectToClipPos(v.vertex);
            // Desplazamiento constante en pixeles, independiente del zoom.
            o.position.xy += direction * (_OutlineWidth * 2.0 / _ScreenParams.xy) * o.position.w;
            o.uv = v.texcoord;
            o.color = v.color * _Color * _RendererColor;
            return o;
        }

        OutlineVaryings OutlineRight     (OutlineAppData v) { return OutlineVertex(v, float2( 1.0,  0.0)); }
        OutlineVaryings OutlineLeft      (OutlineAppData v) { return OutlineVertex(v, float2(-1.0,  0.0)); }
        OutlineVaryings OutlineUp        (OutlineAppData v) { return OutlineVertex(v, float2( 0.0,  1.0)); }
        OutlineVaryings OutlineDown      (OutlineAppData v) { return OutlineVertex(v, float2( 0.0, -1.0)); }
        OutlineVaryings OutlineUpRight   (OutlineAppData v) { return OutlineVertex(v, float2( 0.7071068,  0.7071068)); }
        OutlineVaryings OutlineUpLeft    (OutlineAppData v) { return OutlineVertex(v, float2(-0.7071068,  0.7071068)); }
        OutlineVaryings OutlineDownRight (OutlineAppData v) { return OutlineVertex(v, float2( 0.7071068, -0.7071068)); }
        OutlineVaryings OutlineDownLeft  (OutlineAppData v) { return OutlineVertex(v, float2(-0.7071068, -0.7071068)); }

        fixed4 OutlineFragmentInternal(OutlineVaryings i, half cutoff) : SV_Target
        {
            clip(_OutlineEnabled - 0.5h);
            clip(_OutlineWidth - 0.001h);

            // Usamos las mismas transparencias y el mismo alpha cutout
            // que el sprite (compatible con ETC1 y sprites en atlas).
            half spriteAlpha = SampleSpriteTexture(i.uv).a * i.color.a;
            clip(spriteAlpha - cutoff);

            half alpha = spriteAlpha * _OutlineColor.a;
            return fixed4(_OutlineColor.rgb * alpha, alpha);
        }
        ENDCG

        // Los pases de contorno son unlit, sin sombras adicionales.
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex OutlineRight
            #pragma fragment OutlineFragmentPass
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            half _Cutoff;
            fixed4 OutlineFragmentPass(OutlineVaryings i) : SV_Target
            {
                return OutlineFragmentInternal(i, _Cutoff);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex OutlineLeft
            #pragma fragment OutlineFragmentPass
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            half _Cutoff;
            fixed4 OutlineFragmentPass(OutlineVaryings i) : SV_Target
            {
                return OutlineFragmentInternal(i, _Cutoff);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex OutlineUp
            #pragma fragment OutlineFragmentPass
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            half _Cutoff;
            fixed4 OutlineFragmentPass(OutlineVaryings i) : SV_Target
            {
                return OutlineFragmentInternal(i, _Cutoff);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex OutlineDown
            #pragma fragment OutlineFragmentPass
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            half _Cutoff;
            fixed4 OutlineFragmentPass(OutlineVaryings i) : SV_Target
            {
                return OutlineFragmentInternal(i, _Cutoff);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex OutlineUpRight
            #pragma fragment OutlineFragmentPass
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            half _Cutoff;
            fixed4 OutlineFragmentPass(OutlineVaryings i) : SV_Target
            {
                return OutlineFragmentInternal(i, _Cutoff);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex OutlineUpLeft
            #pragma fragment OutlineFragmentPass
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            half _Cutoff;
            fixed4 OutlineFragmentPass(OutlineVaryings i) : SV_Target
            {
                return OutlineFragmentInternal(i, _Cutoff);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex OutlineDownRight
            #pragma fragment OutlineFragmentPass
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            half _Cutoff;
            fixed4 OutlineFragmentPass(OutlineVaryings i) : SV_Target
            {
                return OutlineFragmentInternal(i, _Cutoff);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex OutlineDownLeft
            #pragma fragment OutlineFragmentPass
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            half _Cutoff;
            fixed4 OutlineFragmentPass(OutlineVaryings i) : SV_Target
            {
                return OutlineFragmentInternal(i, _Cutoff);
            }
            ENDCG
        }

        // Se mantiene el surface shader original con Lambert,
        // sombra/cutout y flash blanco al recibir dano.
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert alphatest:_Cutoff addshadow nofog nolightmap nodynlightmap keepalpha noinstancing noforwardadd
        #pragma target 3.0
        #pragma multi_compile_local _ PIXELSNAP_ON
        #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

        half _FlashAmount;

        struct Input
        {
            float2 uv_MainTex;
            fixed4 color;
        };

        void vert(inout appdata_full v, out Input o)
        {
            v.vertex = UnityFlipSprite(v.vertex, _Flip);
            #if defined(PIXELSNAP_ON)
                v.vertex = UnityPixelSnap(v.vertex);
            #endif
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color * _Color * _RendererColor;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = SampleSpriteTexture(IN.uv_MainTex) * IN.color;
            half flash = saturate(_FlashAmount);
            o.Albedo = c.rgb * c.a * (1.0h - flash);
            o.Emission = fixed3(1, 1, 1) * c.a * flash;
            o.Alpha = c.a;
        }
        ENDCG
    }

    Fallback "Transparent/Cutout/VertexLit"
}
