
using System.Collections.Generic;
using UnityEngine;

public class ConstellationMagneticBoard : MonoBehaviour
{
    [Header("Player")]
    public Camera playerCamera;
    public FPSController playerMovement;

    [Header("Telescope")]
    public TelescopeObservation telescope;

    [Header("Interaction")]
    public float interactionDistance = 4f;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode exitKey = KeyCode.Escape;

    [Header("Puzzle")]
    public bool requireTelescopeDiscovery = true;
    public bool showReference = true;

    [Range(0.05f, 0.35f)]
    public float shapeTolerance = 0.18f;

    [Range(12f, 48f)]
    public float magnetSize = 28f;

    public float duplicateStarTolerance = 0.0001f;

    [Header("Reference")]
    [Range(0.5f, 1f)]
    public float referenceScale = 0.94f;

    [Header("Colors")]
    public Color boardColor =
        new Color(0.055f, 0.075f, 0.095f);

    public Color magnetColor = Color.white;

    public Color accentColor =
        new Color(0.45f, 0.77f, 1f);

    [Header("Interaction Panel")]
    public float panelFadeSpeed = 6f;
    public float panelSlideDistance = 35f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Dictionary<string, bool> completed =
        new Dictionary<string, bool>();

    private List<Vector2> magnets =
        new List<Vector2>();

    private List<Vector2> targetStars =
        new List<Vector2>();

    private List<Vector2Int> targetConnections =
        new List<Vector2Int>();

    private ConstellationDefinition selected;

    private bool boardOpen;
    private bool lookingAtBoard;

    private int draggingIndex = -1;

    private Vector2 listScroll;
    private float panelAlpha;

    private string notification = "";
    private float notificationTimer;

    private Texture2D pixel;
    private Texture2D panelTexture;
    private Texture2D magnetTexture;

    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle bodyStyle;
    private GUIStyle smallStyle;
    private GUIStyle hintStyle;
    private GUIStyle buttonStyle;

    // ----------------------------------
    // INITIALIZATION
    // ----------------------------------

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerMovement == null &&
            playerCamera != null)
        {
            playerMovement =
                playerCamera.GetComponentInParent<FPSController>();
        }

        pixel = Texture2D.whiteTexture;

        CreateTextures();
    }

    void CreateTextures()
    {
        panelTexture = new Texture2D(1, 1);

        panelTexture.SetPixel(
            0, 0,
            new Color(0.035f, 0.045f, 0.06f, 0.92f)
        );

        panelTexture.Apply();

        int size = 64;

        magnetTexture = new Texture2D(
            size, size, TextureFormat.RGBA32, false
        );

        magnetTexture.filterMode = FilterMode.Bilinear;
        magnetTexture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];

        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;

                float distance = Mathf.Sqrt(
                    dx * dx + dy * dy
                );

                float alpha = 1f - Mathf.SmoothStep(
                    radius - 2f,
                    radius,
                    distance
                );

                pixels[y * size + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        magnetTexture.SetPixels(pixels);
        magnetTexture.Apply();
    }

    // ----------------------------------
    // UPDATE
    // ----------------------------------

    void Update()
    {
        if (boardOpen)
        {
            if (Input.GetKeyDown(exitKey))
            {
                CloseBoard();
                return;
            }
        }
        else
        {
            DetectBoard();
        }

        float target =
            lookingAtBoard && !boardOpen ? 1f : 0f;

        panelAlpha = Mathf.MoveTowards(
            panelAlpha,
            target,
            panelFadeSpeed * Time.deltaTime
        );

        if (notificationTimer > 0f)
            notificationTimer -= Time.deltaTime;
    }

    void DetectBoard()
    {
        lookingAtBoard = false;

        if (playerCamera == null)
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
            lookingAtBoard =
                hit.collider.GetComponentInParent
                    <ConstellationMagneticBoard>() == this;
        }

        if (lookingAtBoard &&
            Input.GetKeyDown(interactKey))
        {
            OpenBoard();
        }
    }

    void OpenBoard()
    {
        if (telescope == null)
        {
            Debug.LogWarning(
                "MagneticBoard: Telescope is missing!",
                this
            );
            return;
        }

        boardOpen = true;
        lookingAtBoard = false;

        draggingIndex = -1;

        if (playerMovement != null)
            playerMovement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (selected == null || !IsUnlocked(selected))
        {
            selected = null;

            foreach (var c in GetConstellations())
            {
                if (IsUnlocked(c))
                {
                    SelectConstellation(c);
                    break;
                }
            }
        }

        if (selected == null)
        {
            Notify(
                "Сначала найди созвездие в телескопе."
            );
        }
    }

    void CloseBoard()
    {
        boardOpen = false;
        lookingAtBoard = false;
        draggingIndex = -1;

        if (playerMovement != null)
            playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ----------------------------------
    // CONSTELLATIONS
    // ----------------------------------

    List<ConstellationDefinition> GetConstellations()
    {
        if (telescope == null ||
            telescope.constellations == null)
        {
            return new List<ConstellationDefinition>();
        }

        return telescope.constellations;
    }

    bool IsUnlocked(ConstellationDefinition c)
    {
        return c != null &&
            (!requireTelescopeDiscovery || c.discovered);
    }

    bool IsCompleted(ConstellationDefinition c)
    {
        return c != null &&
            completed.TryGetValue(
                c.constellationName,
                out bool result
            ) && result;
    }

    void SelectConstellation(ConstellationDefinition c)
    {
        if (!IsUnlocked(c))
            return;

        selected = c;

        magnets.Clear();
        draggingIndex = -1;

        BuildUniqueTarget();

        Notify(
            "Выбрано: " + c.constellationName
        );
    }

    void BuildUniqueTarget()
    {
        targetStars.Clear();
        targetConnections.Clear();

        if (selected == null ||
            selected.stars == null)
            return;

        int[] remap = new int[selected.stars.Count];

        float toleranceSq =
            duplicateStarTolerance * duplicateStarTolerance;

        for (int i = 0; i < selected.stars.Count; i++)
        {
            Vector2 point = selected.stars[i];

            int existing = -1;

            for (int j = 0; j < targetStars.Count; j++)
            {
                if ((targetStars[j] - point).sqrMagnitude
                    <= toleranceSq)
                {
                    existing = j;
                    break;
                }
            }

            if (existing >= 0)
            {
                remap[i] = existing;
            }
            else
            {
                remap[i] = targetStars.Count;
                targetStars.Add(point);
            }
        }

        if (selected.connections == null)
            return;

        foreach (Vector2Int link in selected.connections)
        {
            if (link.x < 0 || link.y < 0 ||
                link.x >= remap.Length ||
                link.y >= remap.Length)
                continue;

            int a = remap[link.x];
            int b = remap[link.y];

            if (a == b)
                continue;

            bool exists = false;

            foreach (Vector2Int old in targetConnections)
            {
                if ((old.x == a && old.y == b) ||
                    (old.x == b && old.y == a))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                targetConnections.Add(
                    new Vector2Int(a, b)
                );
            }
        }
    }

    // ----------------------------------
    // LAYOUT
    // ----------------------------------

    Rect GetBoardRect()
    {
        float sidebarWidth = Mathf.Clamp(
            Screen.width * 0.32f,
            285f, 420f
        );

        return new Rect(
            24f,
            105f,
            Mathf.Max(
                140f,
                Screen.width - sidebarWidth - 76f
            ),
            Mathf.Max(
                150f,
                Screen.height - 155f
            )
        );
    }

    Rect GetSidebarRect()
    {
        Rect board = GetBoardRect();

        float x = board.xMax + 20f;

        return new Rect(
            x,
            105f,
            Mathf.Max(150f, Screen.width - x - 24f),
            Mathf.Max(150f, Screen.height - 140f)
        );
    }

    Vector2 ToUV(Vector2 mouse, Rect rect)
    {
        return new Vector2(
            Mathf.Clamp01(
                (mouse.x - rect.x) / rect.width
            ),
            Mathf.Clamp01(
                (mouse.y - rect.y) / rect.height
            )
        );
    }

    Vector2 ToScreen(Vector2 uv, Rect rect)
    {
        return new Vector2(
            rect.x + uv.x * rect.width,
            rect.y + uv.y * rect.height
        );
    }

    // ----------------------------------
    // ORIGINAL ONGUI MAGNET INPUT
    // ----------------------------------

    void HandleBoardMouse(Rect rect)
    {
        if (selected == null ||
            targetStars.Count == 0)
            return;

        Event e = Event.current;

        if (e == null)
            return;

        Vector2 mouse = e.mousePosition;

        if (e.type == EventType.MouseDown &&
            rect.Contains(mouse))
        {
            int closest = -1;
            float closestDistance = magnetSize;

            for (int i = 0; i < magnets.Count; i++)
            {
                Vector2 point = ToScreen(
                    magnets[i], rect
                );

                float distance =
                    Vector2.Distance(point, mouse);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = i;
                }
            }

            // Right mouse: remove magnet.
            if (e.button == 1)
            {
                if (closest >= 0)
                {
                    magnets.RemoveAt(closest);
                    Notify("Магнит удалён");
                }

                draggingIndex = -1;
                e.Use();
                return;
            }

            // Left mouse: create or select.
            if (e.button == 0)
            {
                if (closest >= 0)
                {
                    draggingIndex = closest;
                }
                else if (magnets.Count < targetStars.Count)
                {
                    magnets.Add(ToUV(mouse, rect));

                    draggingIndex = magnets.Count - 1;

                    if (showDebugInfo)
                    {
                        Debug.Log(
                            "MAGNET CREATED: " +
                            magnets.Count,
                            this
                        );
                    }
                }
                else
                {
                    Notify(
                        "Все магниты размещены."
                    );
                }

                e.Use();
                return;
            }
        }

        if (e.type == EventType.MouseDrag &&
            e.button == 0 &&
            draggingIndex >= 0 &&
            draggingIndex < magnets.Count)
        {
            magnets[draggingIndex] =
                ToUV(mouse, rect);

            e.Use();
            return;
        }

        if (e.rawType == EventType.MouseUp &&
            e.button == 0)
        {
            draggingIndex = -1;
        }
    }

    // ----------------------------------
    // PUZZLE CHECK
    // ----------------------------------

    List<Vector2> Normalize(List<Vector2> points)
    {
        List<Vector2> result = new List<Vector2>();

        if (points == null || points.Count == 0)
            return result;

        Vector2 min = points[0];
        Vector2 max = points[0];

        foreach (Vector2 p in points)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        Vector2 center = (min + max) * 0.5f;

        float scale = Mathf.Max(
            max.x - min.x,
            max.y - min.y,
            0.0001f
        );

        foreach (Vector2 p in points)
            result.Add((p - center) / scale);

        return result;
    }

    bool TryMatch(
        int index,
        List<Vector2> target,
        List<Vector2> actual,
        int[] assigned,
        bool[] visited)
    {
        for (int i = 0; i < actual.Count; i++)
        {
            if (visited[i])
                continue;

            if (Vector2.Distance(
                target[index], actual[i]
            ) > shapeTolerance)
                continue;

            visited[i] = true;

            if (assigned[i] == -1 ||
                TryMatch(
                    assigned[i],
                    target,
                    actual,
                    assigned,
                    visited))
            {
                assigned[i] = index;
                return true;
            }
        }

        return false;
    }

    bool MatchesShape()
    {
        if (magnets.Count == 0 ||
            magnets.Count != targetStars.Count)
            return false;

        List<Vector2> actualPoints =
            new List<Vector2>();

        foreach (Vector2 p in magnets)
        {
            actualPoints.Add(
                new Vector2(p.x, 1f - p.y)
            );
        }

        List<Vector2> target = Normalize(targetStars);
        List<Vector2> actual = Normalize(actualPoints);

        int[] assigned = new int[actual.Count];

        for (int i = 0; i < assigned.Length; i++)
            assigned[i] = -1;

        for (int i = 0; i < target.Count; i++)
        {
            bool[] visited = new bool[actual.Count];

            if (!TryMatch(
                i,
                target,
                actual,
                assigned,
                visited))
                return false;
        }

        return true;
    }

    void CheckPuzzle()
    {
        if (selected == null)
        {
            Notify("Выбери созвездие.");
            return;
        }

        if (magnets.Count != targetStars.Count)
        {
            Notify(
                "Магнитов: " +
                magnets.Count +
                " / " + targetStars.Count
            );
            return;
        }

        if (MatchesShape())
        {
            completed[selected.constellationName] = true;

            Notify(
                "ПРАВИЛЬНО! " +
                selected.constellationName
            );
        }
        else
        {
            Notify(
                "Рисунок пока не совпадает."
            );
        }
    }

    void Notify(string text)
    {
        notification = text;
        notificationTimer = 3f;
    }

    // ----------------------------------
    // GUI STYLES
    // ----------------------------------

    void InitializeGUI()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 22;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;

        bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 16;
        bodyStyle.wordWrap = true;
        bodyStyle.normal.textColor =
            new Color(0.88f, 0.92f, 0.97f);

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 14;
        smallStyle.wordWrap = true;
        smallStyle.normal.textColor =
            new Color(0.72f, 0.8f, 0.9f);

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 20;
        hintStyle.alignment = TextAnchor.MiddleRight;
        hintStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 15;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
    }

    // ----------------------------------
    // DRAW HELPERS
    // ----------------------------------

    void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(rect, pixel);

        GUI.color = previous;
    }

    void DrawPanel(Rect rect)
    {
        GUI.DrawTexture(rect, panelTexture);
    }

    void DrawCircle(
        Vector2 center,
        float diameter,
        Color color)
    {
        Color previous = GUI.color;

        GUI.color = color;

        GUI.DrawTexture(
            new Rect(
                center.x - diameter * 0.5f,
                center.y - diameter * 0.5f,
                diameter,
                diameter
            ),
            magnetTexture
        );

        GUI.color = previous;
    }

    void DrawLine(
        Vector2 a,
        Vector2 b,
        float thickness,
        Color color)
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;

        Vector2 direction = b - a;

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        GUIUtility.RotateAroundPivot(angle, a);

        GUI.color = color;

        GUI.DrawTexture(
            new Rect(
                a.x,
                a.y - thickness * 0.5f,
                direction.magnitude,
                thickness
            ),
            pixel
        );

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    // ----------------------------------
    // REFERENCE
    // ----------------------------------

    void DrawReference(Rect rect)
    {
        if (targetStars.Count == 0)
            return;

        List<Vector2> normalized =
            Normalize(targetStars);

        List<Vector2> points = new List<Vector2>();

        float scale = Mathf.Min(
            rect.width - 30f,
            rect.height - 30f
        ) * referenceScale;

        foreach (Vector2 star in normalized)
        {
            points.Add(
                rect.center + new Vector2(
                    star.x * scale,
                    -star.y * scale
                )
            );
        }

        foreach (Vector2Int link in targetConnections)
        {
            if (link.x < 0 || link.y < 0 ||
                link.x >= points.Count ||
                link.y >= points.Count)
                continue;

            DrawLine(
                points[link.x],
                points[link.y],
                2f,
                accentColor
            );
        }

        foreach (Vector2 point in points)
        {
            DrawCircle(point, 13f, Color.white);
        }
    }

    // ----------------------------------
    // MAGNET RENDERING
    // ----------------------------------

    void DrawMagnets(Rect board)
    {
        foreach (Vector2 uv in magnets)
        {
            Vector2 point = ToScreen(uv, board);

            // Shadow.
            DrawCircle(
                point + new Vector2(3f, 4f),
                magnetSize + 8f,
                new Color(0f, 0f, 0f, 0.55f)
            );

            // Strong white magnet.
            DrawCircle(
                point,
                magnetSize,
                magnetColor
            );

            // Blue central detail.
            DrawCircle(
                point,
                magnetSize * 0.35f,
                accentColor
            );

            // Guaranteed visible center marker.
            // Even if circle texture has a problem,
            // this white square must be rendered.
            DrawRect(
                new Rect(
                    point.x - 3f,
                    point.y - 3f,
                    6f,
                    6f
                ),
                Color.white
            );
        }
    }

    // ----------------------------------
    // ONGUI
    // ----------------------------------

    void OnGUI()
    {
        InitializeGUI();

        if (!boardOpen)
        {
            DrawInteractionHint();
            return;
        }

        DrawBoardScreen();
    }

    void DrawInteractionHint()
    {
        if (panelAlpha <= 0.001f)
            return;

        float width = Mathf.Min(
            380f,
            Screen.width - 40f
        );

        float x =
            Screen.width - width - 25f +
            (1f - panelAlpha) *
            panelSlideDistance;

        Rect panel = new Rect(
            x,
            Screen.height - 85f,
            width,
            55f
        );

        Color old = GUI.color;

        GUI.color = new Color(
            1f, 1f, 1f, panelAlpha
        );

        DrawPanel(panel);

        GUI.Label(
            new Rect(
                x + 15f,
                panel.y + 10f,
                width - 30f,
                35f
            ),
            "[E] Магнитная доска",
            hintStyle
        );

        GUI.color = old;
    }

    void DrawBoardScreen()
    {
        DrawRect(
            new Rect(
                0, 0,
                Screen.width,
                Screen.height
            ),
            new Color(0.008f, 0.012f, 0.02f)
        );

        Rect header = new Rect(
            24f, 18f, 430f, 76f
        );

        DrawPanel(header);

        GUI.Label(
            new Rect(40f, 28f, 390f, 30f),
            "МАГНИТНАЯ ДОСКА",
            titleStyle
        );

        GUI.Label(
            new Rect(40f, 61f, 390f, 25f),
            "Астрономическая мастерская",
            smallStyle
        );

        Rect board = GetBoardRect();

        DrawRect(
            new Rect(
                board.x - 5f,
                board.y - 5f,
                board.width + 10f,
                board.height + 10f
            ),
            new Color(0.17f, 0.21f, 0.24f)
        );

        DrawRect(board, boardColor);

        // Subtle background grid.
        for (float x = board.x; x < board.xMax; x += 50f)
        {
            DrawRect(
                new Rect(x, board.y, 1f, board.height),
                new Color(1f, 1f, 1f, 0.035f)
            );
        }

        for (float y = board.y; y < board.yMax; y += 50f)
        {
            DrawRect(
                new Rect(board.x, y, board.width, 1f),
                new Color(1f, 1f, 1f, 0.035f)
            );
        }

        // Original working input approach.
        HandleBoardMouse(board);

        // Render magnets AFTER board and grid.
        DrawMagnets(board);

        GUI.Label(
            new Rect(
                board.x,
                board.yMax + 11f,
                board.width,
                28f
            ),
            "ЛКМ — поставить / двигать  |  ПКМ — удалить  |  Esc — выйти",
            smallStyle
        );

        DrawSidebar(GetSidebarRect());

        if (showDebugInfo)
        {
            DrawPanel(new Rect(
                board.x + 12f,
                board.y + 12f,
                280f,
                44f
            ));

            GUI.Label(
                new Rect(
                    board.x + 22f,
                    board.y + 20f,
                    260f,
                    30f
                ),
                "DEBUG: magnets = " +
                magnets.Count +
                " / " +
                targetStars.Count,
                bodyStyle
            );
        }

        if (notificationTimer > 0f)
        {
            float width = Mathf.Min(
                520f,
                Screen.width - 50f
            );

            Rect notice = new Rect(
                Screen.width * 0.5f - width * 0.5f,
                28f,
                width,
                52f
            );

            DrawPanel(notice);

            GUI.Label(
                new Rect(
                    notice.x + 15f,
                    notice.y + 11f,
                    width - 30f,
                    30f
                ),
                notification,
                bodyStyle
            );
        }
    }

    // ----------------------------------
    // SIDEBAR
    // ----------------------------------

    void DrawSidebar(Rect rect)
    {
        DrawPanel(rect);

        float x = rect.x + 15f;
        float width = rect.width - 30f;

        GUI.Label(
            new Rect(
                x, rect.y + 12f,
                width, 28f
            ),
            "СОЗВЕЗДИЯ",
            headerStyle
        );

        List<ConstellationDefinition> all =
            GetConstellations();

        int unlocked = 0;
        int solved = 0;

        foreach (var c in all)
        {
            if (IsUnlocked(c))
                unlocked++;

            if (IsCompleted(c))
                solved++;
        }

        GUI.Label(
            new Rect(
                x, rect.y + 42f,
                width, 25f
            ),
            "Доступно: " + unlocked +
            " / " + all.Count +
            "    Собрано: " + solved,
            smallStyle
        );

        float listHeight = rect.height * 0.3f;

        Rect listRect = new Rect(
            x,
            rect.y + 72f,
            width,
            listHeight
        );

        DrawRect(
            listRect,
            new Color(0.018f, 0.028f, 0.04f)
        );

        listScroll = GUI.BeginScrollView(
            listRect,
            listScroll,
            new Rect(
                0f, 0f,
                Mathf.Max(100f, width - 20f),
                all.Count * 39f + 8f
            )
        );

        float itemY = 5f;

        foreach (var c in all)
        {
            if (c == null)
                continue;

            bool canSelect = IsUnlocked(c);

            string label = canSelect
                ? c.constellationName
                : "??? " + c.constellationName;

            if (IsCompleted(c))
                label = "✓ " + c.constellationName;

            GUI.enabled = canSelect;

            if (GUI.Button(
                new Rect(
                    4f,
                    itemY,
                    width - 28f,
                    33f
                ),
                label,
                buttonStyle
            ))
            {
                SelectConstellation(c);
            }

            GUI.enabled = true;

            itemY += 39f;
        }

        GUI.EndScrollView();

        float y = listRect.yMax + 12f;

        if (selected == null)
        {
            GUI.Label(
                new Rect(x, y, width, 80f),
                "Выбери созвездие для начала.",
                bodyStyle
            );

            return;
        }

        GUI.Label(
            new Rect(x, y, width, 28f),
            selected.constellationName,
            headerStyle
        );

        y += 31f;

        GUI.Label(
            new Rect(x, y, width, 26f),
            "Магниты: " + magnets.Count +
            " / " + targetStars.Count,
            bodyStyle
        );

        y += 32f;

        float referenceHeight = Mathf.Max(
            85f,
            rect.yMax - y - 105f
        );

        Rect reference = new Rect(
            x,
            y,
            width,
            referenceHeight
        );

        DrawRect(
            reference,
            new Color(0.014f, 0.023f, 0.04f)
        );

        if (showReference)
            DrawReference(reference);

        y = reference.yMax + 9f;

        if (GUI.Button(
            new Rect(x, y, width, 36f),
            "ПРОВЕРИТЬ",
            buttonStyle
        ))
        {
            CheckPuzzle();
        }

        y += 43f;

        if (GUI.Button(
            new Rect(x, y, width, 34f),
            "Очистить доску",
            buttonStyle
        ))
        {
            magnets.Clear();
            draggingIndex = -1;
        }
    }

    void OnDisable()
    {
        if (boardOpen)
            CloseBoard();
    }

    void OnDestroy()
    {
        if (panelTexture != null)
            Destroy(panelTexture);

        if (magnetTexture != null)
            Destroy(magnetTexture);
    }
}
