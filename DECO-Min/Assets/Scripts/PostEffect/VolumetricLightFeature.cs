using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class VolumetricLightFeature : ScriptableRendererFeature
{
    public enum DebugMode
    {
        Off = 0,
        LightOnly = 1,
        ShadowVisibility = 2,
        ShadowKeyword = 3,
        SurfaceShadow = 4,
        WorldPosition = 5
    }

    [System.Serializable]
    public class Settings
    {
        [Header("Shader")]
        public Shader shader;

        [Header("Ray Marching")]
        [Range(16, 256)]
        public int sampleCount = 96;

        [Min(0.1f)]
        public float maxDistance = 100f;

        [Header("Scattering")]
        [Range(0.001f, 0.2f)]
        public float density = 0.025f;

        [Range(0f, 20f)]
        public float intensity = 3f;

        [Range(0f, 0.95f)]
        public float anisotropy = 0.9f;

        [Range(0f, 1f)]
        public float viewAngleCompensation = 0.1f;

        public Color lightTint = Color.white;

        [Header("Shadow")]
        [Range(0f, 1f)]
        public float shadowThreshold = 0.775f;

        [Range(0.001f, 1f)]
        public float shadowSoftness = 0.8f;

        [Header("Appearance")]
        [Range(0f, 1f)]
        public float maxLightContribution = 1f;

        [Range(0f, 1f)]
        public float noiseStrength = 0f;

        [Range(0.01f, 10f)]
        public float noiseScale = 0.01f;

        [Header("Debug")]
        public DebugMode debugMode = DebugMode.Off;

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
            settings.shader = Shader.Find(
                "Hidden/Custom/VolumetricLight"
            );
        }

        if (settings.shader == null)
        {
            Debug.LogError(
                "VolumetricLight shader not found."
            );

            pass = null;
            return;
        }

        material = CoreUtils.CreateEngineMaterial(settings.shader);

        pass = new VolumetricLightPass(
            material,
            settings
        );

        pass.renderPassEvent = settings.injectionPoint;

        pass.ConfigureInput(
            ScriptableRenderPassInput.Depth
        );
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

        CameraType cameraType =
            renderingData.cameraData.cameraType;

        if (cameraType != CameraType.Game &&
            cameraType != CameraType.SceneView)
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
        private class PassData
        {
            public TextureHandle source;
            public Material material;
        }

        private readonly Material material;
        private readonly Settings settings;

        private static readonly int SampleCountID =
            Shader.PropertyToID("_SampleCount");

        private static readonly int MaxDistanceID =
            Shader.PropertyToID("_MaxDistance");

        private static readonly int DensityID =
            Shader.PropertyToID("_Density");

        private static readonly int IntensityID =
            Shader.PropertyToID("_Intensity");

        private static readonly int AnisotropyID =
            Shader.PropertyToID("_Anisotropy");

        private static readonly int ViewAngleCompensationID =
            Shader.PropertyToID("_ViewAngleCompensation");

        private static readonly int LightTintID =
            Shader.PropertyToID("_LightTint");

        private static readonly int ShadowThresholdID =
            Shader.PropertyToID("_ShadowThreshold");

        private static readonly int ShadowSoftnessID =
            Shader.PropertyToID("_ShadowSoftness");

        private static readonly int MaxLightContributionID =
            Shader.PropertyToID("_MaxLightContribution");

        private static readonly int NoiseStrengthID =
            Shader.PropertyToID("_NoiseStrength");

        private static readonly int NoiseScaleID =
            Shader.PropertyToID("_NoiseScale");

        private static readonly int DebugModeID =
            Shader.PropertyToID("_DebugMode");

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

            TextureHandle source =
                resources.activeColorTexture;

            TextureHandle shadow =
                resources.mainShadowsTexture;

            if (!source.IsValid() || !shadow.IsValid())
            {
                return;
            }

            RenderTextureDescriptor descriptor =
                cameraData.cameraTargetDescriptor;

            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            TextureHandle destination =
                UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    descriptor,
                    "_VolumetricLightResult",
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
                AnisotropyID,
                settings.anisotropy
            );

            material.SetFloat(
                ViewAngleCompensationID,
                settings.viewAngleCompensation
            );

            material.SetColor(
                LightTintID,
                settings.lightTint
            );

            material.SetFloat(
                ShadowThresholdID,
                settings.shadowThreshold
            );

            material.SetFloat(
                ShadowSoftnessID,
                settings.shadowSoftness
            );

            material.SetFloat(
                MaxLightContributionID,
                settings.maxLightContribution
            );

            material.SetFloat(
                NoiseStrengthID,
                settings.noiseStrength
            );

            material.SetFloat(
                NoiseScaleID,
                settings.noiseScale
            );

            material.SetInt(
                DebugModeID,
                (int)settings.debugMode
            );

            using (var builder =
                renderGraph.AddRasterRenderPass<PassData>(
                    "Volumetric Light",
                    out PassData passData
                ))
            {
                passData.source = source;
                passData.material = material;

                builder.UseTexture(
                    source,
                    AccessFlags.Read
                );

                builder.UseTexture(
                    shadow,
                    AccessFlags.Read
                );

                builder.SetRenderAttachment(
                    destination,
                    0,
                    AccessFlags.Write
                );

                builder.SetRenderFunc(
                    (PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(
                            context.cmd,
                            data.source,
                            new Vector4(1f, 1f, 0f, 0f),
                            data.material,
                            0
                        );
                    }
                );
            }

            resources.cameraColor = destination;
        }
    }
}