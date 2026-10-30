
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ConstellationDefinition
{
    public string constellationName = "Созвездие";

    [TextArea(2, 5)]
    public string description = "";

    public Vector2 skyPosition = new Vector2(0.5f, 0.5f);

    public List<Vector2> stars = new List<Vector2>();
    public List<Vector2Int> connections = new List<Vector2Int>();

    [HideInInspector]
    public bool discovered;
}

public class TelescopeObservation : MonoBehaviour
{
    [Header("Player")]
    public Camera playerCamera;
    public FPSController playerMovement;

    [Header("Interaction")]
    public float interactionDistance = 4f;
    public KeyCode useKey = KeyCode.E;
    public KeyCode observeKey = KeyCode.F;
    public KeyCode exitKey = KeyCode.Escape;

    [Header("Ceiling")]
    public Transform leftCeilingPanel;
    public float openingThreshold = 0.5f;

    [Header("Telescope")]
    [Range(1f, 12f)]
    public float zoom = 3.5f;

    public float minZoom = 1.8f;
    public float maxZoom = 9f;
    public float zoomSpeed = 1.25f;
    public float panSpeed = 0.0018f;

    [Range(200f, 900f)]
    public float scopeRadius = 430f;

    [Range(20f, 160f)]
    public float discoveryRadius = 65f;

    [Header("Sky Distribution")]
    [Range(0.08f, 0.22f)]
    public float minimumConstellationSpacing = 0.16f;

    [Range(0, 99999)]
    public int skySeed = 24817;

    [Range(0.05f, 1f)]
    public float constellationPatternScale = 0.4f;

    [Header("Sky Appearance")]
    [Range(100, 1500)]
    public int backgroundStarCount = 850;

    [Range(0f, 1f)]
    public float starGlow = 0.6f;

    [Range(0f, 1f)]
    public float constellationLineAlpha = 0.65f;

    [Header("UI Animation")]
    [Range(1f, 15f)]
    public float panelFadeSpeed = 6f;

    [Range(0f, 100f)]
    public float panelSlideDistance = 35f;

    [Header("Constellations")]
    public List<ConstellationDefinition> constellations =
        new List<ConstellationDefinition>();

    private Camera cam;
    private Vector3 closedPanelPosition;

    private bool observing;
    private bool lookingAtTelescope;

    private float panelAlpha;
    private float messageTimer;
    private string message = "";

    private Vector2 viewCenter = new Vector2(0.5f, 0.5f);

    private Texture2D pixel;
    private Texture2D panelTexture;
    private Texture2D skyTexture;
    private Texture2D circleMask;

    private GUIStyle titleStyle;
    private GUIStyle infoStyle;
    private GUIStyle hintStyle;
    private GUIStyle smallStyle;

    private readonly List<Vector2> backgroundStars =
        new List<Vector2>();

    // --------------------------------
    // CONSTELLATION CREATION
    // --------------------------------

    void Reset()
    {
        FillOctoberConstellations();
    }

    [ContextMenu("Rebuild Realistic October Constellations")]
    public void FillOctoberConstellations()
    {
        // Preserve discovered states by name.
        HashSet<string> discoveredNames =
            new HashSet<string>();

        if (constellations != null)
        {
            foreach (var c in constellations)
            {
                if (c != null && c.discovered)
                    discoveredNames.Add(c.constellationName);
            }
        }

        constellations = new List<ConstellationDefinition>();

        // All points use a local coordinate system.
        // Lines are explicit pairs of star indices.

        // URSA MAJOR: Big Dipper
        AddFigure("Большая Медведица",
            "Большой Ковш — семь ярких звёзд, помогающих найти Полярную звезду.",
            new int[] {
                -5,3, -3,2, -1,1, 1,0,
                5,1, 5,-3, 1,-3
            },
            new int[] {
                0,1, 1,2, 2,3,
                3,4, 4,5, 5,6, 6,3
            });

        // URSA MINOR
        AddFigure("Малая Медведица",
            "Малый Ковш. На конце его ручки находится Полярная звезда.",
            new int[] {
                -5,-3, -3,-1, -1,1, 1,3,
                5,3, 5,0, 1,0
            },
            new int[] {
                0,1, 1,2, 2,3,
                3,4, 4,5, 5,6, 6,3
            });

        // CASSIOPEIA: W
        AddFigure("Кассиопея",
            "Пять ярких звёзд образуют узнаваемую фигуру W.",
            new int[] {
                -5,3, -3,-2, 0,2, 3,-3, 5,2
            },
            new int[] {
                0,1, 1,2, 2,3, 3,4
            });

        // CEPHEUS: house
        AddFigure("Цефей",
            "Фигура напоминает домик с пятиугольной крышей.",
            new int[] {
                -4,-4, -4,1, 0,5, 4,1, 4,-4
            },
            new int[] {
                0,1, 1,2, 2,3, 3,4, 4,0
            });

        // DRACO: winding chain
        AddFigure("Дракон",
            "Длинное извилистое созвездие около Малой Медведицы.",
            new int[] {
                -5,3, -3,4, -1,2, 0,0,
                3,1, 5,-1, 4,-4, 2,-4,
                1,-2, 3,-1
            },
            new int[] {
                0,1, 1,2, 2,3, 3,4,
                4,5, 5,6, 6,7, 7,8,
                8,9
            });

        // PEGASUS: Great Square and extension
        AddFigure("Пегас",
            "Большой квадрат Пегаса — характерная фигура осеннего неба.",
            new int[] {
                -3,4, 3,4, 3,-2, -3,-2,
                -5,0, 5,-4
            },
            new int[] {
                0,1, 1,2, 2,3, 3,0,
                3,4, 2,5
            });

        // ANDROMEDA
        AddFigure("Андромеда",
            "Цепочка ярких звёзд, рядом с которой расположена галактика M31.",
            new int[] {
                -5,-2, -2,0, 1,1, 4,4,
                0,-2, 3,-4, -1,4
            },
            new int[] {
                0,1, 1,2, 2,3,
                2,4, 4,5, 1,6
            });

        // PERSEUS
        AddFigure("Персей",
            "Разветвлённая цепочка звёзд с известной переменной звездой Алголь.",
            new int[] {
                -3,5, -1,2, 1,0, 3,-2,
                5,-4, -2,-2, -4,-4, 3,3
            },
            new int[] {
                0,1, 1,2, 2,3, 3,4,
                2,5, 5,6, 2,7
            });

        // LACERTA
        AddFigure("Ящерица",
            "Небольшая зигзагообразная цепочка тусклых звёзд.",
            new int[] {
                -2,5, 0,3, -1,1, 2,-1,
                -1,-3, 1,-5
            },
            new int[] {
                0,1, 1,2, 2,3, 3,4, 4,5
            });

        // TRIANGULUM
        AddFigure("Треугольник",
            "Три яркие звезды образуют вытянутый треугольник.",
            new int[] {
                -5,-2, 2,4, 5,-3
            },
            new int[] {
                0,1, 1,2, 2,0
            });

        // CYGNUS: Northern Cross
        AddFigure("Лебедь",
            "Северный Крест — фигура в созвездии Лебедя. Денеб находится на его конце.",
            new int[] {
                0,5, 0,2, 0,0, 0,-5,
                -5,1, 5,1, -2,-2, 3,-2
            },
            new int[] {
                0,1, 1,2, 2,3,
                4,2, 2,5, 2,6, 2,7
            });

        // LYRA
        AddFigure("Лира",
            "Вега — ярчайшая звезда Лиры. Остальные основные звёзды образуют параллелограмм.",
            new int[] {
                -4,5, -1,2, 3,2, 4,-3, -1,-4
            },
            new int[] {
                0,1, 1,2, 2,3, 3,4, 4,1
            });

        // AQUILA
        AddFigure("Орёл",
            "Альтаир находится в центральной части созвездия Орла.",
            new int[] {
                0,5, 0,2, 0,0, 0,-4,
                -5,1, 5,1, -3,-2, 3,-3
            },
            new int[] {
                0,1, 1,2, 2,3,
                4,2, 2,5, 4,6, 5,7
            });

        // DELPHINUS
        AddFigure("Дельфин",
            "Небольшой ромб с цепочкой звёзд, напоминающий дельфина.",
            new int[] {
                -3,2, 0,4, 3,1, 0,-2,
                -3,2, -4,-5
            },
            new int[] {
                0,1, 1,2, 2,3,
                3,4, 4,0, 3,5
            });

        // HERCULES
        AddFigure("Геркулес",
            "Центральная фигура — характерный четырёхугольник.",
            new int[] {
                -2,3, 2,4, 3,0, -1,-1,
                -5,5, 5,5, -4,-5, 4,-5
            },
            new int[] {
                0,1, 1,2, 2,3, 3,0,
                0,4, 1,5, 3,6, 2,7
            });

        // PISCES
        AddFigure("Рыбы",
            "Две группы звёзд связаны длинной извилистой цепочкой.",
            new int[] {
                -5,3, -3,5, -1,3, -3,1,
                -5,3, -1,0, 1,-1, 3,-3,
                5,-1, 4,2, 2,1, 3,-3
            },
            new int[] {
                0,1, 1,2, 2,3, 3,4,
                3,5, 5,6, 6,7,
                7,8, 8,9, 9,10, 10,11
            });

        // AQUARIUS
        AddFigure("Водолей",
            "Большое зодиакальное созвездие с фигурой кувшина и расходящимися цепочками.",
            new int[] {
                -2,4, 0,5, 2,3, 0,1,
                -4,0, -5,-4, 2,-1, 4,-4,
                0,-5
            },
            new int[] {
                0,1, 1,2, 2,3, 3,0,
                3,4, 4,5, 3,6, 6,7, 6,8
            });

        // CAPRICORNUS
        AddFigure("Козерог",
            "Звёзды образуют широкую вытянутую фигуру, похожую на треугольник.",
            new int[] {
                -5,2, 0,4, 5,2, 3,-3,
                0,-5, -3,-2, -5,2
            },
            new int[] {
                0,1, 1,2, 2,3,
                3,4, 4,5, 5,6
            });

        // CETUS
        AddFigure("Кит",
            "Крупное осеннее созвездие с головой и длинным телом.",
            new int[] {
                -5,2, -3,4, 0,3, 1,0,
                4,-1, 5,-4, 2,-4, -1,-2,
                -3,-3, -5,2
            },
            new int[] {
                0,1, 1,2, 2,3, 3,4,
                4,5, 5,6, 6,7, 7,8, 8,9
            });

        // ARIES
        AddFigure("Овен",
            "Небольшая изогнутая цепочка из нескольких ярких звёзд.",
            new int[] {
                -5,-2, -2,1, 1,3, 4,2, 5,0
            },
            new int[] {
                0,1, 1,2, 2,3, 3,4
            });

        // TAURUS: V of Hyades and horns
        AddFigure("Телец",
            "Гиады образуют V-образную фигуру; Альдебаран — яркая звезда поблизости.",
            new int[] {
                -4,4, -1,2, 0,0, -2,-3,
                2,2, 5,4, 3,-3, 0,0
            },
            new int[] {
                0,1, 1,2, 2,3,
                2,4, 4,5, 2,6
            });

        // ORION: shoulders, belt, legs
        AddFigure("Орион",
            "Три звезды пояса, Бетельгейзе, Беллатрикс, Ригель и Саиф образуют узнаваемую фигуру.",
            new int[] {
                -4,5, 4,4,
                -2,1, 0,0, 2,-1,
                -4,-5, 4,-5,
                0,-3
            },
            new int[] {
                0,2, 2,3, 3,4, 4,1,
                2,5, 4,6, 3,7
            });

        // AURIGA
        AddFigure("Возничий",
            "Пятиугольная фигура с яркой звездой Капеллой.",
            new int[] {
                -3,2, 0,5, 4,3,
                4,-2, 0,-5, -4,-2
            },
            new int[] {
                0,1, 1,2, 2,3,
                3,4, 4,5, 5,0
            });

        // GEMINI
        AddFigure("Близнецы",
            "Две почти параллельные звёздные цепочки начинаются от Кастора и Поллукса.",
            new int[] {
                -3,5, 2,5,
                -3,2, 2,2,
                -4,-1, 3,-1,
                -5,-5, 4,-5,
                -1,-3
            },
            new int[] {
                0,2, 2,4, 4,6,
                1,3, 3,5, 5,7,
                2,3, 4,8
            });

        // CORONA BOREALIS
        AddFigure("Северная Корона",
            "Полукруглая дуга ярких звёзд с Альфеккой.",
            new int[] {
                -5,-3, -4,0, -2,3,
                0,4, 2,3, 4,0, 5,-3
            },
            new int[] {
                0,1, 1,2, 2,3,
                3,4, 4,5, 5,6
            });

        DistributeConstellations();

        foreach (var c in constellations)
        {
            c.discovered =
                discoveredNames.Contains(c.constellationName);
        }
    }

    void AddFigure(
        string name,
        string description,
        int[] points,
        int[] links)
    {
        ConstellationDefinition c =
            new ConstellationDefinition();

        c.constellationName = name;
        c.description = description;

        for (int i = 0; i + 1 < points.Length; i += 2)
        {
            c.stars.Add(new Vector2(
                points[i] / 5f,
                points[i + 1] / 5f
            ));
        }

        for (int i = 0; i + 1 < links.Length; i += 2)
        {
            c.connections.Add(
                new Vector2Int(links[i], links[i + 1])
            );
        }

        constellations.Add(c);
    }

    // --------------------------------
    // SPREAD OBJECTS THROUGH SKY
    // --------------------------------

    [ContextMenu("Redistribute Constellations Only")]
    public void DistributeConstellations()
    {
        System.Random random =
            new System.Random(skySeed);

        List<Vector2> placed = new List<Vector2>();

        foreach (var constellation in constellations)
        {
            Vector2 best = Vector2.one * 0.5f;
            float bestDistance = -1f;

            // Sample many irregular positions, not rows.
            for (int attempt = 0; attempt < 1200; attempt++)
            {
                Vector2 candidate = new Vector2(
                    Mathf.Lerp(
                        0.07f, 0.93f,
                        (float)random.NextDouble()
                    ),
                    Mathf.Lerp(
                        0.07f, 0.93f,
                        (float)random.NextDouble()
                    )
                );

                float nearest = float.MaxValue;

                foreach (Vector2 p in placed)
                {
                    nearest = Mathf.Min(
                        nearest,
                        Vector2.Distance(candidate, p)
                    );
                }

                if (placed.Count == 0)
                    nearest = 1f;

                if (nearest > bestDistance)
                {
                    bestDistance = nearest;
                    best = candidate;
                }

                if (nearest >= minimumConstellationSpacing)
                {
                    best = candidate;
                    break;
                }
            }

            placed.Add(best);
            constellation.skyPosition = best;
        }
    }

    // --------------------------------
    // INITIALIZATION
    // --------------------------------

    void Awake()
    {
        cam = playerCamera != null
            ? playerCamera : Camera.main;

        if (playerCamera == null)
            playerCamera = cam;

        if (playerMovement == null && cam != null)
        {
            playerMovement =
                cam.GetComponentInParent<FPSController>();
        }

        if (leftCeilingPanel != null)
        {
            closedPanelPosition =
                leftCeilingPanel.localPosition;
        }

        if (constellations == null ||
            constellations.Count == 0)
        {
            FillOctoberConstellations();
        }

        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

        CreateTextures();
        GenerateStars();
    }

    void GenerateStars()
    {
        backgroundStars.Clear();

        System.Random random =
            new System.Random(12345);

        for (int i = 0; i < backgroundStarCount; i++)
        {
            backgroundStars.Add(new Vector2(
                (float)random.NextDouble(),
                (float)random.NextDouble()
            ));
        }
    }

    void CreateTextures()
    {
        pixel = Texture2D.whiteTexture;

        panelTexture = new Texture2D(1, 1);
        panelTexture.SetPixel(
            0, 0,
            new Color(0.035f, 0.045f, 0.06f, 0.88f)
        );
        panelTexture.Apply();

        int size = 512;

        skyTexture = new Texture2D(
            size, size, TextureFormat.RGBA32, false
        );

        circleMask = new Texture2D(
            size, size, TextureFormat.RGBA32, false
        );

        skyTexture.wrapMode = TextureWrapMode.Clamp;
        circleMask.wrapMode = TextureWrapMode.Clamp;

        Vector2 center =
            Vector2.one * (size - 1) * 0.5f;

        float radius = size * 0.5f;

        Color[] skyPixels = new Color[size * size];
        Color[] maskPixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center.x) / radius;
                float dy = (y - center.y) / radius;

                float distance =
                    Mathf.Sqrt(dx * dx + dy * dy);

                float noise = Mathf.PerlinNoise(
                    x * 0.012f + 12f,
                    y * 0.012f + 27f
                );

                Color inner = new Color(
                    0.018f, 0.032f, 0.085f, 1f
                );

                Color outer = new Color(
                    0.004f, 0.007f, 0.023f, 1f
                );

                Color sky = Color.Lerp(
                    inner,
                    outer,
                    Mathf.Clamp01(distance)
                );

                sky += new Color(
                    0.022f, 0.016f, 0.045f, 0f
                ) * noise;

                sky.a = 1f;

                skyPixels[y * size + x] = sky;

                float alpha = Mathf.SmoothStep(
                    0f, 1f,
                    Mathf.InverseLerp(
                        0.925f, 0.97f, distance
                    )
                );

                maskPixels[y * size + x] =
                    new Color(
                        0.005f, 0.007f, 0.012f,
                        alpha
                    );
            }
        }

        skyTexture.SetPixels(skyPixels);
        skyTexture.Apply();

        circleMask.SetPixels(maskPixels);
        circleMask.Apply();
    }

    // --------------------------------
    // GUI
    // --------------------------------

    void InitializeGUI()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 21;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        infoStyle = new GUIStyle(GUI.skin.label);
        infoStyle.fontSize = 18;
        infoStyle.wordWrap = true;
        infoStyle.normal.textColor =
            new Color(0.85f, 0.9f, 1f);

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 19;
        hintStyle.alignment = TextAnchor.MiddleRight;
        hintStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 15;
        smallStyle.wordWrap = true;
        smallStyle.normal.textColor =
            new Color(0.72f, 0.8f, 0.92f);
    }

    bool IsUnlocked()
    {
        if (leftCeilingPanel == null)
            return false;

        return Vector3.Distance(
            leftCeilingPanel.localPosition,
            closedPanelPosition
        ) >= openingThreshold;
    }

    void Update()
    {
        if (observing)
            HandleObservation();
        else
            DetectTelescope();

        float desiredAlpha =
            !observing && lookingAtTelescope ? 1f : 0f;

        panelAlpha = Mathf.MoveTowards(
            panelAlpha,
            desiredAlpha,
            panelFadeSpeed * Time.deltaTime
        );

        if (messageTimer > 0f)
            messageTimer -= Time.deltaTime;
    }

    void DetectTelescope()
    {
        lookingAtTelescope = false;

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
            lookingAtTelescope =
                hit.collider
                    .GetComponentInParent<TelescopeObservation>()
                == this;
        }

        if (lookingAtTelescope &&
            Input.GetKeyDown(useKey))
        {
            if (IsUnlocked())
                StartObservation();
            else
                ShowMessage("Сначала открой потолок!");
        }
    }

    void HandleObservation()
    {
        if (Input.GetKeyDown(exitKey))
        {
            StopObservation();
            return;
        }

        if (Input.GetMouseButton(0))
        {
            viewCenter.x -=
                Input.GetAxis("Mouse X") *
                panSpeed / zoom;

            viewCenter.y -=
                Input.GetAxis("Mouse Y") *
                panSpeed / zoom;
        }

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.001f)
        {
            zoom = Mathf.Clamp(
                zoom + scroll * zoomSpeed * 0.15f,
                minZoom, maxZoom
            );
        }

        viewCenter.x = Mathf.Clamp01(viewCenter.x);
        viewCenter.y = Mathf.Clamp01(viewCenter.y);

        if (Input.GetKeyDown(observeKey))
            DiscoverConstellation();
    }

    void StartObservation()
    {
        observing = true;
        lookingAtTelescope = false;
        messageTimer = 0f;

        if (playerMovement != null)
            playerMovement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void StopObservation()
    {
        observing = false;
        lookingAtTelescope = false;

        if (playerMovement != null)
            playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ShowMessage(string text)
    {
        message = text;
        messageTimer = 2.5f;
    }

    // --------------------------------
    // SKY COORDINATES
    // --------------------------------

    Rect GetScopeRect()
    {
        float radius = Mathf.Min(
            scopeRadius,
            Screen.width * 0.485f,
            Screen.height * 0.49f
        );

        return new Rect(
            Screen.width * 0.5f - radius,
            Screen.height * 0.5f - radius,
            radius * 2f,
            radius * 2f
        );
    }

    Vector2 SkyToScreen(Vector2 sky, Rect rect)
    {
        return new Vector2(
            rect.center.x +
                (sky.x - viewCenter.x) *
                rect.width * zoom,

            rect.center.y -
                (sky.y - viewCenter.y) *
                rect.height * zoom
        );
    }

    bool InsideCircle(
        Vector2 point, Vector2 center, float radius)
    {
        return (point - center).sqrMagnitude
            <= radius * radius;
    }

    void DiscoverConstellation()
    {
        Rect scope = GetScopeRect();

        ConstellationDefinition nearest = null;
        float bestDistance = discoveryRadius;

        foreach (var c in constellations)
        {
            Vector2 point = SkyToScreen(
                c.skyPosition, scope
            );

            float distance = Vector2.Distance(
                point, scope.center
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = c;
            }
        }

        if (nearest == null)
        {
            ShowMessage("Созвездие не обнаружено");
            return;
        }

        if (!nearest.discovered)
        {
            nearest.discovered = true;

            ShowMessage(
                "Созвездие обнаружено: " +
                nearest.constellationName
            );
        }
        else
        {
            ShowMessage(
                "Уже найдено: " +
                nearest.constellationName
            );
        }
    }

    // --------------------------------
    // DRAW HELPERS
    // --------------------------------

    void DrawRect(Rect rect, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixel);
        GUI.color = oldColor;
    }

    void DrawStar(Vector2 point, float size, Color color)
    {
        DrawRect(
            new Rect(
                point.x - size,
                point.y - size,
                size * 2f,
                size * 2f
            ),
            new Color(
                color.r, color.g, color.b,
                starGlow * 0.12f
            )
        );

        DrawRect(
            new Rect(
                point.x - size * 0.35f,
                point.y - size * 0.35f,
                size * 0.7f,
                size * 0.7f
            ),
            color
        );
    }

    void DrawLine(Vector2 a, Vector2 b, Color color)
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;

        Vector2 d = b - a;

        float angle =
            Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

        GUIUtility.RotateAroundPivot(angle, a);

        GUI.color = color;

        GUI.DrawTexture(
            new Rect(
                a.x,
                a.y - 0.75f,
                d.magnitude,
                1.5f
            ),
            pixel
        );

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    void DrawPanel(Rect rect)
    {
        GUI.DrawTexture(rect, panelTexture);
    }

    // --------------------------------
    // ONGUI
    // --------------------------------

    void OnGUI()
    {
        InitializeGUI();

        if (!observing)
        {
            DrawIdleUI();
            return;
        }

        DrawTelescopeView();
        DrawObservationUI();
    }

    void DrawIdleUI()
    {
        if (panelAlpha <= 0.001f)
            return;

        float width = Mathf.Min(
            380f, Screen.width - 40f
        );

        float x =
            Screen.width - width - 25f +
            (1f - panelAlpha) *
            panelSlideDistance;

        Rect panel = new Rect(
            x, Screen.height - 85f,
            width, 55f
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
            IsUnlocked()
                ? "[E] Смотреть в телескоп"
                : "Сначала открой потолок",
            hintStyle
        );

        GUI.color = old;
    }

    void DrawTelescopeView()
    {
        DrawRect(
            new Rect(
                0, 0, Screen.width, Screen.height
            ),
            new Color(
                0.005f, 0.007f, 0.012f, 1f
            )
        );

        Rect scope = GetScopeRect();

        Vector2 center = scope.center;
        float radius = scope.width * 0.5f;

        GUI.DrawTexture(scope, skyTexture);

        // Background field stars
        foreach (Vector2 star in backgroundStars)
        {
            Vector2 point = SkyToScreen(star, scope);

            if (!InsideCircle(
                point, center, radius * 0.92f))
                continue;

            DrawStar(
                point, 1.5f,
                new Color(0.85f, 0.9f, 1f)
            );
        }

        foreach (var c in constellations)
        {
            List<Vector2> points =
                new List<Vector2>();

            foreach (Vector2 local in c.stars)
            {
                Vector2 position =
                    c.skyPosition +
                    local * constellationPatternScale;

                points.Add(
                    SkyToScreen(position, scope)
                );
            }

            if (c.discovered)
            {
                foreach (var connection in c.connections)
                {
                    if (connection.x < 0 ||
                        connection.y < 0 ||
                        connection.x >= points.Count ||
                        connection.y >= points.Count)
                        continue;

                    Vector2 a = points[connection.x];
                    Vector2 b = points[connection.y];

                    if (!InsideCircle(
                        a, center, radius * 0.92f) ||
                        !InsideCircle(
                        b, center, radius * 0.92f))
                        continue;

                    DrawLine(
                        a, b,
                        new Color(
                            0.55f, 0.78f, 1f,
                            constellationLineAlpha
                        )
                    );
                }
            }

            foreach (Vector2 point in points)
            {
                if (!InsideCircle(
                    point, center, radius * 0.92f))
                    continue;

                DrawStar(
                    point,
                    c.discovered ? 6f : 4.5f,
                    Color.white
                );
            }

            if (c.discovered)
            {
                Vector2 label = SkyToScreen(
                    c.skyPosition, scope
                );

                if (InsideCircle(
                    label, center, radius * 0.7f))
                {
                    GUI.Label(
                        new Rect(
                            label.x + 12f,
                            label.y + 10f,
                            230f, 30f
                        ),
                        c.constellationName,
                        smallStyle
                    );
                }
            }
        }

        GUI.DrawTexture(scope, circleMask);

        // Central targeting reticle
        DrawRect(
            new Rect(
                center.x - 12f, center.y,
                24f, 1f
            ),
            new Color(1f, 1f, 1f, 0.8f)
        );

        DrawRect(
            new Rect(
                center.x, center.y - 12f,
                1f, 24f
            ),
            new Color(1f, 1f, 1f, 0.8f)
        );
    }

    void DrawObservationUI()
    {
        Rect titlePanel = new Rect(
            20f, 20f, 370f, 75f
        );

        DrawPanel(titlePanel);

        GUI.Label(
            new Rect(
                35f, 29f, 340f, 30f
            ),
            "НАБЛЮДЕНИЕ ЗВЁЗД",
            titleStyle
        );

        GUI.Label(
            new Rect(
                35f, 59f, 330f, 25f
            ),
            "Октябрьское небо • Беларусь",
            smallStyle
        );

        float controlsWidth = Mathf.Min(
            590f, Screen.width - 40f
        );

        Rect controls = new Rect(
            20f,
            Screen.height - 100f,
            controlsWidth,
            75f
        );

        DrawPanel(controls);

        GUI.Label(
            new Rect(
                controls.x + 15f,
                controls.y + 8f,
                controls.width - 30f,
                40f
            ),
            "ЛКМ + мышь — обзор\n" +
            "Колёсико — зум | F — найти | Esc — выйти",
            smallStyle
        );

        GUI.Label(
            new Rect(
                controls.x + 15f,
                controls.y + 49f,
                controls.width - 30f,
                22f
            ),
            "Приближение: " +
            zoom.ToString("0.0") + "x",
            smallStyle
        );

        int found = 0;

        foreach (var c in constellations)
        {
            if (c.discovered)
                found++;
        }

        float listWidth = Mathf.Min(
            300f, Screen.width * 0.32f
        );

        float listHeight = Mathf.Min(
            90f + found * 22f,
            Screen.height * 0.58f
        );

        Rect listPanel = new Rect(
            Screen.width - listWidth - 20f,
            20f,
            listWidth,
            listHeight
        );

        DrawPanel(listPanel);

        GUI.Label(
            new Rect(
                listPanel.x + 12f,
                listPanel.y + 10f,
                listWidth - 24f,
                25f
            ),
            "Найдено: " + found +
            " / " + constellations.Count,
            infoStyle
        );

        float y = listPanel.y + 42f;

        foreach (var c in constellations)
        {
            if (!c.discovered)
                continue;

            if (y + 22f > listPanel.yMax - 8f)
                break;

            GUI.Label(
                new Rect(
                    listPanel.x + 15f,
                    y,
                    listWidth - 30f,
                    22f
                ),
                "• " + c.constellationName,
                smallStyle
            );

            y += 22f;
        }

        if (messageTimer > 0f)
        {
            float width = Mathf.Min(
                460f, Screen.width - 40f
            );

            Rect messagePanel = new Rect(
                Screen.width * 0.5f - width * 0.5f,
                105f,
                width,
                50f
            );

            DrawPanel(messagePanel);

            GUI.Label(
                new Rect(
                    messagePanel.x + 15f,
                    messagePanel.y + 10f,
                    width - 30f,
                    30f
                ),
                message,
                infoStyle
            );
        }
    }

    void OnDisable()
    {
        if (observing)
            StopObservation();
    }

    void OnDestroy()
    {
        if (panelTexture != null)
            Destroy(panelTexture);

        if (skyTexture != null)
            Destroy(skyTexture);

        if (circleMask != null)
            Destroy(circleMask);
    }
}
