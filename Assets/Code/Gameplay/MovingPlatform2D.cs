
using UnityEngine;


//Este script se encarga de hacer el movimiento de la plataforma movil
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class MovingPlatform2D : MonoBehaviour
{
    public enum PathMode
    {
        PingPong,
        Loop,
        Once
    }

    [Header("Movement")]
    [SerializeField] private PathMode pathMode = PathMode.PingPong;

    [SerializeField, Min(0.01f)]
    private float speed = 2f;

    [SerializeField, Min(0f)]
    private float waitTime = 0.5f;

    [Header("Path")]
    [SerializeField]
    private Vector2[] points =
    {
        new Vector2(3f, 0f)
    };

    [Header("Debug")]
    [SerializeField] private bool showPath = true;

    private Rigidbody2D rb;
    private Vector2 startPosition;

    private int targetIndex = 1;
    private int direction = 1;

    private float waitTimer;
    private bool finished;

    public Vector2 PlatformVelocity { get; private set; }

    private int PointCount =>
        (points == null ? 0 : points.Length) + 1;

    private void Reset()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = rb.position;
    }

    private void FixedUpdate()
    {
        PlatformVelocity = Vector2.zero;

        if (PointCount < 2 || finished)
            return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 target = GetPoint(targetIndex, startPosition);

        Vector2 nextPosition = Vector2.MoveTowards(
            rb.position,
            target,
            speed * Time.fixedDeltaTime
        );

        PlatformVelocity =
            (nextPosition - rb.position) / Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);

        if ((nextPosition - target).sqrMagnitude > 0.000001f)
            return;

        waitTimer = waitTime;

        AdvanceTarget();
    }

    private void AdvanceTarget()
    {
        switch (pathMode)
        {
            case PathMode.PingPong:

                if (targetIndex == PointCount - 1)
                    direction = -1;
                else if (targetIndex == 0)
                    direction = 1;

                targetIndex += direction;
                break;

            case PathMode.Loop:

                targetIndex = (targetIndex + 1) % PointCount;
                break;

            case PathMode.Once:

                if (targetIndex >= PointCount - 1)
                    finished = true;
                else
                    targetIndex++;

                break;
        }
    }

    private Vector2 GetPoint(int index, Vector2 origin)
    {
        if (index == 0)
            return origin;

        return origin + points[index - 1];
    }

    private void OnDrawGizmosSelected()
    {
        if (!showPath || points == null)
            return;

        Vector2 origin = Application.isPlaying
            ? startPosition
            : (Vector2)transform.position;

        Vector2 previous = origin;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin, 0.15f);

        for (int i = 1; i < PointCount; i++)
        {
            Vector2 current = GetPoint(i, origin);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(previous, current);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(current, 0.15f);

            previous = current;
        }

        if (pathMode == PathMode.Loop && PointCount > 1)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(previous, origin);
        }
    }
}
