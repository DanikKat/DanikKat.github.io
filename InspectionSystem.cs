
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class InspectionSystem : MonoBehaviour
{
    [Header("Detection")]
    public float inspectDistance = 4f;
    public LayerMask detectionLayers = ~0;
    public KeyCode inspectKey = KeyCode.E;

    [Header("Object Inspection")]
    public float rotationSpeed = 4f;
    public float inspectionDistance = 1.5f;

    [Header("UI")]
    public int fontSize = 20;
    public Color textColor = Color.white;

    [Header("Panel Animation")]
    [Range(1f, 15f)]
    public float panelFadeSpeed = 6f;

    [Range(0f, 100f)]
    public float panelSlideDistance = 35f;

    [Header("Background Blur")]
    public bool enableBlur = true;

    [Range(1f, 16f)]
    public float blurAperture = 1.2f;

    [Range(1f, 15f)]
    public float blurFadeSpeed = 5f;

    private Camera cam;
    private FPSController movement;

    private InspectableObject target;
    private InspectableObject displayedItem;
    private InspectableObject inspectedItem;

    private GameObject preview;
    private Volume blurVolume;
    private VolumeProfile blurProfile;
    private DepthOfField depthOfField;

    private bool inspecting;
    private bool rotating;

    private float panelAlpha = 0f;

    private GUIStyle titleStyle;
    private GUIStyle descriptionStyle;
    private GUIStyle hintStyle;
    private Texture2D panelTexture;

    void Awake()
    {
        cam = GetComponent<Camera>();
        movement = GetComponentInParent<FPSController>();
        CreateBlurVolume();
    }

    void Update()
    {
        if (inspecting)
            HandleInspection();
        else
            DetectObject();

        UpdatePanelAnimation();
        UpdateBlur();
    }

    // -------------------------
    // OBJECT DETECTION
    // -------------------------

    void DetectObject()
    {
        target = null;

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            inspectDistance,
            detectionLayers,
            QueryTriggerInteraction.Ignore))
        {
            target = hit.collider
                .GetComponentInParent<InspectableObject>();
        }

        if (target != null)
            displayedItem = target;

        if (target != null && Input.GetKeyDown(inspectKey))
            BeginInspection(target);
    }

    // -------------------------
    // PANEL ANIMATION
    // -------------------------

    void UpdatePanelAnimation()
    {
        float targetAlpha =
            !inspecting && target != null ? 1f : 0f;

        panelAlpha = Mathf.MoveTowards(
            panelAlpha,
            targetAlpha,
            panelFadeSpeed * Time.deltaTime
        );

        if (panelAlpha <= 0.001f && target == null)
            displayedItem = null;
    }

    // -------------------------
    // BEGIN INSPECTION
    // -------------------------

    void BeginInspection(InspectableObject item)
    {
        inspectedItem = item;
        inspecting = true;
        rotating = false;

        panelAlpha = 0f;
        target = null;
        displayedItem = null;

        if (movement != null)
            movement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CreatePreview(item);
    }

    // -------------------------
    // CREATE PREVIEW
    // -------------------------

    void CreatePreview(InspectableObject item)
    {
        if (preview != null)
            Destroy(preview);

        preview = new GameObject("Inspection Preview");
        preview.transform.SetParent(transform, false);

        preview.transform.localPosition =
            new Vector3(0f, 0f, inspectionDistance);

        preview.transform.localRotation =
            Quaternion.identity;

        preview.transform.localScale = Vector3.one;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(preview.transform, false);

        CopyVisuals(item.transform, visual.transform, true);

        Renderer[] renderers =
            visual.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No mesh found: " + item.name);
            return;
        }

        Bounds bounds = new Bounds();
        bool first = true;

        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;
            Vector3 min = b.min;
            Vector3 max = b.max;

            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z
                        );

                        Vector3 local =
                            preview.transform.InverseTransformPoint(corner);

                        if (first)
                        {
                            bounds = new Bounds(local, Vector3.zero);
                            first = false;
                        }
                        else
                        {
                            bounds.Encapsulate(local);
                        }
                    }
        }

        float maxSize = Mathf.Max(
            bounds.size.x,
            bounds.size.y,
            bounds.size.z,
            0.001f
        );

        float scale =
            0.7f / maxSize * item.previewSize;

        visual.transform.localPosition =
            -bounds.center * scale;

        visual.transform.localScale =
            Vector3.one * scale;
    }

    // -------------------------
    // COPY VISUALS
    // -------------------------

    void CopyVisuals(
        Transform source,
        Transform parent,
        bool isRoot = false)
    {
        GameObject node = new GameObject(source.name);

        node.transform.SetParent(parent, false);

        if (isRoot)
        {
            node.transform.localPosition = Vector3.zero;
            node.transform.localRotation = Quaternion.identity;
            node.transform.localScale = source.lossyScale;
        }
        else
        {
            node.transform.localPosition =
                source.localPosition;

            node.transform.localRotation =
                source.localRotation;

            node.transform.localScale =
                source.localScale;
        }

        MeshFilter filter =
            source.GetComponent<MeshFilter>();

        MeshRenderer renderer =
            source.GetComponent<MeshRenderer>();

        if (filter != null &&
            renderer != null &&
            filter.sharedMesh != null)
        {
            MeshFilter newFilter =
                node.AddComponent<MeshFilter>();

            newFilter.sharedMesh = filter.sharedMesh;

            MeshRenderer newRenderer =
                node.AddComponent<MeshRenderer>();

            newRenderer.sharedMaterials =
                renderer.sharedMaterials;

            newRenderer.shadowCastingMode =
                ShadowCastingMode.Off;

            newRenderer.receiveShadows = false;
        }

        foreach (Transform child in source)
        {
            if (child.name == "__InteractionOutline")
                continue;

            CopyVisuals(child, node.transform);
        }
    }

    // -------------------------
    // ROTATION
    // -------------------------

    void HandleInspection()
    {
        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.F))
        {
            EndInspection();
            return;
        }

        if (Input.GetMouseButtonDown(0))
            rotating = true;

        if (Input.GetMouseButtonUp(0))
            rotating = false;

        if (rotating && preview != null)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Quaternion yaw = Quaternion.AngleAxis(
                -mouseX * rotationSpeed,
                Vector3.up
            );

            Quaternion pitch = Quaternion.AngleAxis(
                mouseY * rotationSpeed,
                Vector3.right
            );

            preview.transform.localRotation =
                yaw * preview.transform.localRotation * pitch;
        }
    }

    // -------------------------
    // END INSPECTION
    // -------------------------

    void EndInspection()
    {
        if (inspectedItem != null)
            inspectedItem.alreadyStudied = true;

        inspecting = false;
        rotating = false;

        inspectedItem = null;
        target = null;
        displayedItem = null;
        panelAlpha = 0f;

        if (preview != null)
        {
            Destroy(preview);
            preview = null;
        }

        if (movement != null)
            movement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // -------------------------
    // BLUR
    // -------------------------

    void CreateBlurVolume()
    {
        GameObject obj = new GameObject("Inspection Blur");

        blurVolume = obj.AddComponent<Volume>();
        blurVolume.isGlobal = true;
        blurVolume.priority = 1000f;

        blurProfile =
            ScriptableObject.CreateInstance<VolumeProfile>();

        blurVolume.sharedProfile = blurProfile;

        depthOfField = blurProfile.Add<DepthOfField>(true);

        depthOfField.mode.Override(
            DepthOfFieldMode.Bokeh
        );

        depthOfField.focusDistance.Override(
            inspectionDistance
        );

        depthOfField.aperture.Override(
            blurAperture
        );

        depthOfField.focalLength.Override(50f);

        blurVolume.weight = 0f;
    }

    void UpdateBlur()
    {
        if (blurVolume == null)
            return;

        float targetWeight =
            inspecting && enableBlur ? 1f : 0f;

        blurVolume.weight = Mathf.MoveTowards(
            blurVolume.weight,
            targetWeight,
            blurFadeSpeed * Time.deltaTime
        );

        if (depthOfField != null)
        {
            depthOfField.aperture.Override(blurAperture);

            depthOfField.focusDistance.Override(
                inspectionDistance
            );
        }
    }

    // -------------------------
    // GUI STYLES
    // -------------------------

    void InitializeStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = fontSize + 5;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = textColor;
        titleStyle.wordWrap = true;

        descriptionStyle = new GUIStyle(GUI.skin.label);
        descriptionStyle.fontSize = fontSize;
        descriptionStyle.normal.textColor = textColor;
        descriptionStyle.wordWrap = true;

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = fontSize;
        hintStyle.alignment = TextAnchor.MiddleRight;
        hintStyle.normal.textColor = textColor;

        panelTexture = new Texture2D(1, 1);

        panelTexture.SetPixel(
            0, 0,
            new Color(0.035f, 0.045f, 0.06f, 0.85f)
        );

        panelTexture.Apply();
    }

    // -------------------------
    // GUI
    // -------------------------

    void OnGUI()
    {
        InitializeStyles();

        if (inspecting)
        {
            DrawInspectionUI();
            return;
        }

        DrawAnimatedPanel();
    }

    void DrawInspectionUI()
    {
        if (inspectedItem != null)
        {
            GUI.Label(
                new Rect(25f, 20f, Screen.width - 50f, 45f),
                inspectedItem.itemName,
                titleStyle
            );
        }

        GUI.Label(
            new Rect(
                20f,
                Screen.height - 55f,
                Screen.width - 40f,
                35f
            ),
            "ЛКМ + мышь - вращать | F / Esc - назад",
            descriptionStyle
        );
    }

    void DrawAnimatedPanel()
    {
        if (panelAlpha <= 0.001f ||
            displayedItem == null)
            return;

        float width = Mathf.Min(
            380f,
            Screen.width - 40f
        );

        float x =
            Screen.width - width - 25f;

        float slide =
            (1f - panelAlpha) * panelSlideDistance;

        x += slide;

        Color previousColor = GUI.color;

        GUI.color = new Color(
            1f, 1f, 1f, panelAlpha
        );

        if (!displayedItem.alreadyStudied)
        {
            DrawStudyHint(x, width);
        }
        else
        {
            DrawDescription(x, width);
        }

        GUI.color = previousColor;
    }

    void DrawStudyHint(float x, float width)
    {
        Rect panelRect = new Rect(
            x,
            Screen.height - 85f,
            width,
            55f
        );

        GUI.DrawTexture(panelRect, panelTexture);

        GUI.Label(
            new Rect(
                x + 15f,
                panelRect.y + 10f,
                width - 30f,
                35f
            ),
            "[E] Изучить",
            hintStyle
        );
    }

    void DrawDescription(float x, float width)
    {
        float padding = 15f;

        float textWidth = width - padding * 2f;

        float titleHeight = titleStyle.CalcHeight(
            new GUIContent(displayedItem.itemName),
            textWidth
        );

        float descriptionHeight =
            descriptionStyle.CalcHeight(
                new GUIContent(displayedItem.description),
                textWidth
            );

        float height =
            padding +
            titleHeight +
            12f +
            descriptionHeight +
            padding;

        float y =
            Screen.height - height - 25f;

        Rect panelRect =
            new Rect(x, y, width, height);

        GUI.DrawTexture(panelRect, panelTexture);

        GUI.Label(
            new Rect(
                x + padding,
                y + padding,
                textWidth,
                titleHeight
            ),
            displayedItem.itemName,
            titleStyle
        );

        GUI.Label(
            new Rect(
                x + padding,
                y + padding + titleHeight + 12f,
                textWidth,
                descriptionHeight
            ),
            displayedItem.description,
            descriptionStyle
        );
    }

    // -------------------------
    // CLEANUP
    // -------------------------

    void OnDisable()
    {
        if (inspecting)
            EndInspection();

        target = null;
        displayedItem = null;
        panelAlpha = 0f;

        if (blurVolume != null)
            blurVolume.weight = 0f;
    }

    void OnDestroy()
    {
        if (preview != null)
            Destroy(preview);

        if (blurVolume != null)
            Destroy(blurVolume.gameObject);

        if (blurProfile != null)
            Destroy(blurProfile);

        if (panelTexture != null)
            Destroy(panelTexture);
    }
}
