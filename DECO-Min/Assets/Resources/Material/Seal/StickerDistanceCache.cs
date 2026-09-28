using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public sealed class StickerDistanceCache : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Optional per-object override. Leave null to use the material's Sticker Texture.")]
    [SerializeField]
    private Texture2D stickerTexture;

    [Header("Height Precompute")]
    [Tooltip("Compute shader that builds a globally normalized 0-1 height field from the alpha mask.")]
    [SerializeField]
    private ComputeShader distanceCompute;

    private static readonly int MainTexId =
        Shader.PropertyToID("_MainTex");

    private static readonly int HeightTexId =
        Shader.PropertyToID("_HeightTex");

    private static readonly int AlphaCutoffId =
        Shader.PropertyToID("_AlphaCutoff");

    private static readonly int SourceId =
        Shader.PropertyToID("_Source");

    private static readonly int SeedReadId =
        Shader.PropertyToID("_SeedRead");

    private static readonly int SeedWriteId =
        Shader.PropertyToID("_SeedWrite");

    private static readonly int DistanceSqId =
        Shader.PropertyToID("_DistanceSq");

    private static readonly int DistanceSqReadId =
        Shader.PropertyToID("_DistanceSqRead");

    private static readonly int MaxDistanceSqId =
        Shader.PropertyToID("_MaxDistanceSq");

    private static readonly int WidthId =
        Shader.PropertyToID("_Width");

    private static readonly int HeightId =
        Shader.PropertyToID("_Height");

    private static readonly int JumpStepId =
        Shader.PropertyToID("_JumpStep");

    private static readonly int AlphaCutoffComputeId =
        Shader.PropertyToID("_AlphaCutoff");

    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private RenderTexture heightTexture;

    private Texture2D generatedFromTexture;
    private float generatedAlphaCutoff = -1.0f;

    private int initSeedsKernel = -1;
    private int jumpFloodKernel = -1;
    private int computeDistanceKernel = -1;
    private int normalizeHeightKernel = -1;

    private void OnEnable()
    {
        CacheReferences();
        RebuildIfNeeded();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        CacheReferences();
        InvalidateKernelCache();
        RebuildIfNeeded();
    }

    private void Update()
    {
        RebuildIfNeeded();
    }

    public void SetTexture(Texture2D texture)
    {
        if (stickerTexture == texture)
            return;

        stickerTexture = texture;
        Rebuild();
    }

    public void ClearTextureOverride()
    {
        if (stickerTexture == null)
            return;

        stickerTexture = null;
        Rebuild();
    }

    public void RebuildIfNeeded()
    {
        CacheReferences();

        Texture2D sourceTexture = ResolveSourceTexture();

        if (sourceTexture == null || distanceCompute == null)
        {
            ReleaseHeightTexture();
            return;
        }

        float alphaCutoff = ResolveAlphaCutoff();

        bool needsRebuild =
            heightTexture == null ||
            generatedFromTexture != sourceTexture ||
            !Mathf.Approximately(generatedAlphaCutoff, alphaCutoff) ||
            heightTexture.width != sourceTexture.width ||
            heightTexture.height != sourceTexture.height;

        if (needsRebuild)
            Rebuild(sourceTexture, alphaCutoff);
        else
            ApplyTextures(sourceTexture);
    }

    public void Rebuild()
    {
        CacheReferences();

        Texture2D sourceTexture = ResolveSourceTexture();

        if (sourceTexture == null || distanceCompute == null)
            return;

        Rebuild(sourceTexture, ResolveAlphaCutoff());
    }

    private void Rebuild(Texture2D sourceTexture, float alphaCutoff)
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("Volumetric sticker height precompute requires compute shader support.", this);
            return;
        }

        if (!CacheKernels())
            return;

        ReleaseHeightTexture();

        int width = sourceTexture.width;
        int height = sourceTexture.height;

        heightTexture = CreateTexture(
            width,
            height,
            GraphicsFormat.R32_SFloat,
            $"{name}_StickerHeight",
            FilterMode.Bilinear
        );

        RenderTexture seedA = CreateTexture(
            width,
            height,
            GraphicsFormat.R32G32_SFloat,
            $"{name}_StickerSeedA",
            FilterMode.Point
        );

        RenderTexture seedB = CreateTexture(
            width,
            height,
            GraphicsFormat.R32G32_SFloat,
            $"{name}_StickerSeedB",
            FilterMode.Point
        );

        RenderTexture distanceSqTexture = CreateTexture(
            width,
            height,
            GraphicsFormat.R32_SFloat,
            $"{name}_StickerDistanceSq",
            FilterMode.Point
        );

        ComputeBuffer maxDistanceBuffer = new ComputeBuffer(1, sizeof(uint));
        maxDistanceBuffer.SetData(new uint[] { 0u });

        try
        {
            int groupX = Mathf.CeilToInt(width / 8.0f);
            int groupY = Mathf.CeilToInt(height / 8.0f);

            SetCommonParameters(sourceTexture, width, height, alphaCutoff);

            distanceCompute.SetTexture(initSeedsKernel, SourceId, sourceTexture);
            distanceCompute.SetTexture(initSeedsKernel, SeedWriteId, seedA);
            distanceCompute.Dispatch(initSeedsKernel, groupX, groupY, 1);

            RenderTexture seedRead = seedA;
            RenderTexture seedWrite = seedB;

            int jumpStep = Mathf.NextPowerOfTwo(Mathf.Max(width, height)) >> 1;
            jumpStep = Mathf.Max(jumpStep, 1);

            while (jumpStep >= 1)
            {
                DispatchJumpFlood(seedRead, seedWrite, jumpStep, groupX, groupY);
                Swap(ref seedRead, ref seedWrite);
                jumpStep >>= 1;
            }

            // One extra step-1 pass (JFA+1) reduces the small nearest-seed errors
            // that plain jump flooding can leave around complex silhouettes.
            DispatchJumpFlood(seedRead, seedWrite, 1, groupX, groupY);
            Swap(ref seedRead, ref seedWrite);

            distanceCompute.SetTexture(computeDistanceKernel, SourceId, sourceTexture);
            distanceCompute.SetTexture(computeDistanceKernel, SeedReadId, seedRead);
            distanceCompute.SetTexture(computeDistanceKernel, DistanceSqId, distanceSqTexture);
            distanceCompute.SetBuffer(computeDistanceKernel, MaxDistanceSqId, maxDistanceBuffer);
            distanceCompute.Dispatch(computeDistanceKernel, groupX, groupY, 1);

            distanceCompute.SetTexture(normalizeHeightKernel, SourceId, sourceTexture);
            distanceCompute.SetTexture(normalizeHeightKernel, DistanceSqReadId, distanceSqTexture);
            distanceCompute.SetTexture(normalizeHeightKernel, HeightTexId, heightTexture);
            distanceCompute.SetBuffer(normalizeHeightKernel, MaxDistanceSqId, maxDistanceBuffer);
            distanceCompute.Dispatch(normalizeHeightKernel, groupX, groupY, 1);
        }
        finally
        {
            maxDistanceBuffer.Release();
            DestroyRenderTexture(seedA);
            DestroyRenderTexture(seedB);
            DestroyRenderTexture(distanceSqTexture);
        }

        generatedFromTexture = sourceTexture;
        generatedAlphaCutoff = alphaCutoff;

        ApplyTextures(sourceTexture);
    }

    private void SetCommonParameters(
        Texture2D sourceTexture,
        int width,
        int height,
        float alphaCutoff
    )
    {
        distanceCompute.SetTexture(initSeedsKernel, SourceId, sourceTexture);
        distanceCompute.SetInt(WidthId, width);
        distanceCompute.SetInt(HeightId, height);
        distanceCompute.SetFloat(AlphaCutoffComputeId, alphaCutoff);
    }

    private void DispatchJumpFlood(
        RenderTexture seedRead,
        RenderTexture seedWrite,
        int jumpStep,
        int groupX,
        int groupY
    )
    {
        distanceCompute.SetInt(JumpStepId, jumpStep);
        distanceCompute.SetTexture(jumpFloodKernel, SeedReadId, seedRead);
        distanceCompute.SetTexture(jumpFloodKernel, SeedWriteId, seedWrite);
        distanceCompute.Dispatch(jumpFloodKernel, groupX, groupY, 1);
    }

    private bool CacheKernels()
    {
        if (distanceCompute == null)
            return false;

        if (initSeedsKernel >= 0 &&
            jumpFloodKernel >= 0 &&
            computeDistanceKernel >= 0 &&
            normalizeHeightKernel >= 0)
        {
            return true;
        }

        if (!distanceCompute.HasKernel("InitSeeds") ||
            !distanceCompute.HasKernel("JumpFlood") ||
            !distanceCompute.HasKernel("ComputeDistanceAndMax") ||
            !distanceCompute.HasKernel("NormalizeHeight"))
        {
            Debug.LogError(
                "StickerDistance.compute is missing one or more required kernels.",
                this
            );
            return false;
        }

        initSeedsKernel = distanceCompute.FindKernel("InitSeeds");
        jumpFloodKernel = distanceCompute.FindKernel("JumpFlood");
        computeDistanceKernel = distanceCompute.FindKernel("ComputeDistanceAndMax");
        normalizeHeightKernel = distanceCompute.FindKernel("NormalizeHeight");
        return true;
    }

    private void InvalidateKernelCache()
    {
        initSeedsKernel = -1;
        jumpFloodKernel = -1;
        computeDistanceKernel = -1;
        normalizeHeightKernel = -1;
    }

    private static void Swap(ref RenderTexture a, ref RenderTexture b)
    {
        RenderTexture temporary = a;
        a = b;
        b = temporary;
    }

    private static RenderTexture CreateTexture(
        int width,
        int height,
        GraphicsFormat format,
        string textureName,
        FilterMode filterMode
    )
    {
        RenderTextureDescriptor descriptor =
            new RenderTextureDescriptor(width, height)
            {
                graphicsFormat = format,
                depthBufferBits = 0,
                msaaSamples = 1,
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false,
                sRGB = false
            };

        RenderTexture texture = new RenderTexture(descriptor)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = filterMode
        };

        texture.Create();
        return texture;
    }

    private void CacheReferences()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
    }

    private Texture2D ResolveSourceTexture()
    {
        if (stickerTexture != null)
            return stickerTexture;

        if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            return null;

        return targetRenderer.sharedMaterial.GetTexture(MainTexId) as Texture2D;
    }

    private float ResolveAlphaCutoff()
    {
        if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            return 0.5f;

        Material material = targetRenderer.sharedMaterial;

        if (!material.HasProperty(AlphaCutoffId))
            return 0.5f;

        return material.GetFloat(AlphaCutoffId);
    }

    private void ApplyTextures(Texture2D sourceTexture)
    {
        CacheReferences();

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(MainTexId, sourceTexture);
        propertyBlock.SetTexture(HeightTexId, heightTexture);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDisable()
    {
        ReleaseHeightTexture();
    }

    private void OnDestroy()
    {
        ReleaseHeightTexture();
    }

    private void ReleaseHeightTexture()
    {
        if (heightTexture != null)
        {
            heightTexture.Release();
            DestroyObject(heightTexture);
            heightTexture = null;
        }

        generatedFromTexture = null;
        generatedAlphaCutoff = -1.0f;
    }

    private static void DestroyRenderTexture(RenderTexture texture)
    {
        if (texture == null)
            return;

        texture.Release();
        DestroyObject(texture);
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Object.DestroyImmediate(target);
        else
#endif
            Object.Destroy(target);
    }
}
