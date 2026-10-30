
using System;
using System.Collections.Generic;
using UnityEngine;

public class FinalExamDoor : MonoBehaviour
{
    [Header("Player")]
    public Camera playerCamera;
    public FPSController playerMovement;
    public ResearchBooksManager booksManager;

    [Header("Interaction")]
    public float interactionDistance = 4f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Exam")]
    [Range(1, 40)]
    public int questionsPerExam = 15;

    [Range(1, 15)]
    public int passingScore = 10;

    [Header("Controls")]
    public KeyCode notesKey = KeyCode.N;
    public KeyCode closeNotesKey = KeyCode.Escape;

    [Header("Appearance")]
    public Color backgroundColor =
        new Color(0.008f, 0.012f, 0.02f);

    public Color panelColor =
        new Color(0.035f, 0.045f, 0.06f);

    public Color accentColor =
        new Color(0.48f, 0.7f, 0.88f);

    public Color correctColor =
        new Color(0.34f, 0.73f, 0.49f);

    public Color wrongColor =
        new Color(0.9f, 0.39f, 0.38f);

    [Header("Hint Animation")]
    public float panelFadeSpeed = 6f;
    public float panelSlideDistance = 35f;

    private class ExamQuestion
    {
        public string question;
        public string[] answers;
        public int correctIndex;
        public string explanation;
    }

    private readonly List<ExamQuestion> questionBank =
        new List<ExamQuestion>();

    private readonly List<ExamQuestion> currentExam =
        new List<ExamQuestion>();

    private bool lookingAtDoor;
    private bool examActive;
    private bool examFinished;
    private bool notesOpen;

    private int currentQuestionIndex;
    private int selectedAnswer = -1;
    private int correctAnswers;

    private float hintAlpha;

    private Vector2 notesScroll;

    private Texture2D pixel;

    private GUIStyle titleStyle;
    private GUIStyle headingStyle;
    private GUIStyle textStyle;
    private GUIStyle smallStyle;
    private GUIStyle hintStyle;
    private GUIStyle buttonStyle;
    private GUIStyle answerStyle;
    private GUIStyle centeredStyle;

    // --------------------------------
    // INITIALIZATION
    // --------------------------------

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerMovement == null && playerCamera != null)
        {
            playerMovement =
                playerCamera.GetComponentInParent<FPSController>();
        }

        if (booksManager == null)
            booksManager = ResearchBooksManager.Instance;

        pixel = Texture2D.whiteTexture;

        BuildQuestionBank();
    }

    void Start()
    {
        if (booksManager == null)
            booksManager = ResearchBooksManager.Instance;
    }

    // --------------------------------
    // UPDATE
    // --------------------------------

    void Update()
    {
        if (examActive)
        {
            HandleExamInput();
            return;
        }

        DetectDoor();

        hintAlpha = Mathf.MoveTowards(
            hintAlpha,
            lookingAtDoor ? 1f : 0f,
            panelFadeSpeed * Time.deltaTime
        );
    }

    void DetectDoor()
    {
        lookingAtDoor = false;

        if (playerCamera == null)
            return;

        if (playerMovement != null &&
            !playerMovement.enabled)
        {
            return;
        }

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
            lookingAtDoor =
                hit.collider.GetComponentInParent
                <FinalExamDoor>() == this;
        }

        if (lookingAtDoor &&
            Input.GetKeyDown(interactKey))
        {
            StartExam();
        }
    }

    void HandleExamInput()
    {
        if (Input.GetKeyDown(notesKey))
        {
            ToggleNotes();
            return;
        }

        if (notesOpen &&
            Input.GetKeyDown(closeNotesKey))
        {
            notesOpen = false;
        }

        if (notesOpen || examFinished)
            return;

        // Answer using number keys 1-4.
        if (selectedAnswer == -1)
        {
            for (int i = 0; i < 4; i++)
            {
                KeyCode key =
                    (KeyCode)((int)KeyCode.Alpha1 + i);

                if (Input.GetKeyDown(key))
                {
                    SelectAnswer(i);
                    break;
                }
            }
        }
    }

    // --------------------------------
    // EXAM FLOW
    // --------------------------------

    void StartExam()
    {
        if (questionBank.Count == 0)
            return;

        if (booksManager == null)
            booksManager = ResearchBooksManager.Instance;

        // Never start a second interaction on top of a book.
        if (booksManager != null &&
            booksManager.IsBookOpen)
        {
            return;
        }

        currentExam.Clear();

        // Make a temporary copy of the question bank.
        List<ExamQuestion> available =
            new List<ExamQuestion>(questionBank);

        // Fisher-Yates shuffle: no repeated questions.
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            ExamQuestion temp = available[i];
            available[i] = available[j];
            available[j] = temp;
        }

        int count = Mathf.Min(
            questionsPerExam,
            available.Count
        );

        for (int i = 0; i < count; i++)
        {
            ExamQuestion source = available[i];

            ExamQuestion question = new ExamQuestion
            {
                question = source.question,
                answers = (string[])source.answers.Clone(),
                correctIndex = source.correctIndex,
                explanation = source.explanation
            };

            // Shuffle answer options independently.
            string correctText =
                question.answers[question.correctIndex];

            for (int j = question.answers.Length - 1;
                 j > 0; j--)
            {
                int k = UnityEngine.Random.Range(0, j + 1);

                string temp = question.answers[j];
                question.answers[j] = question.answers[k];
                question.answers[k] = temp;
            }

            for (int j = 0; j < question.answers.Length; j++)
            {
                if (question.answers[j] == correctText)
                {
                    question.correctIndex = j;
                    break;
                }
            }

            currentExam.Add(question);
        }

        currentQuestionIndex = 0;
        selectedAnswer = -1;
        correctAnswers = 0;

        examActive = true;
        examFinished = false;
        notesOpen = false;
        lookingAtDoor = false;

        if (booksManager != null)
            booksManager.SetQuizMode(true);

        if (playerMovement != null)
            playerMovement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void SelectAnswer(int index)
    {
        if (!examActive ||
            examFinished ||
            notesOpen ||
            selectedAnswer >= 0)
        {
            return;
        }

        ExamQuestion question =
            currentExam[currentQuestionIndex];

        if (index < 0 || index >= question.answers.Length)
            return;

        selectedAnswer = index;

        if (index == question.correctIndex)
            correctAnswers++;
    }

    void NextQuestion()
    {
        if (selectedAnswer == -1)
            return;

        currentQuestionIndex++;
        selectedAnswer = -1;

        if (currentQuestionIndex >= currentExam.Count)
            examFinished = true;
    }

    void ToggleNotes()
    {
        if (booksManager == null ||
            !booksManager.hasJournal)
        {
            notesOpen = false;
            return;
        }

        notesOpen = !notesOpen;
        notesScroll = Vector2.zero;
    }

    void ExitExam()
    {
        examActive = false;
        examFinished = false;
        notesOpen = false;
        lookingAtDoor = false;

        if (booksManager != null)
            booksManager.SetQuizMode(false);

        if (playerMovement != null)
            playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --------------------------------
    // QUESTION BANK
    // --------------------------------

    void AddQuestion(
        string question,
        string correct,
        string wrong1,
        string wrong2,
        string wrong3,
        string explanation)
    {
        questionBank.Add(new ExamQuestion
        {
            question = question,
            answers = new string[]
            {
                correct, wrong1, wrong2, wrong3
            },
            correctIndex = 0,
            explanation = explanation
        });
    }

    void BuildQuestionBank()
    {
        questionBank.Clear();

        // PLANETS: 1-10

        AddQuestion(
            "Какая планета ближе всего к Солнцу?",
            "Меркурий", "Венера", "Земля", "Марс",
            "Меркурий — ближайшая к Солнцу планета."
        );

        AddQuestion(
            "Какая планета самая большая в Солнечной системе?",
            "Юпитер", "Сатурн", "Нептун", "Земля",
            "Юпитер — крупнейшая планета Солнечной системы."
        );

        AddQuestion(
            "Какую планету называют Красной планетой?",
            "Марс", "Венеру", "Меркурий", "Юпитер",
            "Красноватый цвет Марса связан с оксидами железа."
        );

        AddQuestion(
            "На какой планете находится Большое красное пятно?",
            "Юпитер", "Сатурн", "Нептун", "Уран",
            "Большое красное пятно — гигантский атмосферный вихрь Юпитера."
        );

        AddQuestion(
            "Какая планета имеет наиболее выраженную систему колец?",
            "Сатурн", "Марс", "Земля", "Меркурий",
            "Кольца Сатурна состоят преимущественно из частиц льда и каменного материала."
        );

        AddQuestion(
            "Какая планета вращается почти на боку?",
            "Уран", "Венера", "Юпитер", "Меркурий",
            "Ось вращения Урана наклонена примерно на 98 градусов."
        );

        AddQuestion(
            "Какая планета является самой горячей в Солнечной системе?",
            "Венера", "Меркурий", "Марс", "Юпитер",
            "Плотная атмосфера Венеры создаёт чрезвычайно сильный парниковый эффект."
        );

        AddQuestion(
            "Какая планета находится дальше всего от Солнца среди восьми планет?",
            "Нептун", "Уран", "Сатурн", "Юпитер",
            "Нептун — восьмая и самая удалённая от Солнца планета."
        );

        AddQuestion(
            "На какой планете находятся знакомые нам океаны жидкой воды?",
            "Земля", "Венера", "Марс", "Меркурий",
            "Земля обладает устойчивыми океанами жидкой воды на поверхности."
        );

        AddQuestion(
            "Сколько планет в современной классификации Солнечной системы?",
            "8", "7", "9", "10",
            "С 2006 года Плутон классифицируется как карликовая планета."
        );

        // CONSTELLATIONS: 11-20

        AddQuestion(
            "Какое созвездие известно звёздным рисунком в форме буквы W?",
            "Кассиопея", "Лира", "Орёл", "Овен",
            "Пять ярких звёзд Кассиопеи образуют узнаваемую форму W или M."
        );

        AddQuestion(
            "В каком созвездии находится Большой Ковш?",
            "Большая Медведица", "Малая Медведица",
            "Орион", "Пегас",
            "Большой Ковш — известный астеризм внутри Большой Медведицы."
        );

        AddQuestion(
            "Какое созвездие содержит Полярную звезду?",
            "Малая Медведица", "Большая Медведица",
            "Цефей", "Дракон",
            "Полярная звезда расположена в Малой Медведице."
        );

        AddQuestion(
            "В каком созвездии расположена звезда Вега?",
            "Лира", "Лебедь", "Орёл", "Персей",
            "Вега — самая яркая звезда созвездия Лиры."
        );

        AddQuestion(
            "В каком созвездии находится звезда Денеб?",
            "Лебедь", "Лира", "Орион", "Кассиопея",
            "Денеб — одна из самых ярких звёзд Лебедя."
        );

        AddQuestion(
            "Какое созвездие известно тремя звёздами пояса?",
            "Орион", "Телец", "Персей", "Андромеда",
            "Пояс Ориона образуют Альнитак, Альнилам и Минтака."
        );

        AddQuestion(
            "Какое созвездие содержит звезду Альтаир?",
            "Орёл", "Лебедь", "Лира", "Пегас",
            "Альтаир — ярчайшая звезда созвездия Орла."
        );

        AddQuestion(
            "Какое созвездие связано с крылатым конём древнегреческих мифов?",
            "Пегас", "Персей", "Цефей", "Геркулес",
            "Пегас в мифологии изображается крылатым конём."
        );

        AddQuestion(
            "Какое созвездие названо в честь дочери Кассиопеи?",
            "Андромеда", "Лира", "Лебедь", "Ящерица",
            "В греческой мифологии Андромеда была дочерью Кассиопеи и Цефея."
        );

        AddQuestion(
            "Какое созвездие изображает морское млекопитающее?",
            "Кит", "Рыбы", "Дельфин", "Орел",
            "Современное название «Кит» связано с образом морского чудовища из мифологии."
        );

        // ASTRONOMY: 21-30

        AddQuestion(
            "Что является центром Солнечной системы?",
            "Солнце", "Земля", "Юпитер", "Луна",
            "Планеты обращаются вокруг Солнца под действием гравитации."
        );

        AddQuestion(
            "Как называется естественный спутник Земли?",
            "Луна", "Фобос", "Титан", "Европа",
            "Луна — единственный естественный спутник Земли."
        );

        AddQuestion(
            "Как называют огромную систему звёзд, газа и пыли, связанную гравитацией?",
            "Галактика", "Комета", "Астероид", "Планета",
            "Галактика содержит звёзды, газ, пыль и тёмную материю."
        );

        AddQuestion(
            "Как называется галактика, в которой расположена Солнечная система?",
            "Млечный Путь", "Андромеда", "Треугольник", "Большое Магелланово Облако",
            "Солнечная система находится в галактике Млечный Путь."
        );

        AddQuestion(
            "Что такое световой год?",
            "Единица расстояния", "Единица времени",
            "Период вращения Земли", "Возраст звезды",
            "Световой год — расстояние, которое свет проходит за один год."
        );

        AddQuestion(
            "Что вызывает смену времён года на Земле?",
            "Наклон оси вращения Земли", "Изменение расстояния до Луны",
            "Вращение Луны", "Изменение массы Солнца",
            "Сезоны определяются наклоном земной оси относительно плоскости орбиты."
        );

        AddQuestion(
            "Почему происходит смена дня и ночи?",
            "Из-за вращения Земли вокруг оси", "Из-за движения Луны",
            "Из-за вращения Солнца вокруг Земли", "Из-за солнечного ветра",
            "Земля вращается вокруг собственной оси примерно за сутки."
        );

        AddQuestion(
            "Что представляет собой комета?",
            "Малое ледяное тело Солнечной системы",
            "Погасшую звезду", "Естественный спутник Земли",
            "Разновидность галактики",
            "Кометы состоят из льда, пыли и каменного материала."
        );

        AddQuestion(
            "Что происходит во время солнечного затмения?",
            "Луна закрывает Солнце для наблюдателя",
            "Земля закрывает Луну", "Солнце перестаёт излучать свет",
            "Марс закрывает Солнце",
            "Солнечное затмение происходит, когда Луна находится между Землёй и Солнцем."
        );

        AddQuestion(
            "Какая сила удерживает планеты на орбитах?",
            "Гравитация", "Магнетизм", "Трение", "Атмосферное давление",
            "Гравитационное притяжение определяет движение планет вокруг Солнца."
        );

        // HISTORY AND TELESCOPES: 31-40

        AddQuestion(
            "Какой учёный проводил телескопические наблюдения спутников Юпитера в 1610 году?",
            "Галилео Галилей", "Исаак Ньютон",
            "Иоганн Кеплер", "Николай Коперник",
            "Галилей открыл четыре крупнейших спутника Юпитера."
        );

        AddQuestion(
            "Кто сформулировал три закона движения планет?",
            "Иоганн Кеплер", "Галилео Галилей",
            "Эдвин Хаббл", "Исаак Ньютон",
            "Кеплер описал движение планет по эллиптическим орбитам."
        );

        AddQuestion(
            "Кто сформулировал закон всемирного тяготения?",
            "Исаак Ньютон", "Иоганн Кеплер",
            "Тихо Браге", "Эдвин Хаббл",
            "Ньютон сформулировал закон всемирного тяготения."
        );

        AddQuestion(
            "Какой астроном развивал гелиоцентрическую модель мира в XVI веке?",
            "Николай Коперник", "Птолемей",
            "Эдвин Хаббл", "Тихо Браге",
            "Коперник обосновывал модель, в которой Земля обращается вокруг Солнца."
        );

        AddQuestion(
            "Как называется телескоп, использующий зеркало для формирования изображения?",
            "Рефлектор", "Рефрактор",
            "Спектроскоп", "Секстант",
            "Главный собирающий элемент рефлектора — зеркало."
        );

        AddQuestion(
            "Как называется телескоп, использующий линзы?",
            "Рефрактор", "Рефлектор",
            "Радиоинтерферометр", "Спектрограф",
            "Рефрактор формирует изображение с помощью линз."
        );

        AddQuestion(
            "С каким учёным связан ранний зеркальный телескоп конструкции XVII века?",
            "Исаак Ньютон", "Николай Коперник",
            "Эдвин Хаббл", "Тихо Браге",
            "Ньютон построил известную конструкцию телескопа-рефлектора."
        );

        AddQuestion(
            "Что помогает астроному наблюдать более слабые объекты при прочих равных?",
            "Увеличение диаметра объектива",
            "Уменьшение диаметра объектива",
            "Уменьшение времени наблюдения",
            "Закрытие части объектива",
            "Большая апертура позволяет собирать больше света."
        );

        AddQuestion(
            "Для изучения какого излучения предназначен радиотелескоп?",
            "Радиоволн", "Видимого света",
            "Ультрафиолета", "Рентгеновских лучей",
            "Радиотелескопы принимают радиоизлучение космических объектов."
        );

        AddQuestion(
            "Как называется прибор для разложения света на спектр?",
            "Спектроскоп", "Секстант",
            "Хронометр", "Барометр",
            "Спектроскоп позволяет анализировать спектральный состав света."
        );
    }

    // --------------------------------
    // UI STYLES
    // --------------------------------

    void SetStyleColors(GUIStyle style, Color color)
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

    GUIStyle CreateLabel(
        int size,
        bool bold,
        TextAnchor alignment)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);

        style.fontSize = size;
        style.fontStyle =
            bold ? FontStyle.Bold : FontStyle.Normal;

        style.alignment = alignment;
        style.wordWrap = true;

        SetStyleColors(style, Color.white);

        return style;
    }

    void InitializeGUI()
    {
        if (titleStyle != null &&
            headingStyle != null &&
            textStyle != null &&
            smallStyle != null &&
            hintStyle != null &&
            buttonStyle != null &&
            answerStyle != null &&
            centeredStyle != null)
        {
            return;
        }

        titleStyle = CreateLabel(
            26, true, TextAnchor.MiddleCenter
        );

        headingStyle = CreateLabel(
            22, true, TextAnchor.UpperLeft
        );

        textStyle = CreateLabel(
            18, false, TextAnchor.UpperLeft
        );

        smallStyle = CreateLabel(
            14, false, TextAnchor.MiddleLeft
        );

        hintStyle = CreateLabel(
            20, false, TextAnchor.MiddleRight
        );

        centeredStyle = CreateLabel(
            20, false, TextAnchor.MiddleCenter
        );

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 16;
        buttonStyle.wordWrap = true;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        SetStyleColors(buttonStyle, Color.white);

        answerStyle = new GUIStyle(buttonStyle);
        answerStyle.fontSize = 18;
        answerStyle.alignment = TextAnchor.MiddleLeft;
        answerStyle.padding = new RectOffset(18, 12, 8, 8);
    }

    void DrawRect(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixel);
        GUI.color = old;
    }

    void DrawPanel(Rect rect)
    {
        DrawRect(rect, panelColor);
    }

    // --------------------------------
    // UI MAIN
    // --------------------------------

    void OnGUI()
    {
        InitializeGUI();

        if (!examActive)
        {
            DrawInteractionHint();
            return;
        }

        // Draw in front of ordinary game HUD.
        GUI.depth = -20;

        Color previousColor = GUI.color;
        Color previousContent = GUI.contentColor;
        bool previousEnabled = GUI.enabled;

        GUI.color = Color.white;
        GUI.contentColor = Color.white;
        GUI.enabled = true;

        DrawExamScreen();

        GUI.color = previousColor;
        GUI.contentColor = previousContent;
        GUI.enabled = previousEnabled;
    }

    void DrawInteractionHint()
    {
        if (hintAlpha <= 0.001f)
            return;

        float width = Mathf.Min(
            410f,
            Screen.width - 40f
        );

        float x =
            Screen.width - width - 25f +
            (1f - hintAlpha) * panelSlideDistance;

        Rect panel = new Rect(
            x,
            Screen.height - 85f,
            width,
            55f
        );

        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, hintAlpha);

        DrawPanel(panel);

        GUI.Label(
            new Rect(
                x + 12f,
                panel.y + 8f,
                width - 24f,
                38f
            ),
            "[E] Начать финальный тест",
            hintStyle
        );

        GUI.color = previous;
    }

    void DrawExamScreen()
    {
        DrawRect(
            new Rect(0, 0, Screen.width, Screen.height),
            backgroundColor
        );

        float width = Mathf.Min(
            900f, Screen.width - 36f
        );

        float height = Mathf.Min(
            740f, Screen.height - 36f
        );

        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        DrawPanel(panel);

        GUI.Label(
            new Rect(
                panel.x + 25f,
                panel.y + 18f,
                panel.width - 50f,
                42f
            ),
            "ФИНАЛЬНЫЙ ЭКЗАМЕН",
            titleStyle
        );

        DrawRect(
            new Rect(
                panel.x + 35f,
                panel.y + 67f,
                panel.width - 70f,
                1f
            ),
            new Color(1f, 1f, 1f, 0.2f)
        );

        if (examFinished)
            DrawResults(panel);
        else
            DrawQuestion(panel);

        if (notesOpen)
            DrawNotesOverlay(panel);
    }

    void DrawQuestion(Rect panel)
    {
        ExamQuestion question =
            currentExam[currentQuestionIndex];

        float x = panel.x + 42f;
        float width = panel.width - 84f;

        GUI.Label(
            new Rect(x, panel.y + 83f, width, 28f),
            "ВОПРОС " + (currentQuestionIndex + 1) +
            " ИЗ " + currentExam.Count,
            smallStyle
        );

        GUI.Label(
            new Rect(
                x,
                panel.y + 117f,
                width,
                100f
            ),
            question.question,
            headingStyle
        );

        float answerTop = panel.y + 225f;

        for (int i = 0; i < question.answers.Length; i++)
        {
            Rect answerRect = new Rect(
                x,
                answerTop + i * 68f,
                width,
                56f
            );

            if (selectedAnswer >= 0)
            {
                if (i == question.correctIndex)
                {
                    DrawRect(
                        answerRect,
                        new Color(
                            correctColor.r,
                            correctColor.g,
                            correctColor.b,
                            0.42f
                        )
                    );
                }
                else if (i == selectedAnswer)
                {
                    DrawRect(
                        answerRect,
                        new Color(
                            wrongColor.r,
                            wrongColor.g,
                            wrongColor.b,
                            0.42f
                        )
                    );
                }
            }

            bool oldEnabled = GUI.enabled;
            GUI.enabled = selectedAnswer == -1;

            if (GUI.Button(
                answerRect,
                (i + 1) + ". " + question.answers[i],
                answerStyle
            ))
            {
                SelectAnswer(i);
            }

            GUI.enabled = oldEnabled;
        }

        float bottom = panel.yMax - 135f;

        if (selectedAnswer >= 0)
        {
            bool correct =
                selectedAnswer == question.correctIndex;

            GUI.Label(
                new Rect(
                    x,
                    bottom - 32f,
                    width,
                    30f
                ),
                correct
                    ? "ПРАВИЛЬНО!"
                    : "НЕВЕРНО!",
                headingStyle
            );

            GUI.Label(
                new Rect(
                    x,
                    bottom,
                    width,
                    66f
                ),
                question.explanation,
                textStyle
            );

            if (GUI.Button(
                new Rect(
                    panel.xMax - 230f,
                    panel.yMax - 56f,
                    190f,
                    38f
                ),
                currentQuestionIndex == currentExam.Count - 1
                    ? "ЗАВЕРШИТЬ"
                    : "СЛЕДУЮЩИЙ →",
                buttonStyle
            ))
            {
                NextQuestion();
            }
        }
        else
        {
            GUI.Label(
                new Rect(
                    x,
                    bottom + 30f,
                    width,
                    28f
                ),
                "Выбери один правильный ответ.",
                smallStyle
            );
        }

        DrawProgressBar(
            new Rect(
                x,
                panel.yMax - 76f,
                width - 220f,
                8f
            ),
            (float)currentQuestionIndex / currentExam.Count
        );

        if (booksManager != null && booksManager.hasJournal)
        {
            if (GUI.Button(
                new Rect(
                    x,
                    panel.yMax - 56f,
                    210f,
                    36f
                ),
                "[N] МОИ ЗАМЕТКИ",
                buttonStyle
            ))
            {
                ToggleNotes();
            }
        }
    }

    void DrawProgressBar(Rect rect, float progress)
    {
        DrawRect(
            rect,
            new Color(1f, 1f, 1f, 0.13f)
        );

        DrawRect(
            new Rect(
                rect.x,
                rect.y,
                rect.width * Mathf.Clamp01(progress),
                rect.height
            ),
            accentColor
        );
    }

    // --------------------------------
    // RESULTS
    // --------------------------------

    void DrawResults(Rect panel)
    {
        float percentage =
            currentExam.Count > 0
                ? (float)correctAnswers /
                  currentExam.Count * 100f
                : 0f;

        bool passed = correctAnswers >= passingScore;

        GUI.Label(
            new Rect(
                panel.x + 40f,
                panel.y + 110f,
                panel.width - 80f,
                50f
            ),
            passed
                ? "ЭКЗАМЕН СДАН"
                : "ЭКЗАМЕН НЕ СДАН",
            titleStyle
        );

        GUI.Label(
            new Rect(
                panel.x + 40f,
                panel.y + 198f,
                panel.width - 80f,
                80f
            ),
            correctAnswers +
            " / " + currentExam.Count,
            titleStyle
        );

        GUI.Label(
            new Rect(
                panel.x + 40f,
                panel.y + 275f,
                panel.width - 80f,
                45f
            ),
            "Правильных ответов: " +
            percentage.ToString("0") + "%",
            centeredStyle
        );

        GUI.Label(
            new Rect(
                panel.x + 65f,
                panel.y + 365f,
                panel.width - 130f,
                85f
            ),
            passed
                ? "Поздравляем! Ты успешно завершил исследование обсерватории."
                : "Некоторые темы стоит повторить. Изучи дневник и попробуй ещё раз.",
            centeredStyle
        );

        float buttonsY = panel.yMax - 100f;

        if (GUI.Button(
            new Rect(
                panel.center.x - 205f,
                buttonsY,
                195f,
                45f
            ),
            "ПОПРОБОВАТЬ СНОВА",
            buttonStyle
        ))
        {
            StartExam();
        }

        if (GUI.Button(
            new Rect(
                panel.center.x + 10f,
                buttonsY,
                195f,
                45f
            ),
            "ВЫЙТИ",
            buttonStyle
        ))
        {
            ExitExam();
        }
    }

    // --------------------------------
    // NOTES DURING EXAM
    // --------------------------------

    void DrawNotesOverlay(Rect panel)
    {
        DrawRect(
            panel,
            new Color(0.012f, 0.018f, 0.028f)
        );

        float x = panel.x + 35f;
        float width = panel.width - 70f;

        GUI.Label(
            new Rect(
                x,
                panel.y + 25f,
                width,
                45f
            ),
            "МОИ ЗАМЕТКИ",
            titleStyle
        );

        GUI.Label(
            new Rect(
                x,
                panel.y + 75f,
                width,
                45f
            ),
            "Во время экзамена можно читать свои записи.",
            textStyle
        );

        List<PlayerNote> notes =
            booksManager != null
                ? booksManager.GetNotes()
                : new List<PlayerNote>();

        Rect scrollRect = new Rect(
            x,
            panel.y + 135f,
            width,
            panel.height - 220f
        );

        float totalHeight = 15f;

        foreach (PlayerNote note in notes)
        {
            if (note == null)
                continue;

            string content = note.content ?? "";

            totalHeight += 90f +
                Mathf.Max(
                    20f,
                    textStyle.CalcHeight(
                        new GUIContent(content),
                        Mathf.Max(100f, width - 35f)
                    )
                );
        }

        notesScroll = GUI.BeginScrollView(
            scrollRect,
            notesScroll,
            new Rect(
                0f,
                0f,
                Mathf.Max(120f, width - 20f),
                Mathf.Max(scrollRect.height, totalHeight)
            )
        );

        if (notes.Count == 0)
        {
            GUI.Label(
                new Rect(
                    15f, 20f,
                    width - 50f, 65f
                ),
                "Ты пока не записал ни одной заметки.",
                textStyle
            );
        }

        float y = 12f;

        foreach (PlayerNote note in notes)
        {
            if (note == null)
                continue;

            string title = string.IsNullOrWhiteSpace(note.title)
                ? "Без названия"
                : note.title;

            string content = note.content ?? "";

            float contentHeight = Mathf.Max(
                25f,
                textStyle.CalcHeight(
                    new GUIContent(content),
                    Mathf.Max(100f, width - 35f)
                ) + 12f
            );

            GUI.Label(
                new Rect(
                    12f, y,
                    width - 45f, 35f
                ),
                title,
                headingStyle
            );

            y += 38f;

            GUI.Label(
                new Rect(
                    12f, y,
                    width - 45f,
                    contentHeight
                ),
                content,
                textStyle
            );

            y += contentHeight + 26f;

            DrawRect(
                new Rect(
                    12f, y,
                    width - 45f, 1f
                ),
                new Color(1f, 1f, 1f, 0.13f)
            );

            y += 20f;
        }

        GUI.EndScrollView();

        if (GUI.Button(
            new Rect(
                panel.center.x - 110f,
                panel.yMax - 64f,
                220f,
                40f
            ),
            "[N / ESC] ЗАКРЫТЬ",
            buttonStyle
        ))
        {
            notesOpen = false;
        }
    }

    // --------------------------------
    // CLEANUP
    // --------------------------------

    void OnDisable()
    {
        if (examActive)
            ExitExam();
    }
}
