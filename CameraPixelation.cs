
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraPixelation : MonoBehaviour
{
    [Header("Pixelation Settings")]

    public bool enablePixelation = true;

    [Range(1, 64)]
    public int pixelSize = 6;
}
