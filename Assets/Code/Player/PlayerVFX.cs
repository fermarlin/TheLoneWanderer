using UnityEngine;


//He creado este script para no crear cada dos por tres gameobjects de las particulas, basicamente se van reutilizando

[RequireComponent(typeof(Animator))]
public sealed class PlayerVFX : MonoBehaviour
{
    private Transform spawnPoint;

    private Transform leftWallPoint;
    private Transform rightWallPoint;

    public Vector3 offset;

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private Animator animator;
    private bool isInitialized;
    private bool isActivatingEffect;
    private bool isPlaying;

    private void Awake()
    {
        Initialize();
        if (!isActivatingEffect)
            gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalLocalScale = transform.localScale;
        animator = GetComponent<Animator>();
        isInitialized = true;
    }

    public void PlayVFX()
    {
        EndVFX();
        PlayVFX(0);
    }


    public void PlayVFX(int wallSide)
    {
        Initialize();

        if (isPlaying)
            return;

        if (animator == null || originalParent == null)
        {
            Debug.LogWarning("[PlayerVFX] Requiere Animator y un padre inicial.", this);
            return;
        }

        Transform point = ChoosePoint(wallSide);
        Vector3 worldPosition;
        Quaternion worldRotation;

        if (point != null)
        {
            worldPosition = point.TransformPoint(offset);
            worldRotation = point.rotation;
        }
        else
        {
            worldPosition = originalParent.TransformPoint(originalLocalPosition + offset);
            worldRotation = originalParent.rotation * originalLocalRotation;
        }


        transform.SetParent(null, true);
        transform.SetPositionAndRotation(worldPosition, worldRotation);

        isPlaying = true;
        isActivatingEffect = true;
        gameObject.SetActive(true);
        isActivatingEffect = false;

        animator.Rebind();
        animator.Update(0f);
    }

    private Transform ChoosePoint(int wallSide)
    {
        if (wallSide < 0 && leftWallPoint != null)
            return leftWallPoint;

        if (wallSide > 0 && rightWallPoint != null)
            return rightWallPoint;

        return spawnPoint;
    }


    public void EndVFX()
    {
        if (!isPlaying)
            return;

        RestoreHierarchy();
        isPlaying = false;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (isPlaying && !isActivatingEffect)
        {
            RestoreHierarchy();
            isPlaying = false;
        }
    }

    private void RestoreHierarchy()
    {
        if (originalParent == null)
            return;

        transform.SetParent(originalParent, false);
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        transform.localScale = originalLocalScale;
    }
}
