
using System.Collections;
using UnityEngine;

public class LeverMechanism : MonoBehaviour
{
    [Header("Interaction")]
    public Camera playerCamera;
    public float interactionDistance = 4f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("Lever")]
    public Transform leverHandle;
    public Vector3 leverRotation = new Vector3(60, 0, 0);

    [Header("Ceiling Panels")]
    public Transform leftPanel;
    public Transform rightPanel;

    public Vector3 leftPanelMovement =
        new Vector3(-2f, 0f, 0f);

    public Vector3 rightPanelMovement =
        new Vector3(2f, 0f, 0f);

    [Header("Animation")]
    [Min(0.1f)]
    public float animationDuration = 2f;

    [Header("Camera Shake")]
    public CameraShake cameraShake;
    public bool enableShake = true;

    [Range(0f, 0.2f)]
    public float shakeIntensity = 0.035f;

    [Min(0f)]
    public float shakeDuration = 2f;

    [Header("Interaction Panel")]
    public string interactionText = "[E] Взаимодействовать";

    [Range(1f, 15f)]
    public float panelFadeSpeed = 6f;

    [Range(0f, 100f)]
    public float panelSlideDistance = 35f;

    public int fontSize = 20;
    public Color textColor = Color.white;

    private Quaternion initialLeverRotation;
    private Vector3 initialLeftPosition;
    private Vector3 initialRightPosition;

    private bool hasBeenActivated;
    private bool isAnimating;
    private bool playerLookingAtLever;

    private float panelAlpha;

    private GUIStyle hintStyle;
    private Texture2D panelTexture;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (cameraShake == null && playerCamera != null)
            cameraShake =
                playerCamera.GetComponent<CameraShake>();

        if (leverHandle == null ||
            leftPanel == null ||
            rightPanel == null)
        {
            Debug.LogError(
                "Assign lever handle and ceiling panels!",
                this
            );

            enabled = false;
            return;
        }

        initialLeverRotation = leverHandle.localRotation;
        initialLeftPosition = leftPanel.localPosition;
        initialRightPosition = rightPanel.localPosition;
    }

    void Update()
    {
        DetectPlayer();
        UpdatePanelAnimation();
    }

    void DetectPlayer()
    {
        playerLookingAtLever = false;

        if (hasBeenActivated ||
            isAnimating ||
            playerCamera == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            LeverMechanism lever =
                hit.collider.GetComponentInParent<LeverMechanism>();

            if (lever == this)
            {
                playerLookingAtLever = true;

                if (Input.GetKeyDown(interactionKey))
                    ActivateLever();
            }
        }
    }

    void ActivateLever()
    {
        if (hasBeenActivated || isAnimating)
            return;

        hasBeenActivated = true;
        playerLookingAtLever = false;

        if (enableShake && cameraShake != null)
        {
            cameraShake.Shake(
                shakeDuration,
                shakeIntensity
            );
        }

        StartCoroutine(AnimateMechanism());
    }

    IEnumerator AnimateMechanism()
    {
        isAnimating = true;

        Quaternion startRotation =
            leverHandle.localRotation;

        Quaternion endRotation =
            initialLeverRotation *
            Quaternion.Euler(leverRotation);

        Vector3 startLeft = leftPanel.localPosition;
        Vector3 startRight = rightPanel.localPosition;

        Vector3 endLeft =
            initialLeftPosition + leftPanelMovement;

        Vector3 endRight =
            initialRightPosition + rightPanelMovement;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / animationDuration
            );

            t = t * t * (3f - 2f * t);

            leverHandle.localRotation =
                Quaternion.Slerp(
                    startRotation, endRotation, t
                );

            leftPanel.localPosition =
                Vector3.Lerp(startLeft, endLeft, t);

            rightPanel.localPosition =
                Vector3.Lerp(startRight, endRight, t);

            yield return null;
        }

        leverHandle.localRotation = endRotation;
        leftPanel.localPosition = endLeft;
        rightPanel.localPosition = endRight;

        isAnimating = false;
    }

    void UpdatePanelAnimation()
    {
        float target =
            playerLookingAtLever &&
            !hasBeenActivated ? 1f : 0f;

        panelAlpha = Mathf.MoveTowards(
            panelAlpha,
            target,
            panelFadeSpeed * Time.deltaTime
        );
    }

    void InitializeGUI()
    {
        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = fontSize;
            hintStyle.alignment = TextAnchor.MiddleRight;
            hintStyle.normal.textColor = textColor;
        }

        if (panelTexture == null)
        {
            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(
                0, 0,
                new Color(0.035f, 0.045f, 0.06f, 0.85f)
            );
            panelTexture.Apply();
        }
    }

    void OnGUI()
    {
        if (panelAlpha <= 0.001f)
            return;

        InitializeGUI();

        float width = Mathf.Min(
            380f,
            Screen.width - 40f
        );

        float x =
            Screen.width - width - 25f +
            (1f - panelAlpha) * panelSlideDistance;

        Rect panel = new Rect(
            x,
            Screen.height - 85f,
            width,
            55f
        );

        Color previousColor = GUI.color;

        GUI.color = new Color(
            1f, 1f, 1f, panelAlpha
        );

        GUI.DrawTexture(panel, panelTexture);

        GUI.Label(
            new Rect(
                x + 15f,
                panel.y + 10f,
                width - 30f,
                35f
            ),
            interactionText,
            hintStyle
        );

        GUI.color = previousColor;
    }

    void OnDestroy()
    {
        if (panelTexture != null)
            Destroy(panelTexture);
    }
}
