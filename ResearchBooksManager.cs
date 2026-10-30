
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResearchBookType
{
    Diary,
    Journal
}

public enum DiaryCategory
{
    Planets,
    Telescopes,
    People,
    Constellations,
    Other
}

[Serializable]
public class DiaryRecord
{
    public string id = "";
    public string title = "";

    [TextArea(4, 12)]
    public string description = "";

    public DiaryCategory category = DiaryCategory.Other;
    public Texture2D image;
}

[Serializable]
public class PlayerNote
{
    public string id = "";
    public string title = "";
    public string content = "";
}

[Serializable]
public class PlayerNotesData
{
    public List<PlayerNote> notes = new List<PlayerNote>();
}

public class ResearchBooksManager : MonoBehaviour
{
    public static ResearchBooksManager Instance
    {
        get;
        private set;
    }

    [Header("Player")]
    public FPSController playerMovement;

    [Header("Controls")]
    public KeyCode diaryKey = KeyCode.J;
    public KeyCode journalKey = KeyCode.N;
    public KeyCode closeKey = KeyCode.Escape;

    [Header("Inventory")]
    public bool hasDiary = false;
    public bool hasJournal = false;

    [Header("Quiz")]
    [Tooltip("Diary is unavailable during the final quiz.")]
    public bool quizMode = false;

    [Header("Automatic Research")]
    public bool collectInspectableObjects = true;
    public bool collectConstellations = true;

    [Min(0.2f)]
    public float scanInterval = 0.75f;

    [Header("Diary")]
    public List<DiaryRecord> diaryRecords =
        new List<DiaryRecord>();

    [Header("Notes")]
    public int maxNotes = 100;

    public int maxTitleCharacters = 70;
    public int maxNoteCharacters = 1500;

    public string saveKey = "Astronomy_PlayerNotes_v1";

    [Header("Colors")]
    public Color backgroundColor =
        new Color(0.008f, 0.012f, 0.02f, 1f);

    public Color coverColor =
        new Color(0.075f, 0.065f, 0.065f, 1f);

    public Color paperColor =
        new Color(0.87f, 0.85f, 0.79f, 1f);

    public Color inkColor =
        new Color(0.12f, 0.13f, 0.15f, 1f);

    public Color accentColor =
        new Color(0.45f, 0.35f, 0.28f, 1f);

    private bool bookOpen;
    private ResearchBookType openedBook;

    private float scanTimer;

    private int selectedCategory;
    private int currentPage;

    private Vector2 diaryScroll;
    private Vector2 notesListScroll;
    private Vector2 noteEditorScroll;

    private PlayerNotesData notesData = new PlayerNotesData();

    private int selectedNoteIndex = -1;

    private string editingTitle = "";
    private string editingContent = "";

    private bool noteDirty;
    private string statusMessage = "";

    private Texture2D pixel;

    private GUIStyle titleStyle;
    private GUIStyle headingStyle;
    private GUIStyle textStyle;
    private GUIStyle smallStyle;
    private GUIStyle pageStyle;
    private GUIStyle buttonStyle;
    private GUIStyle fieldStyle;
    private GUIStyle areaStyle;
    private GUIStyle lightStyle;

    private readonly string[] categoryNames =
    {
        "ВСЕ",
        "ПЛАНЕТЫ",
        "ТЕЛЕСКОПЫ",
        "ЛЮДИ",
        "СОЗВЕЗДИЯ",
        "ДРУГОЕ"
    };

    public bool IsBookOpen => bookOpen;

    public bool IsDiaryOpen =>
        bookOpen && openedBook == ResearchBookType.Diary;

    public bool IsJournalOpen =>
        bookOpen && openedBook == ResearchBookType.Journal;

    public int NotesCount =>
        notesData != null && notesData.notes != null
            ? notesData.notes.Count
            : 0;

    // --------------------------------
    // INITIALIZATION
    // --------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "Only one ResearchBooksManager is allowed.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        if (playerMovement == null)
            playerMovement = GetComponent<FPSController>();

        pixel = Texture2D.whiteTexture;

        if (diaryRecords == null)
            diaryRecords = new List<DiaryRecord>();

        LoadNotes();
    }

    void Update()
    {
        ScanResearch();

        if (bookOpen)
        {
            HandleBookInput();
            return;
        }

        if (playerMovement != null &&
            !playerMovement.enabled)
        {
            return;
        }

        if (Input.GetKeyDown(diaryKey))
        {
            if (hasDiary && !quizMode)
                OpenBook(ResearchBookType.Diary);
        }
        else if (Input.GetKeyDown(journalKey))
        {
            if (hasJournal)
                OpenBook(ResearchBookType.Journal);
        }
    }

    // --------------------------------
    // BOOK INVENTORY
    // --------------------------------

    public bool HasBook(ResearchBookType type)
    {
        return type == ResearchBookType.Diary
            ? hasDiary
            : hasJournal;
    }

    public bool PickUpBook(ResearchBookType type)
    {
        if (HasBook(type))
            return false;

        if (type == ResearchBookType.Diary)
            hasDiary = true;
        else
            hasJournal = true;

        Debug.Log("Book acquired: " + type);
        return true;
    }

    public void OpenBook(ResearchBookType type)
    {
        if (bookOpen || !HasBook(type))
            return;

        if (quizMode && type == ResearchBookType.Diary)
            return;

        // Do not interrupt other interaction systems.
        if (playerMovement != null &&
            !playerMovement.enabled)
        {
            return;
        }

        openedBook = type;
        bookOpen = true;

        selectedCategory = 0;
        currentPage = 0;

        diaryScroll = Vector2.zero;

        if (type == ResearchBookType.Journal)
        {
            if (NotesCount > 0)
                SelectNote(0);
            else
                ClearEditor();
        }

        if (playerMovement != null)
            playerMovement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseBook()
    {
        if (!bookOpen)
            return;

        if (openedBook == ResearchBookType.Journal)
            SaveCurrentNote();

        bookOpen = false;

        GUI.FocusControl("");

        if (playerMovement != null)
            playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void HandleBookInput()
    {
        if (Input.GetKeyDown(closeKey))
        {
            CloseBook();
            return;
        }

        if (openedBook == ResearchBookType.Diary)
        {
            if (Input.GetKeyDown(diaryKey))
            {
                CloseBook();
                return;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
                ChangePage(1);

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                ChangePage(-1);
        }

        // Do not close the journal with N.
        // Otherwise typing the letter N into a note
        // could close the journal accidentally.
    }

    // Called by the future final quiz.
    public void SetQuizMode(bool active)
    {
        quizMode = active;

        if (active && IsDiaryOpen)
            CloseBook();
    }

    // --------------------------------
    // AUTOMATIC RESEARCH
    // --------------------------------

    void ScanResearch()
    {
        scanTimer -= Time.deltaTime;

        if (scanTimer > 0f)
            return;

        scanTimer = Mathf.Max(0.2f, scanInterval);

        if (collectInspectableObjects)
            ScanInspectableObjects();

        if (collectConstellations)
            ScanConstellations();
    }

    void ScanInspectableObjects()
    {
        InspectableObject[] objects =
            FindObjectsOfType<InspectableObject>(true);

        foreach (InspectableObject item in objects)
        {
            if (item == null || !item.alreadyStudied)
                continue;

            DiaryEntryMetadata metadata =
                item.GetComponent<DiaryEntryMetadata>();

            if (metadata == null)
            {
                metadata =
                    item.GetComponentInParent<DiaryEntryMetadata>();
            }

            if (metadata != null &&
                !metadata.includeInDiary)
                continue;

            string title = item.itemName;
            string description = item.description;

            DiaryCategory category = DiaryCategory.Other;
            Texture2D image = null;

            if (metadata != null)
            {
                category = metadata.category;
                image = metadata.image;

                if (!string.IsNullOrWhiteSpace(metadata.customTitle))
                    title = metadata.customTitle;

                if (!string.IsNullOrWhiteSpace(
                    metadata.customDescription))
                {
                    description = metadata.customDescription;
                }
            }

            AddOrUpdateRecord(
                "object:" + item.GetInstanceID(),
                title,
                description,
                category,
                image
            );
        }
    }

    void ScanConstellations()
    {
        TelescopeObservation[] telescopes =
            FindObjectsOfType<TelescopeObservation>(true);

        foreach (TelescopeObservation telescope in telescopes)
        {
            if (telescope == null ||
                telescope.constellations == null)
                continue;

            foreach (ConstellationDefinition c
                     in telescope.constellations)
            {
                if (c == null || !c.discovered)
                    continue;

                AddOrUpdateRecord(
                    "constellation:" + c.constellationName,
                    c.constellationName,
                    c.description,
                    DiaryCategory.Constellations
                );
            }
        }
    }

    public void AddOrUpdateRecord(
        string id,
        string title,
        string description,
        DiaryCategory category,
        Texture2D image = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (diaryRecords == null)
            diaryRecords = new List<DiaryRecord>();

        foreach (DiaryRecord record in diaryRecords)
        {
            if (record == null || record.id != id)
                continue;

            record.title = string.IsNullOrWhiteSpace(title)
                ? "Без названия" : title;

            record.description = description ?? "";
            record.category = category;

            if (image != null)
                record.image = image;

            return;
        }

        diaryRecords.Add(new DiaryRecord
        {
            id = id,
            title = string.IsNullOrWhiteSpace(title)
                ? "Без названия" : title,
            description = description ?? "",
            category = category,
            image = image
        });
    }

    List<DiaryRecord> GetFilteredRecords()
    {
        List<DiaryRecord> result =
            new List<DiaryRecord>();

        if (diaryRecords == null)
            return result;

        foreach (DiaryRecord record in diaryRecords)
        {
            if (record == null)
                continue;

            if (selectedCategory == 0 ||
                (int)record.category == selectedCategory - 1)
            {
                result.Add(record);
            }
        }

        return result;
    }

    void ChangeCategory(int category)
    {
        selectedCategory = Mathf.Clamp(
            category, 0, categoryNames.Length - 1
        );

        currentPage = 0;
        diaryScroll = Vector2.zero;
    }

    void ChangePage(int direction)
    {
        int count = GetFilteredRecords().Count;

        currentPage = count > 0
            ? Mathf.Clamp(currentPage + direction, 0, count - 1)
            : 0;

        diaryScroll = Vector2.zero;
    }

    // --------------------------------
    // NOTES: LOAD / SAVE
    // --------------------------------

    void LoadNotes()
    {
        notesData = new PlayerNotesData();

        if (string.IsNullOrEmpty(saveKey))
            return;

        if (!PlayerPrefs.HasKey(saveKey))
            return;

        try
        {
            string json = PlayerPrefs.GetString(saveKey);

            PlayerNotesData loaded =
                JsonUtility.FromJson<PlayerNotesData>(json);

            if (loaded != null && loaded.notes != null)
                notesData = loaded;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Could not load player notes: " +
                exception.Message
            );
        }
    }

    void SaveNotes()
    {
        if (string.IsNullOrEmpty(saveKey))
            return;

        string json = JsonUtility.ToJson(notesData);

        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
    }

    public List<PlayerNote> GetNotes()
    {
        // Return a copy to prevent accidental changes
        // without saving.
        List<PlayerNote> copy = new List<PlayerNote>();

        foreach (PlayerNote note in notesData.notes)
        {
            if (note == null)
                continue;

            copy.Add(new PlayerNote
            {
                id = note.id,
                title = note.title,
                content = note.content
            });
        }

        return copy;
    }

    // --------------------------------
    // NOTES: EDITING
    // --------------------------------

    void ClearEditor()
    {
        selectedNoteIndex = -1;
        editingTitle = "";
        editingContent = "";
        noteDirty = false;

        noteEditorScroll = Vector2.zero;
        statusMessage = "Новая заметка";
    }

    void SelectNote(int index)
    {
        if (index < 0 || index >= NotesCount)
            return;

        if (noteDirty)
            SaveCurrentNote();

        selectedNoteIndex = index;

        PlayerNote note = notesData.notes[index];

        editingTitle = note.title ?? "";
        editingContent = note.content ?? "";

        noteDirty = false;
        noteEditorScroll = Vector2.zero;

        statusMessage = "";
    }

    void CreateNewNote()
    {
        SaveCurrentNote();

        if (NotesCount >= maxNotes)
        {
            statusMessage = "Достигнут лимит заметок.";
            return;
        }

        notesData.notes.Add(new PlayerNote
        {
            id = Guid.NewGuid().ToString("N"),
            title = "Новая заметка",
            content = ""
        });

        selectedNoteIndex = notesData.notes.Count - 1;

        editingTitle = "";
        editingContent = "";

        noteDirty = false;
        noteEditorScroll = Vector2.zero;

        SaveNotes();

        statusMessage = "Новая заметка создана.";
    }

    void SaveCurrentNote()
    {
        if (selectedNoteIndex < 0 ||
            selectedNoteIndex >= NotesCount)
            return;

        PlayerNote note =
            notesData.notes[selectedNoteIndex];

        note.title = string.IsNullOrWhiteSpace(editingTitle)
            ? "Без названия"
            : editingTitle.Trim();

        note.content = editingContent ?? "";

        noteDirty = false;

        SaveNotes();
        statusMessage = "Заметка сохранена.";
    }

    void DeleteCurrentNote()
    {
        if (selectedNoteIndex < 0 ||
            selectedNoteIndex >= NotesCount)
            return;

        notesData.notes.RemoveAt(selectedNoteIndex);

        noteDirty = false;

        SaveNotes();

        if (NotesCount > 0)
        {
            selectedNoteIndex = -1;
            SelectNote(
                Mathf.Min(
                    selectedNoteIndex < 0 ? 0 : selectedNoteIndex,
                    NotesCount - 1
                )
            );
        }
        else
        {
            ClearEditor();
        }

        statusMessage = "Заметка удалена.";
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
        FontStyle fontStyle,
        Color color,
        bool wrap = true)
    {
        GUIStyle result = new GUIStyle(GUI.skin.label);

        result.fontSize = fontSize;
        result.fontStyle = fontStyle;
        result.wordWrap = wrap;

        SetTextColor(result, color);

        return result;
    }

    void InitializeGUI()
    {
        if (titleStyle != null &&
            headingStyle != null &&
            textStyle != null &&
            smallStyle != null &&
            pageStyle != null &&
            buttonStyle != null &&
            fieldStyle != null &&
            areaStyle != null &&
            lightStyle != null)
        {
            return;
        }

        titleStyle = MakeLabel(
            25, FontStyle.Bold, paperColor
        );

        titleStyle.alignment = TextAnchor.MiddleCenter;

        headingStyle = MakeLabel(
            25, FontStyle.Bold, inkColor
        );

        textStyle = MakeLabel(
            18, FontStyle.Normal, inkColor
        );

        smallStyle = MakeLabel(
            14, FontStyle.Normal, inkColor
        );

        pageStyle = MakeLabel(
            15, FontStyle.Normal, inkColor
        );

        pageStyle.alignment = TextAnchor.MiddleCenter;

        lightStyle = MakeLabel(
            17, FontStyle.Normal, paperColor
        );

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 15;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.wordWrap = true;

        SetTextColor(buttonStyle, inkColor);

        fieldStyle = new GUIStyle(GUI.skin.textField);
        fieldStyle.fontSize = 19;
        fieldStyle.wordWrap = false;
        fieldStyle.padding = new RectOffset(10, 10, 8, 8);

        SetTextColor(fieldStyle, inkColor);

        areaStyle = new GUIStyle(GUI.skin.textArea);
        areaStyle.fontSize = 17;
        areaStyle.wordWrap = true;
        areaStyle.padding = new RectOffset(12, 12, 12, 12);

        SetTextColor(areaStyle, inkColor);
    }

    void DrawRect(Rect rect, Color color)
    {
        Color oldColor = GUI.color;

        GUI.color = color;

        GUI.DrawTexture(
            rect,
            pixel != null ? pixel : Texture2D.whiteTexture
        );

        GUI.color = oldColor;
    }

    void DrawLabel(Rect rect, string value, GUIStyle style)
    {
        if (style == null)
            return;

        Color oldColor = GUI.color;
        Color oldContentColor = GUI.contentColor;

        GUI.color = Color.white;
        GUI.contentColor = Color.white;

        GUI.Label(rect, value ?? "", style);

        GUI.color = oldColor;
        GUI.contentColor = oldContentColor;
    }

    // --------------------------------
    // MAIN GUI
    // --------------------------------

    void OnGUI()
    {
        if (!bookOpen)
            return;

        Color oldColor = GUI.color;
        Color oldContentColor = GUI.contentColor;
        Color oldBackgroundColor = GUI.backgroundColor;
        bool oldEnabled = GUI.enabled;

        GUI.color = Color.white;
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        InitializeGUI();

        if (openedBook == ResearchBookType.Diary)
            DrawDiary();
        else
            DrawJournal();

        GUI.color = oldColor;
        GUI.contentColor = oldContentColor;
        GUI.backgroundColor = oldBackgroundColor;
        GUI.enabled = oldEnabled;
    }

    Rect GetBookRect()
    {
        float width = Mathf.Min(
            1100f, Screen.width - 36f
        );

        float height = Mathf.Min(
            760f, Screen.height - 36f
        );

        return new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );
    }

    Rect DrawBookFrame(string title)
    {
        DrawRect(
            new Rect(0f, 0f, Screen.width, Screen.height),
            backgroundColor
        );

        Rect book = GetBookRect();

        DrawRect(book, coverColor);

        Rect page = new Rect(
            book.x + 15f,
            book.y + 65f,
            book.width - 30f,
            book.height - 82f
        );

        DrawRect(page, paperColor);

        DrawLabel(
            new Rect(
                book.x + 20f,
                book.y + 10f,
                book.width - 40f,
                44f
            ),
            title,
            titleStyle
        );

        return page;
    }

    // --------------------------------
    // DIARY UI
    // --------------------------------

    void DrawDiary()
    {
        Rect page =
            DrawBookFrame("ДНЕВНИК ИССЛЕДОВАТЕЛЯ");

        float margin = 30f;

        float tabsWidth = page.width - margin * 2f;
        float tabWidth = tabsWidth / categoryNames.Length;

        for (int i = 0; i < categoryNames.Length; i++)
        {
            Rect tab = new Rect(
                page.x + margin + i * tabWidth + 2f,
                page.y + 20f,
                tabWidth - 4f,
                34f
            );

            if (selectedCategory == i)
            {
                DrawRect(
                    tab,
                    new Color(0.73f, 0.67f, 0.55f, 0.55f)
                );
            }

            if (GUI.Button(tab, categoryNames[i], buttonStyle))
                ChangeCategory(i);
        }

        List<DiaryRecord> filtered =
            GetFilteredRecords();

        float top = page.y + 95f;

        if (filtered.Count == 0)
        {
            DrawLabel(
                new Rect(
                    page.x + margin,
                    top + 30f,
                    page.width - margin * 2f,
                    120f
                ),
                "Пока нет изученных материалов.\n" +
                "Исследуй предметы и найди созвездия.",
                textStyle
            );

            return;
        }

        currentPage = Mathf.Clamp(
            currentPage, 0, filtered.Count - 1
        );

        DiaryRecord record = filtered[currentPage];

        if (record == null)
            return;

        DrawLabel(
            new Rect(
                page.x + margin,
                top,
                page.width - margin * 2f,
                65f
            ),
            string.IsNullOrWhiteSpace(record.title)
                ? "Без названия"
                : record.title,
            headingStyle
        );

        float contentTop = top + 75f;
        float navigationTop = page.yMax - 62f;

        Rect contentView = new Rect(
            page.x + margin,
            contentTop,
            page.width - margin * 2f,
            Mathf.Max(60f, navigationTop - contentTop - 12f)
        );

        string description =
            string.IsNullOrWhiteSpace(record.description)
                ? "Описание отсутствует."
                : record.description;

        float innerWidth = contentView.width - 22f;

        float textHeight = textStyle.CalcHeight(
            new GUIContent(description),
            innerWidth
        ) + 25f;

        float imageHeight =
            record.image != null ? 210f : 0f;

        float totalHeight = Mathf.Max(
            contentView.height,
            textHeight + imageHeight
        );

        diaryScroll = GUI.BeginScrollView(
            contentView,
            diaryScroll,
            new Rect(0f, 0f, innerWidth, totalHeight)
        );

        if (record.image != null)
        {
            GUI.DrawTexture(
                new Rect(
                    Mathf.Max(0f, (innerWidth - 320f) / 2f),
                    0f,
                    Mathf.Min(320f, innerWidth),
                    190f
                ),
                record.image,
                ScaleMode.ScaleToFit
            );
        }

        DrawLabel(
            new Rect(
                0f,
                imageHeight,
                innerWidth,
                textHeight
            ),
            description,
            textStyle
        );

        GUI.EndScrollView();

        bool oldEnabled = GUI.enabled;

        GUI.enabled = currentPage > 0;

        if (GUI.Button(
            new Rect(
                page.x + margin,
                navigationTop,
                140f,
                36f
            ),
            "← Назад",
            buttonStyle
        ))
        {
            ChangePage(-1);
        }

        GUI.enabled = currentPage < filtered.Count - 1;

        if (GUI.Button(
            new Rect(
                page.xMax - margin - 140f,
                navigationTop,
                140f,
                36f
            ),
            "Далее →",
            buttonStyle
        ))
        {
            ChangePage(1);
        }

        GUI.enabled = oldEnabled;

        DrawLabel(
            new Rect(
                page.center.x - 100f,
                navigationTop + 3f,
                200f,
                28f
            ),
            "Запись " + (currentPage + 1) +
            " / " + filtered.Count,
            pageStyle
        );
    }

    // --------------------------------
    // JOURNAL UI
    // --------------------------------

    void DrawJournal()
    {
        Rect page =
            DrawBookFrame("ЖУРНАЛ НАБЛЮДАТЕЛЯ");

        float margin = 24f;
        float sidebarWidth = Mathf.Clamp(
            page.width * 0.29f,
            150f,
            290f
        );

        Rect sidebar = new Rect(
            page.x + margin,
            page.y + margin,
            sidebarWidth,
            page.height - margin * 2f
        );

        Rect editor = new Rect(
            sidebar.xMax + 18f,
            page.y + margin,
            Mathf.Max(
                170f,
                page.xMax - sidebar.xMax - margin - 18f
            ),
            page.height - margin * 2f
        );

        DrawRect(
            sidebar,
            new Color(0.76f, 0.73f, 0.67f)
        );

        DrawNotesList(sidebar);
        DrawNotesEditor(editor);
    }

    void DrawNotesList(Rect rect)
    {
        float padding = 12f;
        float width = rect.width - padding * 2f;

        DrawLabel(
            new Rect(
                rect.x + padding,
                rect.y + 10f,
                width,
                30f
            ),
            "МОИ ЗАМЕТКИ",
            headingStyle
        );

        DrawLabel(
            new Rect(
                rect.x + padding,
                rect.y + 44f,
                width,
                25f
            ),
            "Всего: " + NotesCount,
            smallStyle
        );

        if (GUI.Button(
            new Rect(
                rect.x + padding,
                rect.y + 76f,
                width,
                37f
            ),
            "+ Новая заметка",
            buttonStyle
        ))
        {
            CreateNewNote();
        }

        Rect listRect = new Rect(
            rect.x + padding,
            rect.y + 128f,
            width,
            Mathf.Max(50f, rect.height - 145f)
        );

        float contentHeight = NotesCount * 56f + 10f;

        notesListScroll = GUI.BeginScrollView(
            listRect,
            notesListScroll,
            new Rect(
                0f, 0f,
                Mathf.Max(70f, width - 18f),
                Mathf.Max(listRect.height, contentHeight)
            )
        );

        for (int i = 0; i < NotesCount; i++)
        {
            PlayerNote note = notesData.notes[i];

            if (note == null)
                continue;

            Rect itemRect = new Rect(
                2f,
                5f + i * 56f,
                Mathf.Max(60f, width - 23f),
                48f
            );

            if (selectedNoteIndex == i)
            {
                DrawRect(
                    itemRect,
                    new Color(0.88f, 0.83f, 0.72f)
                );
            }

            string title =
                string.IsNullOrWhiteSpace(note.title)
                    ? "Без названия"
                    : note.title;

            if (GUI.Button(
                itemRect,
                title,
                buttonStyle
            ))
            {
                SelectNote(i);
            }
        }

        GUI.EndScrollView();
    }

    void DrawNotesEditor(Rect rect)
    {
        float padding = 12f;
        float width = rect.width - padding * 2f;

        if (selectedNoteIndex < 0 ||
            selectedNoteIndex >= NotesCount)
        {
            DrawLabel(
                new Rect(
                    rect.x + 20f,
                    rect.y + 50f,
                    width - 20f,
                    130f
                ),
                "Создай новую заметку, чтобы записать " +
                "свои наблюдения, ассоциации и подсказки.",
                textStyle
            );

            return;
        }

        DrawLabel(
            new Rect(
                rect.x + padding,
                rect.y + 10f,
                width,
                28f
            ),
            "ЗАГОЛОВОК",
            smallStyle
        );

        GUI.SetNextControlName("NoteTitle");

        string newTitle = GUI.TextField(
            new Rect(
                rect.x + padding,
                rect.y + 43f,
                width,
                42f
            ),
            editingTitle ?? "",
            Mathf.Max(1, maxTitleCharacters),
            fieldStyle
        );

        if (newTitle != editingTitle)
        {
            editingTitle = newTitle;
            noteDirty = true;
        }

        DrawLabel(
            new Rect(
                rect.x + padding,
                rect.y + 100f,
                width,
                28f
            ),
            "ТЕКСТ ЗАМЕТКИ",
            smallStyle
        );

        float buttonsY = rect.yMax - 55f;

        Rect textAreaRect = new Rect(
            rect.x + padding,
            rect.y + 132f,
            width,
            Mathf.Max(80f, buttonsY - rect.y - 148f)
        );

        // TextArea is interactive; let Unity handle
        // scrolling and caret movement.
        GUI.SetNextControlName("NoteContent");

        string newContent = GUI.TextArea(
            textAreaRect,
            editingContent ?? "",
            Mathf.Max(1, maxNoteCharacters),
            areaStyle
        );

        if (newContent != editingContent)
        {
            editingContent = newContent;
            noteDirty = true;
        }

        int characterCount =
            string.IsNullOrEmpty(editingContent)
                ? 0
                : editingContent.Length;

        DrawLabel(
            new Rect(
                rect.x + padding,
                textAreaRect.yMax + 4f,
                width,
                22f
            ),
            characterCount + " / " +
            maxNoteCharacters + " символов",
            smallStyle
        );

        float buttonWidth =
            Mathf.Max(80f, (width - 10f) * 0.5f);

        if (GUI.Button(
            new Rect(
                rect.x + padding,
                buttonsY,
                buttonWidth,
                38f
            ),
            "СОХРАНИТЬ",
            buttonStyle
        ))
        {
            SaveCurrentNote();
        }

        if (GUI.Button(
            new Rect(
                rect.x + padding + buttonWidth + 10f,
                buttonsY,
                buttonWidth,
                38f
            ),
            "УДАЛИТЬ",
            buttonStyle
        ))
        {
            DeleteCurrentNote();
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            DrawLabel(
                new Rect(
                    rect.x + padding,
                    buttonsY - 34f,
                    width,
                    24f
                ),
                statusMessage +
                (noteDirty ? " • Есть изменения" : ""),
                smallStyle
            );
        }
        else if (noteDirty)
        {
            DrawLabel(
                new Rect(
                    rect.x + padding,
                    buttonsY - 34f,
                    width,
                    24f
                ),
                "Есть несохранённые изменения",
                smallStyle
            );
        }
    }

    // --------------------------------
    // CLEANUP
    // --------------------------------

    void OnDisable()
    {
        if (bookOpen)
            CloseBook();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            if (IsJournalOpen)
                SaveCurrentNote();

            SaveNotes();
        }
    }

    void OnApplicationQuit()
    {
        if (IsJournalOpen)
            SaveCurrentNote();

        SaveNotes();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
