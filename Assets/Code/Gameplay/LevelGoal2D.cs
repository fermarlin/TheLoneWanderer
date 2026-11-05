
using UnityEngine;

public sealed class LevelGoal2D : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        SceneTransitionManager transition =
            SceneTransitionManager.Instance;

        if (transition == null || transition.IsTransitioning)
            return;

        transition.ChangeScene(mainMenuScene);
    }
}
