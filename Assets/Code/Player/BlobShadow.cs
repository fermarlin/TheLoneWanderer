
using UnityEngine;

//Este script se encarga de la sombra del jugador, de normal funcionaria al reves
// cuanto mas bajo mas oscuro, pero como nosotros queremos que el jugador sepa donde
// va a caer pues lo he puesto al reves porque en funcion de lo alto que este sera mas opaco
public class BlobShadow : MonoBehaviour
{
    public Transform player;
    public LayerMask groundLayer;

    public float maxDistance = 10f;
    public float groundOffset = -.2f;
    public float maxScaleHeight = 4f;

    [Range(0f, 1f)]
    public float maxOpacity = 0.9f;

    [Range(0f, 1f)]
    public float minOpacity = 0.5f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        Vector2 origin = player.position;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            maxDistance,
            groundLayer
        );

        if (!hit.collider)
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.enabled = true;

        //Esto es para que se mantenga la sombra en el suelo
        transform.position = new Vector3(
            player.position.x,
            hit.point.y + groundOffset,
            transform.position.z
        );

        // Esto es para calcular la posicion de la sombra
        float height = Mathf.Max(
            0f,
            player.position.y - hit.point.y
        );

        float t = Mathf.Clamp01(
            height / Mathf.Max(0.01f, maxScaleHeight)
        );

        //Opacidad de la sombra
        Color color = spriteRenderer.color;

        color.a = Mathf.Lerp(
            minOpacity,
            maxOpacity,
            t
        );

        spriteRenderer.color = color;
    }
}
