using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


//Este script se encarga de controlar la camara, busca la posicion que esta el player y hace lerp, falta limitar la camara para que la podamos encapsualar en los niveles
public class CamaraController : MonoBehaviour
{
    public Transform target;
    public float cameraSpeed = 0.1f;
    public Vector3 offset = new Vector3(0, 4.5f, -10);
    [SerializeField] private Tilemap levelTilemap;

     private Camera cam;
    private Bounds levelBounds;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        CalculateBounds();
        ClampCameraImmediately();
    }

    private void FixedUpdate()
    {
        if (target == null)
            return;

        Vector3 wantedPos = target.position + offset;

        Vector3 lerpPos = Vector3.Lerp(
            transform.position,
            wantedPos,
            cameraSpeed
        );

        if (levelTilemap != null && cam != null && cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            float minX = levelBounds.min.x + halfWidth;
            float maxX = levelBounds.max.x - halfWidth;

            float minY = levelBounds.min.y + halfHeight;
            float maxY = levelBounds.max.y - halfHeight;

            lerpPos.x = minX <= maxX
                ? Mathf.Clamp(lerpPos.x, minX, maxX)
                : levelBounds.center.x;

            lerpPos.y = minY <= maxY
                ? Mathf.Clamp(lerpPos.y, minY, maxY)
                : levelBounds.center.y;
        }

        transform.position = lerpPos;
    }

    private void CalculateBounds()
    {
        if (levelTilemap == null)
            return;

        levelTilemap.CompressBounds();

        BoundsInt cellBounds = levelTilemap.cellBounds;

        Vector3 min = levelTilemap.CellToWorld(cellBounds.min);
        Vector3 max = levelTilemap.CellToWorld(cellBounds.max);

        Vector3 worldMin = Vector3.Min(min, max);
        Vector3 worldMax = Vector3.Max(min, max);

        levelBounds = new Bounds();
        levelBounds.SetMinMax(worldMin, worldMax);
    }

    private void ClampCameraImmediately()
    {
        if (levelTilemap == null || cam == null || !cam.orthographic)
            return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector3 pos = transform.position;

        float minX = levelBounds.min.x + halfWidth;
        float maxX = levelBounds.max.x - halfWidth;

        float minY = levelBounds.min.y + halfHeight;
        float maxY = levelBounds.max.y - halfHeight;

        pos.x = minX <= maxX
            ? Mathf.Clamp(pos.x, minX, maxX)
            : levelBounds.center.x;

        pos.y = minY <= maxY
            ? Mathf.Clamp(pos.y, minY, maxY)
            : levelBounds.center.y;

        transform.position = pos;
    }


}