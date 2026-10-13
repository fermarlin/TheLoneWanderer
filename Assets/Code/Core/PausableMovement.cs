using UnityEngine;

//Este Script hay que ponerselo a todas las cosas que se pausen en el juego
public sealed class PausableMovement : MonoBehaviour
{
    //Esto es por si acaso para que no se muevan los personajes
    [SerializeField] private MonoBehaviour[] movementScripts;

    private Rigidbody2D[] rbBodies;
    private bool[] previousSimulated;
    private bool[] previousActiveScripts;
    private GameEventsSO gameEvent;
    private bool paused;

    private void Awake()
    {
        rbBodies = GetComponentsInChildren<Rigidbody2D>(true);

        previousSimulated = new bool[rbBodies.Length];
        if (movementScripts == null)
            movementScripts = new MonoBehaviour[0];
        previousActiveScripts = new bool[movementScripts.Length];
    }

    private void OnEnable()
    {
        Suscribe();
    }


    private void Suscribe()
    {
        if (gameEvent != null)
            return;

        GameOrchestrator orchestrator = GameOrchestrator.Instance;

        if (orchestrator == null)
        {
            //Esto lo he puesto porque muchas veces este script iba mas rapido y me daba error al suscribir porque no existia aun
            return;
        }

        gameEvent = orchestrator.Events;

        gameEvent.OnPauseChanged += ApplyPause;
        ApplyPause(orchestrator.GamePaused);
    }

    private void OnDisable()
    {
        if (gameEvent != null)
            gameEvent.OnPauseChanged -= ApplyPause;
        gameEvent = null;

        // Despausar si he desactivado el objeto
        ApplyPause(false);
    }

    private void ApplyPause(bool isPaused)
    {
        if (paused == isPaused)
            return;

        paused = isPaused;

        if (isPaused)
        {
            for (int i = 0; i < rbBodies.Length; i++)
            {
                Rigidbody2D cuerpo = rbBodies[i];
                if (cuerpo == null)
                    continue;

                previousSimulated[i] = cuerpo.simulated;
                cuerpo.simulated = false; //Guarda su velocidad
            }

            for (int i = 0; i < movementScripts.Length; i++)
            {
                MonoBehaviour script = movementScripts[i];
                if (script == null || script == this || script is GameOrchestrator)
                    continue;

                previousActiveScripts[i] = script.enabled;
                script.enabled = false;//Desactivo los scripts
            }
        }
        else
        {
            for (int i = 0; i < rbBodies.Length; i++)
                if (rbBodies[i] != null)
                    rbBodies[i].simulated = previousSimulated[i];

            for (int i = 0; i < movementScripts.Length; i++)
            {
                MonoBehaviour script = movementScripts[i];
                if (script != null && script != this && !(script is GameOrchestrator))
                    script.enabled = previousActiveScripts[i];
            }
        }
    }
}
