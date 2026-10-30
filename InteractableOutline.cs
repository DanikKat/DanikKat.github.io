
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InteractableOutline : MonoBehaviour
{
    [Header("Outline Material")]
    public Material outlineMaterial;

    [Header("Appearance")]
    public Color outlineColor = new Color(1f, 0.5f, 0f);

    [Range(0.001f, 0.1f)]
    public float outlineWidth = 0.025f;

    [Range(1f, 30f)]
    public float fadeSpeed = 12f;

    private bool highlighted;
    private float intensity;

    private readonly List<MeshRenderer> outlineRenderers =
        new List<MeshRenderer>();

    private MaterialPropertyBlock properties;

    private static readonly int ColorID =
        Shader.PropertyToID("_OutlineColor");

    private static readonly int WidthID =
        Shader.PropertyToID("_OutlineWidth");

    void Awake()
    {
        properties = new MaterialPropertyBlock();
        CreateOutline();
    }

    void CreateOutline()
    {
        if (outlineMaterial == null)
        {
            Debug.LogError(
                "Assign Outline Material on " + name, this
            );
            return;
        }

        MeshFilter[] filters =
            GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter filter in filters)
        {
            MeshRenderer original =
                filter.GetComponent<MeshRenderer>();

            if (original == null ||
                filter.sharedMesh == null)
                continue;

            GameObject outline =
                new GameObject("__InteractionOutline");

            outline.transform.SetParent(
                filter.transform, false
            );

            outline.transform.localPosition = Vector3.zero;
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = Vector3.one;

            MeshFilter newFilter =
                outline.AddComponent<MeshFilter>();

            newFilter.sharedMesh = filter.sharedMesh;

            MeshRenderer newRenderer =
                outline.AddComponent<MeshRenderer>();

            int materialCount = Mathf.Max(
                1, original.sharedMaterials.Length
            );

            Material[] materials =
                new Material[materialCount];

            for (int i = 0; i < materialCount; i++)
                materials[i] = outlineMaterial;

            newRenderer.sharedMaterials = materials;

            newRenderer.shadowCastingMode =
                ShadowCastingMode.Off;

            newRenderer.receiveShadows = false;
            newRenderer.lightProbeUsage =
                LightProbeUsage.Off;

            newRenderer.enabled = false;

            outlineRenderers.Add(newRenderer);
        }
    }

    public void SetHighlighted(bool value)
    {
        highlighted = value;
    }

    void Update()
    {
        float target = highlighted ? 1f : 0f;

        intensity = Mathf.MoveTowards(
            intensity,
            target,
            fadeSpeed * Time.deltaTime
        );

        bool visible = intensity > 0.001f;

        foreach (MeshRenderer renderer in outlineRenderers)
        {
            if (renderer == null)
                continue;

            renderer.enabled = visible;

            if (!visible)
                continue;

            renderer.GetPropertyBlock(properties);

            Color color = outlineColor;
            color.a *= intensity;

            properties.SetColor(ColorID, color);
            properties.SetFloat(WidthID, outlineWidth);

            renderer.SetPropertyBlock(properties);
        }
    }

    public virtual void Interact()
    {
        Debug.Log("Interacted with: " + gameObject.name);
    }
}
