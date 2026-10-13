using UnityEngine;

//Esto es para establecer los datos del escudo
[CreateAssetMenu(fileName = "ShieldConfig", menuName = "Game/Player/Shield Config")]
public sealed class ShieldConfigSO : ScriptableObject
{
    [Min(0.1f)] public float velocidadIda = 12f;
    [Min(0.1f)] public float distanciaMaxima = 5f;
    [Min(0.1f)] public float velocidadRegreso = 15f;
    [Min(0.05f)] public float radioRecogida = 0.45f;
    [Min(0.5f)] public float tiempoMaximoVuelo = 6f;

    [Min(0)] public int danio = 2;
    public LayerMask capasEnemigas;
    public bool golpearTambienAlVolver = true;

    public bool girarDuranteVuelo = true;
    public float gradosPorSegundo = 720f;
}
