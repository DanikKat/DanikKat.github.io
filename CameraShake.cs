
using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Default Shake")]
    public float defaultDuration = 1.5f;

    [Range(0f, 0.2f)]
    public float defaultIntensity = 0.035f;

    [Range(1f, 60f)]
    public float frequency = 25f;

    private Coroutine routine;

    private Vector3 currentOffset;
    private Vector3 appliedOffset;

    void LateUpdate()
    {
        // Restore position before the previous shake.
        transform.localPosition -= appliedOffset;

        // Apply the new shake.
        appliedOffset = currentOffset;
        transform.localPosition += appliedOffset;
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultIntensity);
    }

    public void Shake(float duration, float intensity)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(
            ShakeRoutine(duration, intensity)
        );
    }

    IEnumerator ShakeRoutine(float duration, float intensity)
    {
        float elapsed = 0f;
        float seed = Random.Range(0f, 1000f);

        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsed / duration);

            float fade = Mathf.Pow(1f - progress, 2f);

            float t = elapsed * frequency;

            float x =
                Mathf.PerlinNoise(seed, t) * 2f - 1f;

            float y =
                Mathf.PerlinNoise(seed + 100f, t) * 2f - 1f;

            currentOffset = new Vector3(x, y, 0f)
                * intensity * fade;

            yield return null;
        }

        currentOffset = Vector3.zero;
        routine = null;
    }

    void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        transform.localPosition -= appliedOffset;

        appliedOffset = Vector3.zero;
        currentOffset = Vector3.zero;
    }
}
