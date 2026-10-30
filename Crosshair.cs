
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [Range(1f, 20f)]
    public float dotSize = 4f;

    public Color dotColor = Color.white;

    private RectTransform dotRect;
    private Image dotImage;

    void Start()
    {
        GameObject canvasObj = new GameObject("Crosshair Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject dotObj = new GameObject("Crosshair Dot");
        dotObj.transform.SetParent(canvasObj.transform, false);

        dotImage = dotObj.AddComponent<Image>();
        dotImage.color = dotColor;
        dotImage.raycastTarget = false;

        dotRect = dotObj.GetComponent<RectTransform>();

        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = Vector2.zero;

        UpdateDot();
    }

    void Update()
    {
        UpdateDot();
    }

    void UpdateDot()
    {
        if (dotRect == null || dotImage == null)
            return;

        dotRect.sizeDelta = Vector2.one * dotSize;
        dotImage.color = dotColor;
    }
}
