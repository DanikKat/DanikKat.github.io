
using UnityEngine;

public class InspectableObject : MonoBehaviour
{
    [Header("Information")]
    public string itemName = "Неизвестный предмет";

    [TextArea(3, 10)]
    public string description = "Описание предмета...";

    [Header("Inspection")]
    public bool alreadyStudied = false;

    [Range(0.5f, 2f)]
    public float previewSize = 1f;
}
