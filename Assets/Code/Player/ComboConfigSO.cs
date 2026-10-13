using System;
using UnityEngine;


//Este script nos permite configurar los ataques del player
[CreateAssetMenu(fileName = "ComboConfig", menuName = "Game/Player/Combo Config")]
public sealed class ComboConfigSO : ScriptableObject
{
    [Serializable]
    public sealed class Golpe
    {
        [Min(0.05f)] public float duracion = 0.35f;
        [Min(0f)] public float momentoImpacto = 0.12f;
        [Min(0)] public int danio = 1;
    }

    [Header("SUELO - Combo normal (3 golpes)")]
    public Golpe[] golpes =
    {
        new Golpe { duracion = 0.34f, momentoImpacto = 0.12f, danio = 1 },
        new Golpe { duracion = 0.42f, momentoImpacto = 0.16f, danio = 2 },
        new Golpe { duracion = 0.52f, momentoImpacto = 0.20f, danio = 3 }
    };

    [Header("SUELO - Combo corriendo (2 golpes)")]
    public Golpe[] golpesCorriendo =
    {
        new Golpe { duracion = 0.28f, momentoImpacto = 0.11f, danio = 1 },
        new Golpe { duracion = 0.36f, momentoImpacto = 0.14f, danio = 2 }
    };

    [Header("AIRE - Golpe arriba (1 golpe)")]
    public Golpe ataqueAereoArriba =
        new Golpe { duracion = 0.36f, momentoImpacto = 0.13f, danio = 2};

    [Header("AIRE - Golpe abajo (1 golpe)")]
    public Golpe ataqueAereoAbajo =
        new Golpe { duracion = 0.40f, momentoImpacto = 0.17f, danio = 2};

    [Header("AIRE - Pogo al impactar con ataque abajo")]

    [Min(0f)] public float fuerzaPogo = 9f;

    [Header("AIRE - Combo lateral (2 golpes)")]
    public Golpe[] golpesAereosLaterales =
    {
        new Golpe { duracion = 0.30f, momentoImpacto = 0.12f, danio = 1},
        new Golpe { duracion = 0.38f, momentoImpacto = 0.15f, danio = 2}
    };

    [Header("Eleccion de direccion")]
    [Range(0.1f, 1f)] public float umbralMovimientoParaCorrer = 0.65f;

    [Range(0.1f, 1f)] public float umbralApuntadoVertical = 0.55f;

    [Header("Ventanas de combo (tierra y aire)")]
    [Min(0f)] public float ventanaEntreGolpes = 0.45f;
    [Min(0f)] public float anticipacionEntrada = 0.18f;

    [Header("Deteccion de enemigos")]
    public LayerMask capasEnemigas;

    [Header("Hitbox - Suelo")]
    public Vector2 tamanoHitbox = new Vector2(1.1f, 0.9f);
    public Vector2 desplazamientoHitbox = new Vector2(0.8f, 0f);

    [Header("Hitbox - Aire lateral")]
    public Vector2 tamanoHitboxAereoLateral = new Vector2(1.1f, 0.9f);
    public Vector2 desplazamientoHitboxAereoLateral = new Vector2(0.8f, 0f);

    [Header("Hitbox - Aire arriba")]
    public Vector2 tamanoHitboxAereoArriba = new Vector2(0.9f, 1.1f);
    public Vector2 desplazamientoHitboxAereoArriba = new Vector2(0f, 0.85f);

    [Header("Hitbox - Aire abajo")]
    public Vector2 tamanoHitboxAereoAbajo = new Vector2(0.9f, 1.1f);
    public Vector2 desplazamientoHitboxAereoAbajo = new Vector2(0f, -0.85f);
}
