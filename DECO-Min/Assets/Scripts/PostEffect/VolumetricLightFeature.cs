
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class VolumetricLightFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Shader shader;

        [Range(8, 128)]
        public int sampleCount = 64;

        [Min(0.1f)]
        public float maxDistance = 30f;

        [Range(0f, 2f)]
        public float density = 0.12f;

        [Range(0f, 10f)]
        public float intensity = 2f;

        [Range(0f, 1f)]
        public float scattering = 0.5f;

        public Color lightTint = Color.white;

        public RenderPassEvent injectionPoint =
            RenderPassEvent.BeforeRenderingPostProcessing;
    }

    [SerializeField]
    private Settings settings = new Settings();

    private Material material;
    private VolumetricLightPass pass;

    public override void Create()
    {
        if (material != null)
        {
            CoreUtils.Destroy(material);
            material = null;
        }

        if (settings.shader == null)
        {
            settings.shader =
                Shader.Find("Hidden/Custom/VolumetricLight");
        }

        if (settings.shader == null)
        {
            return;
        }

        material = CoreUtils.CreateEngineMaterial(settings.shader);

        pass = new VolumetricLightPass(
            material,
            settings
        );

        pass.renderPassEvent = settings.injectionPoint;
        pass.ConfigureInput(ScriptableRenderPassInput.Depth);
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData
    )
    {
        if (pass == null || material == null)
        {
            return;
        }

        if (renderingData.cameraData.cameraType != CameraType.Game)
        {
            return;
        }

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
        material = null;
        pass = null;
    }

    private class VolumetricLightPass : ScriptableRenderPass
    {
        private static readonly int SampleCountID =
            Shader.PropertyToID("_SampleCount");

        private static readonly int MaxDistanceID =
            Shader.PropertyToID("_MaxDistance");

        private static readonly int DensityID =
            Shader.PropertyToID("_Density");

        private static readonly int IntensityID =
            Shader.PropertyToID("_Intensity");

        private static readonly int ScatteringID =
            Shader.PropertyToID("_Scattering");

        private static readonly int LightTintID =
            Shader.PropertyToID("_LightTint");

        private readonly Material material;
        private readonly Settings settings;

        public VolumetricLightPass(
            Material material,
            Settings settings
        )
        {
            this.material = material;
            this.settings = settings;

            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData
        )
        {
            UniversalResourceData resources =
                frameData.Get<UniversalResourceData>();

            UniversalCameraData cameraData =
                frameData.Get<UniversalCameraData>();

            UniversalLightData lightData =
                frameData.Get<UniversalLightData>();

            if (resources.isActiveTargetBackBuffer)
            {
                return;
            }

            if (lightData.mainLightIndex < 0)
            {
                return;
            }

            TextureHandle source = resources.activeColorTexture;

            if (!source.IsValid())
            {
                return;
            }

            RenderTextureDescriptor descriptor =
                cameraData.cameraTargetDescriptor;

            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            TextureHandle temporary =
                UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    descriptor,
                    "_VolumetricLightTemporary",
                    false
                );

            material.SetInt(
                SampleCountID,
                settings.sampleCount
            );

            material.SetFloat(
                MaxDistanceID,
                settings.maxDistance
            );

            material.SetFloat(
                DensityID,
                settings.density
            );

            material.SetFloat(
                IntensityID,
                settings.intensity
            );

            material.SetFloat(
                ScatteringID,
                settings.scattering
            );

            material.SetColor(
                LightTintID,
                settings.lightTint
            );

            var volumetricParameters =
                new RenderGraphUtils.BlitMaterialParameters(
                    source,
                    temporary,
                    material,
                    0
                );

            renderGraph.AddBlitPass(
                volumetricParameters,
                "Volumetric Light"
            );

            var copyParameters =
                new RenderGraphUtils.BlitMaterialParameters(
                    temporary,
                    source,
                    material,
                    1
                );

            renderGraph.AddBlitPass(
                copyParameters,
                "Volumetric Light Composite"
            );
        }
    }
}
