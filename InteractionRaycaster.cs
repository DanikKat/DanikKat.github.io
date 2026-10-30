
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class InteractionRaycaster : MonoBehaviour
{
    [Header("Interaction")]
    public float interactionDistance = 4f;

    public LayerMask interactionLayers = ~0;

    [Header("Controls")]
    public KeyCode interactKey = KeyCode.E;

    private Camera cam;
    private InteractableOutline currentTarget;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        InteractableOutline nextTarget = null;

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit hit;

        if (Physics.Raycast(
            ray,
            out hit,
            interactionDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore))
        {
            nextTarget =
                hit.collider.GetComponentInParent<InteractableOutline>();
        }

        if (currentTarget != nextTarget)
        {
            if (currentTarget != null)
                currentTarget.SetHighlighted(false);

            currentTarget = nextTarget;

            if (currentTarget != null)
                currentTarget.SetHighlighted(true);
        }

        if (currentTarget != null &&
            Input.GetKeyDown(interactKey))
        {
            currentTarget.Interact();
        }
    }

    void OnDisable()
    {
        if (currentTarget != null)
        {
            currentTarget.SetHighlighted(false);
            currentTarget = null;
        }
    }
}
