
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelationRendererFeature : ScriptableRendererFeature
{
    private Material material;
    private PixelationPass pass;

    public override void Create()
    {
        CoreUtils.Destroy(material);
        material = null;

        Shader shader = Shader.Find("Hidden/URP/Pixelation");

        if (shader == null)
        {
            Debug.LogError("Pixelation shader not found!");
            pass = null;
            return;
        }

        material = CoreUtils.CreateEngineMaterial(shader);

        pass = new PixelationPass(material);

        pass.renderPassEvent =
            RenderPassEvent.BeforeRenderingPostProcessing;

        pass.ConfigureInput(ScriptableRenderPassInput.Color);
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (pass == null)
            return;

        Camera camera = renderingData.cameraData.camera;

        if (camera.cameraType != CameraType.Game)
            return;

        CameraPixelation settings =
            camera.GetComponent<CameraPixelation>();

        if (settings == null || !settings.enablePixelation)
            return;

        pass.SetPixelSize(settings.pixelSize);

        renderer.EnqueuePass(pass);
    }

    public override void SetupRenderPasses(
        ScriptableRenderer renderer,
        in RenderingData renderingData)
    {
        if (pass == null)
            return;

        Camera camera = renderingData.cameraData.camera;

        if (camera.cameraType != CameraType.Game)
            return;

        CameraPixelation settings =
            camera.GetComponent<CameraPixelation>();

        if (settings == null || !settings.enablePixelation)
            return;

        pass.SetTarget(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;

        CoreUtils.Destroy(material);
        material = null;
    }

    private class PixelationPass : ScriptableRenderPass
    {
        private Material material;

        private RTHandle cameraTarget;
        private RTHandle tempTexture;

        private int pixelSize = 6;

        private static readonly int PixelSizeID =
            Shader.PropertyToID("_PixelSize");

        public PixelationPass(Material material)
        {
            this.material = material;
        }

        public void SetTarget(RTHandle target)
        {
            cameraTarget = target;
        }

        public void SetPixelSize(int size)
        {
            pixelSize = Mathf.Clamp(size, 1, 64);
        }

        public override void OnCameraSetup(
            CommandBuffer cmd,
            ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor =
                renderingData.cameraData.cameraTargetDescriptor;

            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(
                ref tempTexture,
                descriptor,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                name: "_PixelationTemp"
            );
        }

        public override void Execute(
            ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (material == null ||
                cameraTarget == null ||
                tempTexture == null)
                return;

            CommandBuffer cmd =
                CommandBufferPool.Get("Screen Pixelation");

            material.SetFloat(
                PixelSizeID,
                pixelSize
            );

            // Copy camera image into temporary texture
            Blitter.BlitCameraTexture(
                cmd,
                cameraTarget,
                tempTexture
            );

            // Apply pixelation and output to camera
            Blitter.BlitCameraTexture(
                cmd,
                tempTexture,
                cameraTarget,
                material,
                0
            );

            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempTexture?.Release();
            tempTexture = null;
        }
    }
}
