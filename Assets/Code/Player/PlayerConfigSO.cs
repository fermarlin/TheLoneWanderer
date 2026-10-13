using UnityEngine;

//La configuracion del jugador
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Game/Player/Player Config")]
public sealed class PlayerConfigSO : ScriptableObject
{
    [Min(1)] public int vidaMaxima = 3;
    [Min(0f)] public float velocidad = 5f;

    [Min(0f)] public float fuerzaSalto = 10f;
    [Min(0f)] public float fuerzaRebote = 6f;
    [Min(0.01f)] public float longitudRaycast = 0.6f;
    public LayerMask capaSuelo;

    public bool dobleSaltoDesbloqueadoInicialmente = false;
    [Min(0)] public int saltosExtra = 1;
    [Min(0f)] public float multiplicadorDobleSalto = 1f;

    [Range(0f, 1f)] public float multiplicadorJumpCut = 0.5f;

    [Min(0f)] public float gravedadCaida = 2.5f;
    [Min(0f)] public float gravedadSubida = 1f;
    [Min(0.01f)] public float velocidadMaximaCaida = 20f;

    public bool saltoParedDesbloqueadoInicialmente = false;
    [Min(0.01f)] public float longitudRaycastPared = 0.6f;
    [Min(0f)] public float velocidadDeslizamiento = 2f;
    [Min(0f)] public float fuerzaWallJumpX = 8f;
    [Min(0f)] public float fuerzaWallJumpY = 10f;
    [Min(0f)] public float tiempoBloqueoMovimiento = 0.2f;

    public bool dashDesbloqueadoInicialmente = false;

    [Min(0.1f)] public float velocidadDash = 14f;
    [Min(0.02f)] public float duracionDash = 0.18f;
    [Min(0f)] public float tiempoRecargaDash = 0.4f;
    public bool limitarDashAereo = true;

    [Min(0f)]
    public float toleranciaImpulsoVertical = 0.05f;
}
