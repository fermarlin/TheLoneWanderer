using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Este script se encarga de controlar la camara, busca la posicion que esta el player y hace lerp, falta limitar la camara para que la podamos encapsualar en los niveles
public class CamaraController : MonoBehaviour
{
    public Transform target;
    public float cameraSpeed = 0.1f;
    public Vector3 offset = new Vector3(0,4.5f, -10);

    private void FixedUpdate()
    {
        Vector3 wantedPos = target.position + offset;

        Vector3 lerpPos = Vector3.Lerp(transform.position, wantedPos, cameraSpeed);

        transform.position = lerpPos;
    }
}