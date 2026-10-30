
using System;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Player")]
    public FPSController playerMovement;

    [Header("Menu")]
    public bool showMenuOnStart = true;

    public string gameTitle = "Œ¡—≈–¬¿“Œ–»ﬂ";
    public string subtitle = "¿—“–ŒÕŒÃ»◊≈— Œ≈ œ–» Àﬁ◊≈Õ»≈";

    [Header("Journal")]
    public string journalSaveKey =
        "Astronomy_PlayerNotes_v1";

    [Header("Appearance")]
    public Color backgroundColor =
        new Color(0.008f, 0.014f, 0.030f);

    public Color panelColor =
        new Color(0.035f, 0.052f, 0.079f);

    public Color accentColor =
        new Color(0.32f, 0.49f, 0.70f);

    public Color textColor =
        new Color(0.93f, 0.96f, 1f);

    [Header("Background Stars")]
    [Range(0, 500)]
    public int starCount = 170;

    private bool menuOpen;
    private bool journalOpen;
    private bool gameStarted;

    private int pressedButton = -1;

    private Texture2D pixel;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle headingStyle;
    private GUIStyle bodyStyle;
    private GUIStyle smallStyle;

    private readonly List<Vector2> stars =
        new List<Vector2>();

    private readonly List<SavedNote> savedNotes =
        new List<SavedNote>();

    private Vector2 journalScroll;

    [Serializable]
    private class SavedNote
    {
        public string id;
        public string title;
        public string content;
    }

    [Serializable]
    private class SavedNotesData
    {
        public List<SavedNote> notes =
            new List<SavedNote>();
    }

    public bool IsMenuOpen => menuOpen;
    public bool HasGameStarted => gameStarted;

    // --------------------------------
    // INITIALIZATION
    // --------------------------------

    void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<FPSController>();
        }

        pixel = Texture2D.whiteTexture;

        GenerateStars();

        if (showMenuOnStart)
        {
            OpenMainMenu();
        }
        else
        {
            StartGame();
        }
    }

    void Update()
    {
        if (!menuOpen)
            return;

        // Keep mouse accessible in the menu.
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;

        if (!Cursor.visible)
            Cursor.visible = true;

        if (journalOpen &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseJournal();
        }
    }

    void GenerateStars()
    {
        stars.Clear();

        System.Random random =
            new System.Random(12345);

        for (int i = 0; i < starCount; i++)
        {
            stars.Add(new Vector2(
                (float)random.NextDouble(),
                (float)random.NextDouble()
            ));
        }
    }

    // --------------------------------
    // MENU ACTIONS
    // --------------------------------

    public void OpenMainMenu()
    {
        menuOpen = true;
        journalOpen = false;
        gameStarted = false;

        pressedButton = -1;

        // Disable movement but keep the player camera.
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        menuOpen = false;
        journalOpen = false;
        gameStarted = true;

        pressedButton = -1;

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log(
            "[MAIN MENU] Game started in the current scene."
        );
    }

    public void OpenJournal()
    {
        LoadJournal();

        journalScroll = Vector2.zero;
        journalOpen = true;
        pressedButton = -1;
    }

    public void CloseJournal()
    {
        journalOpen = false;
        pressedButton = -1;
    }

    public void QuitGame()
    {
        Debug.Log("[MAIN MENU] Quit game.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --------------------------------
    // JOURNAL DATA
    // --------------------------------

    void LoadJournal()
    {
        savedNotes.Clear();

        if (string.IsNullOrEmpty(journalSaveKey))
            return;

        if (!PlayerPrefs.HasKey(journalSaveKey))
            return;

        try
        {
            string json =
                PlayerPrefs.GetString(journalSaveKey);

            SavedNotesData data =
                JsonUtility.FromJson<SavedNotesData>(json);

            if (data == null || data.notes == null)
                return;

            foreach (SavedNote note in data.notes)
            {
                if (note != null)
                    savedNotes.Add(note);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[MAIN MENU] Journal load failed: " +
                exception.Message
            );
        }
    }

    // --------------------------------
    // GUI STYLES
    // --------------------------------

    void SetTextColor(GUIStyle style, Color color)
    {
        style.normal.textColor = color;
        style.hover.textColor = color;
        style.active.textColor = color;
        style.focused.textColor = color;

        style.onNormal.textColor = color;
        style.onHover.textColor = color;
        style.onActive.textColor = color;
        style.onFocused.textColor = color;
    }

    GUIStyle MakeLabel(
        int fontSize,
        bool bold,
        TextAnchor alignment,
        Color color)
    {
        GUIStyle style =
            new GUIStyle(GUI.skin.label);

        style.fontSize = fontSize;

        style.fontStyle = bold
            ? FontStyle.Bold
            : FontStyle.Normal;

        style.alignment = alignment;
        style.wordWrap = true;

        SetTextColor(style, color);

        return style;
    }

    void InitializeGUI()
    {
        if (titleStyle != null &&
            subtitleStyle != null &&
            buttonStyle != null &&
            headingStyle != null &&
            bodyStyle != null &&
            smallStyle != null)
        {
            return;
        }

        titleStyle = MakeLabel(
            44,
            true,
            TextAnchor.MiddleCenter,
            textColor
        );

        subtitleStyle = MakeLabel(
            15,
            false,
            TextAnchor.MiddleCenter,
            new Color(0.6f, 0.73f, 0.88f)
        );

        buttonStyle = MakeLabel(
            20,
            true,
            TextAnchor.MiddleCenter,
            Color.white
        );

        headingStyle = MakeLabel(
            24,
            true,
            TextAnchor.UpperLeft,
            textColor
        );

        bodyStyle = MakeLabel(
            17,
            false,
            TextAnchor.UpperLeft,
            textColor
        );

        smallStyle = MakeLabel(
            14,
            false,
            TextAnchor.MiddleCenter,
            new Color(0.68f, 0.8f, 0.92f)
        );
    }

    // --------------------------------
    // DRAWING HELPERS
    // --------------------------------

    void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;

        GUI.color = color;

        GUI.DrawTexture(rect, pixel);

        GUI.color = previous;
    }

    void DrawLabel(
        Rect rect,
        string content,
        GUIStyle style)
    {
        if (style == null)
            return;

        GUI.Label(rect, content ?? "", style);
    }

    void DrawBackground()
    {
        DrawRect(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height
            ),
            backgroundColor
        );

        foreach (Vector2 star in stars)
        {
            DrawRect(
                new Rect(
                    star.x * Screen.width,
                    star.y * Screen.height,
                    2f,
                    2f
                ),
                new Color(
                    0.75f,
                    0.85f,
                    1f,
                    0.55f
                )
            );
        }
    }

    // --------------------------------
    // BUTTON INPUT
    // --------------------------------

    bool DrawMenuButton(
        Rect rect,
        string label,
        int id,
        bool primary)
    {
        Event e = Event.current;

        bool hovered =
            rect.Contains(e.mousePosition);

        Color color = primary
            ? accentColor
            : new Color(0.09f, 0.14f, 0.21f);

        if (hovered)
        {
            color = Color.Lerp(
                color,
                new Color(0.52f, 0.72f, 0.92f),
                0.4f
            );
        }

        if (pressedButton == id && hovered)
        {
            color = Color.Lerp(
                color,
                Color.white,
                0.14f
            );
        }

        DrawRect(rect, color);

        DrawLabel(rect, label, buttonStyle);

        // Mouse down.
        if (e.type == EventType.MouseDown &&
            e.button == 0 &&
            hovered)
        {
            pressedButton = id;
            e.Use();

            return false;
        }

        // Mouse up.
        if (e.type == EventType.MouseUp &&
            e.button == 0 &&
            pressedButton == id)
        {
            bool clicked = hovered;

            pressedButton = -1;
            e.Use();

            return clicked;
        }

        return false;
    }

    // --------------------------------
    // MAIN MENU SCREEN
    // --------------------------------

    void DrawMainMenu()
    {
        float width = Mathf.Min(
            560f,
            Screen.width - 36f
        );

        float height = Mathf.Min(
            590f,
            Screen.height - 36f
        );

        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        DrawRect(panel, panelColor);

        DrawRect(
            new Rect(
                panel.x + 40f,
                panel.y + 20f,
                panel.width - 80f,
                2f
            ),
            accentColor
        );

        DrawLabel(
            new Rect(
                panel.x + 20f,
                panel.y + 80f,
                panel.width - 40f,
                70f
            ),
            gameTitle,
            titleStyle
        );

        DrawLabel(
            new Rect(
                panel.x + 20f,
                panel.y + 150f,
                panel.width - 40f,
                30f
            ),
            subtitle,
            subtitleStyle
        );

        float buttonWidth = Mathf.Min(
            340f,
            panel.width - 70f
        );

        float buttonX =
            panel.center.x - buttonWidth * 0.5f;

        float buttonY = panel.y + 235f;

        if (DrawMenuButton(
            new Rect(
                buttonX,
                buttonY,
                buttonWidth,
                58f
            ),
            "Õ¿◊¿“‹ »√–”",
            1,
            true))
        {
            StartGame();
        }

        if (DrawMenuButton(
            new Rect(
                buttonX,
                buttonY + 78f,
                buttonWidth,
                58f
            ),
            "∆”–Õ¿À",
            2,
            false))
        {
            OpenJournal();
        }

        if (DrawMenuButton(
            new Rect(
                buttonX,
                buttonY + 156f,
                buttonWidth,
                58f
            ),
            "¬€…“» »« »√–€",
            3,
            false))
        {
            QuitGame();
        }

        DrawLabel(
            new Rect(
                panel.x + 25f,
                panel.yMax - 55f,
                panel.width - 50f,
                30f
            ),
            "»——À≈ƒ”…  ï  Õ¿¡Àﬁƒ¿…  ï  Œ“ –€¬¿…",
            smallStyle
        );
    }

    // --------------------------------
    // JOURNAL SCREEN
    // --------------------------------

    void DrawJournal()
    {
        float width = Mathf.Min(
            900f,
            Screen.width - 40f
        );

        float height = Mathf.Min(
            710f,
            Screen.height - 40f
        );

        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        DrawRect(panel, panelColor);

        DrawLabel(
            new Rect(
                panel.x + 30f,
                panel.y + 22f,
                width - 60f,
                45f
            ),
            "∆”–Õ¿À Õ¿¡Àﬁƒ¿“≈Àﬂ",
            headingStyle
        );

        DrawRect(
            new Rect(
                panel.x + 30f,
                panel.y + 77f,
                width - 60f,
                1f
            ),
            accentColor
        );

        Rect scrollArea = new Rect(
            panel.x + 30f,
            panel.y + 105f,
            width - 60f,
            height - 190f
        );

        float contentWidth =
            Mathf.Max(150f, scrollArea.width - 30f);

        float totalHeight = 25f;

        foreach (SavedNote note in savedNotes)
        {
            if (note == null)
                continue;

            totalHeight += 80f + Mathf.Max(
                35f,
                bodyStyle.CalcHeight(
                    new GUIContent(note.content ?? ""),
                    contentWidth - 25f
                )
            );
        }

        journalScroll = GUI.BeginScrollView(
            scrollArea,
            journalScroll,
            new Rect(
                0f,
                0f,
                contentWidth,
                Mathf.Max(scrollArea.height, totalHeight)
            )
        );

        if (savedNotes.Count == 0)
        {
            DrawLabel(
                new Rect(
                    15f,
                    25f,
                    contentWidth - 25f,
                    100f
                ),
                "œÓÍ‡ ÌÂÚ ÒÓı‡Ì∏ÌÌ˚ı Á‡ÏÂÚÓÍ.\n" +
                "ŒÌË ÔÓˇ‚ˇÚÒˇ Á‰ÂÒ¸ ÔÓÒÎÂ Á‡ÔËÒË ‚ Ë„Â.",
                bodyStyle
            );
        }

        float y = 15f;

        foreach (SavedNote note in savedNotes)
        {
            if (note == null)
                continue;

            string title =
                string.IsNullOrWhiteSpace(note.title)
                    ? "¡ÂÁ Ì‡Á‚‡ÌËˇ"
                    : note.title;

            string content = note.content ?? "";

            float textHeight = Mathf.Max(
                35f,
                bodyStyle.CalcHeight(
                    new GUIContent(content),
                    contentWidth - 25f
                ) + 10f
            );

            DrawLabel(
                new Rect(
                    12f,
                    y,
                    contentWidth - 25f,
                    38f
                ),
                title,
                headingStyle
            );

            y += 43f;

            DrawLabel(
                new Rect(
                    12f,
                    y,
                    contentWidth - 25f,
                    textHeight
                ),
                content,
                bodyStyle
            );

            y += textHeight + 20f;

            DrawRect(
                new Rect(
                    12f,
                    y,
                    contentWidth - 25f,
                    1f
                ),
                new Color(
                    1f, 1f, 1f, 0.15f
                )
            );

            y += 20f;
        }

        GUI.EndScrollView();

        Rect backButton = new Rect(
            panel.center.x - 120f,
            panel.yMax - 65f,
            240f,
            42f
        );

        if (DrawMenuButton(
            backButton,
            "Õ¿«¿ƒ",
            4,
            false))
        {
            CloseJournal();
        }
    }

    // --------------------------------
    // ONGUI
    // --------------------------------

    void OnGUI()
    {
        if (!menuOpen)
            return;

        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        Color previousContent = GUI.contentColor;
        bool previousEnabled = GUI.enabled;

        // Menu draws over gameplay interfaces.
        GUI.depth = -1000;
        GUI.color = Color.white;
        GUI.contentColor = Color.white;
        GUI.enabled = true;

        InitializeGUI();
        DrawBackground();

        if (journalOpen)
            DrawJournal();
        else
            DrawMainMenu();

        GUI.depth = previousDepth;
        GUI.color = previousColor;
        GUI.contentColor = previousContent;
        GUI.enabled = previousEnabled;
    }
}
