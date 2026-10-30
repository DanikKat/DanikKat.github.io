
using UnityEngine;

public class PanelLightController : MonoBehaviour
{
    [Header("Ceiling Panel")]
    public Transform ceilingPanel;

    [Header("Light")]
    public Light targetLight;

    [Header("Activation")]
    public float openingThreshold = 0.5f;

    [Header("Smooth Lighting")]
    public bool smoothActivation = true;
    public float fadeDuration = 1.5f;
    public float targetIntensity = 3f;

    private Vector3 initialPanelPosition;
    private bool activated;
    private float currentFade;

    void Start()
    {
        if (ceilingPanel != null)
            initialPanelPosition = ceilingPanel.localPosition;

        if (targetLight != null)
        {
            targetLight.intensity = 0f;
            targetLight.enabled = false;
        }
    }

    void Update()
    {
        if (ceilingPanel == null || targetLight == null)
            return;

        if (!activated)
        {
            float distance = Vector3.Distance(
                ceilingPanel.localPosition,
                initialPanelPosition
            );

            if (distance >= openingThreshold)
            {
                activated = true;
                targetLight.enabled = true;
            }
        }

        if (activated)
        {
            if (smoothActivation)
            {
                currentFade += Time.deltaTime /
                    Mathf.Max(0.01f, fadeDuration);

                targetLight.intensity = Mathf.Lerp(
                    0f,
                    targetIntensity,
                    Mathf.Clamp01(currentFade)
                );
            }
            else
            {
                targetLight.intensity = targetIntensity;
            }
        }
    }
}
