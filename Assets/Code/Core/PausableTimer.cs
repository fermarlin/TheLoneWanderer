using System.Collections;
using UnityEngine;
//Esto esta para que no se fastidien las corrutinas cuando pausemos el juego, cuando usemos una corrutina hay
// que ponerlo siempre asi  yield return PausableTimer.Wait(3f);
public static class PausableTimer
{
    public static IEnumerator Wait(float seconds)
    {
        float timeElapsed = 0f;

        while (timeElapsed < seconds)
        {
            if (GameOrchestrator.Instance == null ||
                !GameOrchestrator.Instance.GamePaused)
            {
                timeElapsed += Time.deltaTime;
            }

            yield return null;
        }
    }
}
