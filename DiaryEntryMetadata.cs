
using UnityEngine;

public class DiaryEntryMetadata : MonoBehaviour
{
    [Header("Diary")]
    public bool includeInDiary = true;

    public DiaryCategory category = DiaryCategory.Other;

    [Header("Optional Content")]
    public string customTitle = "";

    [TextArea(4, 15)]
    public string customDescription = "";

    public Texture2D image;
}
