
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointLightCulling2D : MonoBehaviour
{
    [Header("Culling")]
    [SerializeField, Min(0.02f)]
    private float checkInterval = 0.15f;

    [SerializeField, Min(0f)]
    private float extraMargin = 1f;

    private Camera targetCamera;

    private readonly List<Light> lights = new();
    private readonly List<bool> originalStates = new();

    private Coroutine cullingCoroutine;
    private bool initialized;

    private void Start()
    {
        targetCamera = GetComponent<Camera>();

        if (!targetCamera.orthographic)
        {
            Debug.LogWarning(
                "PointLightCulling2D necesita una camara ortografica."
            );
            enabled = false;
            return;
        }

        FindPointLights();
        initialized = true;

        cullingCoroutine = StartCoroutine(CullingLoop());
    }

    private void OnEnable()
    {
        if (initialized)
            cullingCoroutine = StartCoroutine(CullingLoop());
    }

    private void OnDisable()
    {
        if (cullingCoroutine != null)
        {
            StopCoroutine(cullingCoroutine);
            cullingCoroutine = null;
        }

        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = originalStates[i];
        }
    }

    private void FindPointLights()
    {
        lights.Clear();
        originalStates.Clear();

        Light[] sceneLights =
            FindObjectsByType<Light>(FindObjectsSortMode.None);

        foreach (Light light in sceneLights)
        {
            if (light.type != LightType.Point)
                continue;

            lights.Add(light);
            originalStates.Add(light.enabled);
        }
    }

    private IEnumerator CullingLoop()
    {
        WaitForSecondsRealtime wait =
            new WaitForSecondsRealtime(checkInterval);

        while (true)
        {
            UpdateLights();
            yield return wait;
        }
    }

    private void UpdateLights()
    {
        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        Transform cameraTransform = targetCamera.transform;

        for (int i = 0; i < lights.Count; i++)
        {
            Light light = lights[i];

            if (light == null || !originalStates[i])
                continue;

            Vector3 relativePosition =
                light.transform.position -
                cameraTransform.position;

            float x = Vector3.Dot(
                relativePosition,
                cameraTransform.right
            );

            float y = Vector3.Dot(
                relativePosition,
                cameraTransform.up
            );

            float range = light.range + extraMargin;

            bool visible =
                Mathf.Abs(x) <= halfWidth + range &&
                Mathf.Abs(y) <= halfHeight + range;

            if (light.enabled != visible)
                light.enabled = visible;
        }
    }
}
