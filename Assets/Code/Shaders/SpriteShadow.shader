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

        CGPROGRAM
        // Vertex lights: one forward base pass rather than an additive pass per point light.
        // Note: sprite mesh tessellation determines lighting quality.
        #pragma surface surf Lambert vertex:vert alphatest:_Cutoff addshadow nofog nolightmap nodynlightmap keepalpha noinstancing noforwardadd
        #pragma target 3.0
        #pragma multi_compile_local _ PIXELSNAP_ON
        #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
        #include "UnitySprites.cginc"

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
            // Mantenemos la iluminacion Lambert y las sombras habituales.
            // Al recibir dano, sustituimos temporalmente el color por blanco
            // con Emission, para que tambien se vea blanco en zonas oscuras.
            half flash = saturate(_FlashAmount);
            o.Albedo = c.rgb * c.a * (1.0h - flash);
            o.Emission = fixed3(1, 1, 1) * c.a * flash;
            o.Alpha = c.a;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/VertexLit"
}
