
using UnityEngine;

public class ResearchBookPickup : MonoBehaviour
{
    [Header("Book")]
    public ResearchBookType bookType =
        ResearchBookType.Diary;

    [Header("Interaction")]
    public Camera playerCamera;
    public float interactionDistance = 4f;
    public KeyCode pickupKey = KeyCode.E;

    [Header("Hint Animation")]
    [Range(1f, 15f)]
    public float panelFadeSpeed = 6f;

    [Range(0f, 100f)]
    public float panelSlideDistance = 35f;

    private float panelAlpha;
    private bool lookingAtBook;
    private bool pickedUp;

    private GUIStyle hintStyle;
    private Texture2D panelTexture;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (ResearchBooksManager.Instance != null &&
            ResearchBooksManager.Instance.HasBook(bookType))
        {
            pickedUp = true;
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        lookingAtBook = false;

        if (pickedUp)
            return;

        ResearchBooksManager manager =
            ResearchBooksManager.Instance;

        if (manager == null || playerCamera == null)
            return;

        if (manager.playerMovement != null &&
            !manager.playerMovement.enabled)
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
            ResearchBookPickup book =
                hit.collider
                    .GetComponentInParent<ResearchBookPickup>();

            lookingAtBook = book == this;
        }

        if (lookingAtBook && Input.GetKeyDown(pickupKey))
        {
            PickUp();
        }

        float targetAlpha =
            lookingAtBook ? 1f : 0f;

        panelAlpha = Mathf.MoveTowards(
            panelAlpha,
            targetAlpha,
            panelFadeSpeed * Time.deltaTime
        );
    }

    void PickUp()
    {
        ResearchBooksManager manager =
            ResearchBooksManager.Instance;

        if (manager == null)
            return;

        if (!manager.PickUpBook(bookType))
            return;

        pickedUp = true;

        Debug.Log(
            bookType == ResearchBookType.Diary
                ? "Дневник подобран! Клавиша J."
                : "Журнал подобран! Клавиша N."
        );

        gameObject.SetActive(false);
    }

    void InitializeGUI()
    {
        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 20;
            hintStyle.alignment = TextAnchor.MiddleRight;
            hintStyle.normal.textColor = Color.white;
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
        if (panelAlpha <= 0.001f || pickedUp)
            return;

        InitializeGUI();

        float width = Mathf.Min(
            380f, Screen.width - 40f
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

        Color previous = GUI.color;

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
            bookType == ResearchBookType.Diary
                ? "[E] Подобрать дневник"
                : "[E] Подобрать журнал",
            hintStyle
        );

        GUI.color = previous;
    }

    void OnDestroy()
    {
        if (panelTexture != null)
            Destroy(panelTexture);
    }
}
