using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Unity 6.3.11f / URP 17 / RenderGraph 専用のカスタムポストエフェクトRendererFeature。
/// 
/// このRendererFeatureは、同じスクリプトをURP Renderer Data上に複数追加して使い回せるようにしている。
/// 
/// 例:
/// 1個目:
///     Name                  : Noize
///     Volume Component Name : PE_Noize
///     Material              : PE_Noize用Material
/// 
/// 2個目:
///     Name                  : Bloom
///     Volume Component Name : PP_Bloom
///     Material              : S_PP_Bloom用Material
/// 
/// 方針:
/// ・Enumでエフェクト種類を増やさない。
/// ・VolumeComponentの型名を文字列で指定する。
/// ・指定したVolumeComponentが有効なら、そのMaterialでフルスクリーン描画する。
/// ・VolumeComponent内のFloatParameter / ColorParameterなどを自動でMaterialへ渡す。
/// ・Shader側のプロパティ名は、Volume側のフィールド名から自動変換する。
/// 
/// 例:
/// PE_Noize.cs:
///     public ClampedFloatParameter intensity;
///     public ClampedFloatParameter noiseScale;
/// 
/// Shader:
///     float _Intensity;
///     float _NoiseScale;
/// 
/// 自動変換:
///     intensity  -> _Intensity
///     noiseScale -> _NoiseScale
/// 
/// 注意:
/// Shader側のプロパティ名とVolume側のフィールド名の対応を守る必要がある。
/// </summary>
public sealed class CustomPostProcessEffectRendererFeature : ScriptableRendererFeature
{
    #region FEATURE_FIELDS

    /// <summary>
    /// このRendererFeatureインスタンスが参照するVolumeComponentの型名。
    /// 
    /// 例:
    /// PE_Noize
    /// PP_Bloom
    /// PP_Vignette
    /// 
    /// Namespace付きで書いてもよい。
    /// 例:
    /// Game.Rendering.PE_Noize
    /// </summary>
    [Header("Target Volume")]
    [SerializeField]
    private string m_VolumeComponentName = "PE_Noize";

    /// <summary>
    /// ポストエフェクトに使用するMaterial。
    /// 
    /// このMaterialのShaderは、少なくとも _MainTexture を読む想定。
    /// 
    /// Shader側例:
    ///     TEXTURE2D_X(_MainTexture);
    /// 
    /// C#側から現在の画面テクスチャを _MainTexture として渡す。
    /// </summary>
    [Header("Post Effect Material")]
    [SerializeField]
    private Material m_Material;

    /// <summary>
    /// Shaderに渡す入力画面テクスチャのプロパティ名。
    /// 
    /// 基本は _MainTexture のままでよい。
    /// </summary>
    [SerializeField]
    private string m_MainTexturePropertyName = "_MainTexture";

    /// <summary>
    /// このポストエフェクトを実行するタイミング。
    /// 
    /// 画面全体の最終演出なら AfterRenderingPostProcessing が基本。
    /// 複数RendererFeatureを置く場合は、この値とInspector上の並びで見た目が変わることがある。
    /// </summary>
    [Header("Render Order")]
    [SerializeField]
    private RenderPassEvent m_RenderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    /// <summary>
    /// Play中だけエフェクトをかけるか。
    /// 
    /// true:
    ///     エディタ停止中のGameビューにはかけない。
    /// 
    /// false:
    ///     エディタ停止中のGameビューにもかける。
    /// </summary>
    [Header("Camera Filter")]
    [SerializeField]
    private bool m_ApplyOnlyPlaying = false;

    /// <summary>
    /// SceneViewにもエフェクトをかけるか。
    /// 
    /// false推奨。
    /// SceneViewが歪むと、配置確認、Ray確認、デバッグがやりづらくなる。
    /// </summary>
    [SerializeField]
    private bool m_ApplyToSceneView = false;

    /// <summary>
    /// Unityエディタ内部のPreview用カメラにもエフェクトをかけるか。
    /// 
    /// Previewカメラは、Material、Mesh、Prefab、CameraなどのInspectorプレビューや、
    /// サムネイル生成などで使われることがある。
    /// 
    /// 通常のGameビュー用カメラやSceneビュー用カメラとは別扱い。
    /// 基本はfalse推奨。
    /// </summary>
    [SerializeField]
    private bool m_ApplyToPreviewCamera = false;

    /// <summary>
    /// デバッグログを出すか。
    /// 
    /// trueにすると、VolumeComponentが見つからない場合や、
    /// Shader側に対応するPropertyがない場合にログを出す。
    /// 
    /// 毎フレーム出る可能性があるため、基本はfalse推奨。
    /// </summary>
    [Header("Debug")]
    [SerializeField]
    private bool m_LogWarnings = false;

    /// <summary>
    /// 実際にRenderGraphへPassを登録するクラス。
    /// </summary>
    private CustomPostRenderPass m_FullScreenPass;

    #endregion

    #region FEATURE_METHODS

    /// <summary>
    /// RendererFeature生成時、またはInspectorの値が変わったときに呼ばれる。
    /// 
    /// Materialが未設定ならPassを作らない。
    /// </summary>
    public override void Create()
    {
        if (m_Material == null)
        {
            m_FullScreenPass = null;
            return;
        }

        m_FullScreenPass = new CustomPostRenderPass(
            name,
            m_VolumeComponentName,
            m_Material,
            m_MainTexturePropertyName,
            m_RenderPassEvent,
            m_LogWarnings
        );
    }

    /// <summary>
    /// カメラごとに呼ばれる。
    /// 
    /// ここで、このカメラに対してエフェクトを実行するか判定する。
    /// 実行する場合だけRendererにPassを登録する。
    /// </summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Material == null || m_FullScreenPass == null)
        {
            return;
        }

        if (m_ApplyOnlyPlaying && !Application.isPlaying)
        {
            return;
        }

        CameraData cameraData = renderingData.cameraData;

        if (cameraData.cameraType == CameraType.Reflection)
        {
            return;
        }

        if (!m_ApplyToPreviewCamera && cameraData.cameraType == CameraType.Preview)
        {
            return;
        }

        if (!m_ApplyToSceneView && cameraData.isSceneViewCamera)
        {
            return;
        }

        VolumeComponent volumeComponent =
            VolumeComponentUtility.GetVolumeComponentByName(m_VolumeComponentName, m_LogWarnings);

        if (!VolumeComponentUtility.IsVolumeComponentActive(volumeComponent))
        {
            return;
        }

        m_FullScreenPass.SetSettings(
            name,
            m_VolumeComponentName,
            m_Material,
            m_MainTexturePropertyName,
            m_RenderPassEvent,
            m_LogWarnings
        );

        m_FullScreenPass.renderPassEvent = m_RenderPassEvent;
        m_FullScreenPass.ConfigureInput(ScriptableRenderPassInput.None);

        renderer.EnqueuePass(m_FullScreenPass);
    }

    /// <summary>
    /// RendererFeature破棄時に呼ばれる。
    /// 
    /// Inspectorから渡されたMaterialはここでDestroyしない。
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (m_FullScreenPass != null)
        {
            m_FullScreenPass.Dispose();
            m_FullScreenPass = null;
        }
    }

#if UNITY_EDITOR

    /// <summary>
    /// Inspectorの値が変わったときに呼ばれる。
    /// 
    /// Material、VolumeComponent名、RenderPassEventなどの変更を反映しやすくする。
    /// </summary>
    private void OnValidate()
    {
        Create();
    }

#endif

    #endregion

    /// <summary>
    /// RenderGraph専用のフルスクリーンポストエフェクトPass。
    /// 
    /// このPassは、指定されたVolumeComponent名とMaterialを使って、
    /// 画面全体へポストエフェクトをかける。
    /// </summary>
    private sealed class CustomPostRenderPass : ScriptableRenderPass
    {
        #region PASS_FIELDS

        /// <summary>
        /// RenderGraph上で表示されるPass名。
        /// </summary>
        private string m_PassName;

        /// <summary>
        /// 対象VolumeComponentの型名。
        /// 例: PE_Noize
        /// </summary>
        private string m_VolumeComponentName;

        /// <summary>
        /// 描画に使うMaterial。
        /// </summary>
        private Material m_Material;

        /// <summary>
        /// Shaderに入力画面を渡すためのプロパティ名。
        /// 基本は _MainTexture。
        /// </summary>
        private string m_MainTexturePropertyName;

        /// <summary>
        /// _MainTextureなどのShader property id。
        /// </summary>
        private int m_MainTexturePropertyId;

        /// <summary>
        /// 警告ログを出すか。
        /// </summary>
        private bool m_LogWarnings;

        #endregion

        #region PASS_DATA

        /// <summary>
        /// RenderGraphの実行関数に渡すデータ。
        /// </summary>
        private sealed class PassData
        {
            public TextureHandle src;
            public TextureHandle dst;

            public Material material;

            public string volumeComponentName;

            public int mainTexturePropertyId;

            public bool logWarnings;
        }

        #endregion

        #region PASS_CONSTRUCTOR

        /// <summary>
        /// RenderPassのコンストラクタ。
        /// </summary>
        public CustomPostRenderPass(
            string passName,
            string volumeComponentName,
            Material material,
            string mainTexturePropertyName,
            RenderPassEvent renderPassEvent,
            bool logWarnings
        )
        {
            SetSettings(
                passName,
                volumeComponentName,
                material,
                mainTexturePropertyName,
                renderPassEvent,
                logWarnings
            );

            profilingSampler = new ProfilingSampler(passName);

            this.renderPassEvent = renderPassEvent;

            // 画面カラーを入力として読むため、中間テクスチャを要求する。
            requiresIntermediateTexture = true;
        }

        /// <summary>
        /// Inspector変更などを反映するための設定更新。
        /// </summary>
        public void SetSettings(
            string passName,
            string volumeComponentName,
            Material material,
            string mainTexturePropertyName,
            RenderPassEvent renderPassEvent,
            bool logWarnings
        )
        {
            m_PassName = string.IsNullOrEmpty(passName)
                ? "CustomPostProcessPass"
                : passName;

            m_VolumeComponentName = volumeComponentName;
            m_Material = material;

            m_MainTexturePropertyName = string.IsNullOrEmpty(mainTexturePropertyName)
                ? "_MainTexture"
                : mainTexturePropertyName;

            m_MainTexturePropertyId = Shader.PropertyToID(m_MainTexturePropertyName);

            m_LogWarnings = logWarnings;

            this.renderPassEvent = renderPassEvent;
        }

        #endregion

        #region PASS_RENDER_GRAPH

        /// <summary>
        /// RenderGraphへPassを登録する。
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null)
            {
                return;
            }

            VolumeComponent volumeComponent =
                VolumeComponentUtility.GetVolumeComponentByName(m_VolumeComponentName, m_LogWarnings);

            if (!VolumeComponentUtility.IsVolumeComponentActive(volumeComponent))
            {
                return;
            }

            UniversalResourceData resources =
                frameData.Get<UniversalResourceData>();

            TextureHandle src = resources.activeColorTexture;

            if (!src.IsValid())
            {
                return;
            }

            TextureDesc dstDesc = renderGraph.GetTextureDesc(src);
            dstDesc.name = "_CustomPostProcess_" + m_PassName;
            dstDesc.clearBuffer = false;

            TextureHandle dst = renderGraph.CreateTexture(dstDesc);

            using (IRasterRenderGraphBuilder builder =
                   renderGraph.AddRasterRenderPass<PassData>(
                       m_PassName,
                       out PassData passData,
                       profilingSampler
                   ))
            {
                passData.src = src;
                passData.dst = dst;

                passData.material = m_Material;

                passData.volumeComponentName = m_VolumeComponentName;

                passData.mainTexturePropertyId = m_MainTexturePropertyId;

                passData.logWarnings = m_LogWarnings;

                builder.UseTexture(src, AccessFlags.Read);
                builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

                // SetGlobalTextureを使うために必要。
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(
                    (PassData data, RasterGraphContext context) =>
                    {
                        ExecutePass(data, context);
                    }
                );

                // 以降のPassがこの結果を現在のcameraColorとして扱う。
                resources.cameraColor = dst;
            }
        }

        /// <summary>
        /// RenderGraphのRasterPass実行時に呼ばれる描画処理。
        /// </summary>
        private static void ExecutePass(PassData data, RasterGraphContext context)
        {
            if (data == null)
            {
                return;
            }

            if (data.material == null)
            {
                return;
            }

            if (!data.src.IsValid())
            {
                return;
            }

            VolumeComponent volumeComponent =
                VolumeComponentUtility.GetVolumeComponentByName(data.volumeComponentName, data.logWarnings);

            if (!VolumeComponentUtility.IsVolumeComponentActive(volumeComponent))
            {
                return;
            }

            // VolumeComponent内のVolumeParameterをMaterialへ自動反映する。
            VolumeComponentUtility.ApplyVolumeParametersToMaterial(
                volumeComponent,
                data.material,
                data.logWarnings
            );

            // 現在の画面をShaderへ渡す。
            // Shader側では _MainTexture を読む。
            context.cmd.SetGlobalTexture(data.mainTexturePropertyId, data.src);

            // 現在のRenderAttachment、つまりdstへフルスクリーン描画する。
            CoreUtils.DrawFullScreen(context.cmd, data.material);
        }

        #endregion

        #region PASS_DISPOSE

        /// <summary>
        /// RenderPass破棄時に呼ばれる。
        /// 
        /// Inspectorから渡されたMaterialを使っているため、ここでDestroyしない。
        /// </summary>
        public void Dispose()
        {
        }

        #endregion
    }
}

/// <summary>
/// VolumeComponentを名前で探したり、
/// VolumeComponent内のVolumeParameterをMaterialへ渡すための補助クラス。
/// 
/// このクラスは、同じRendererFeatureを複数追加して、
/// Volume Component Nameだけ変える運用をするために使う。
/// </summary>
internal static class VolumeComponentUtility
{
    #region UTILITY_FIELDS

    /// <summary>
    /// VolumeComponentの型検索結果をキャッシュする。
    /// 毎回Assemblyを全部走査しないようにするため。
    /// </summary>
    private static readonly Dictionary<string, Type> s_VolumeComponentTypeCache =
        new Dictionary<string, Type>();

    /// <summary>
    /// VolumeStack.GetComponent<T>() のMethodInfo。
    /// Reflectionでジェネリックメソッドを呼ぶために使う。
    /// </summary>
    private static MethodInfo s_GetComponentGenericMethod;

    #endregion

    #region COMPONENT_FIND

    /// <summary>
    /// VolumeManagerの現在のStackから、型名でVolumeComponentを探す。
    /// 
    /// 例:
    /// componentName = "PE_Noize"
    /// componentName = "Game.Rendering.PE_Noize"
    /// </summary>
    public static VolumeComponent GetVolumeComponentByName(string componentName, bool logWarnings)
    {
        if (string.IsNullOrEmpty(componentName))
        {
            if (logWarnings)
            {
                Debug.LogWarning("[VolumeComponentUtility] Volume Component Name が空です。");
            }

            return null;
        }

        VolumeStack stack = VolumeManager.instance.stack;

        if (stack == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("[VolumeComponentUtility] VolumeStack が取得できません。");
            }

            return null;
        }

        Type targetType = FindVolumeComponentType(componentName, logWarnings);

        if (targetType == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning(
                    $"[VolumeComponentUtility] VolumeComponent型が見つかりません: {componentName}"
                );
            }

            return null;
        }

        MethodInfo getComponentMethod = GetVolumeStackGenericGetComponentMethod();

        if (getComponentMethod == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("[VolumeComponentUtility] VolumeStack.GetComponent<T>() が見つかりません。");
            }

            return null;
        }

        MethodInfo typedMethod = getComponentMethod.MakeGenericMethod(targetType);

        object result = typedMethod.Invoke(stack, null);

        return result as VolumeComponent;
    }

    /// <summary>
    /// 指定した名前に一致するVolumeComponent型を探す。
    /// </summary>
    private static Type FindVolumeComponentType(string componentName, bool logWarnings)
    {
        if (s_VolumeComponentTypeCache.TryGetValue(componentName, out Type cachedType))
        {
            return cachedType;
        }

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
        {
            Assembly assembly = assemblies[assemblyIndex];

            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                continue;
            }

            for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
            {
                Type type = types[typeIndex];

                if (type == null)
                {
                    continue;
                }

                if (!typeof(VolumeComponent).IsAssignableFrom(type))
                {
                    continue;
                }

                if (type.Name == componentName || type.FullName == componentName)
                {
                    s_VolumeComponentTypeCache.Add(componentName, type);
                    return type;
                }
            }
        }

        if (logWarnings)
        {
            Debug.LogWarning(
                $"[VolumeComponentUtility] VolumeComponent型検索に失敗しました: {componentName}"
            );
        }

        return null;
    }

    /// <summary>
    /// VolumeStack.GetComponent<T>() のMethodInfoを取得する。
    /// </summary>
    private static MethodInfo GetVolumeStackGenericGetComponentMethod()
    {
        if (s_GetComponentGenericMethod != null)
        {
            return s_GetComponentGenericMethod;
        }

        MethodInfo[] methods = typeof(VolumeStack).GetMethods(
            BindingFlags.Instance |
            BindingFlags.Public
        );

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (method.Name != "GetComponent")
            {
                continue;
            }

            if (!method.IsGenericMethodDefinition)
            {
                continue;
            }

            if (method.GetParameters().Length != 0)
            {
                continue;
            }

            s_GetComponentGenericMethod = method;
            return s_GetComponentGenericMethod;
        }

        return null;
    }

    #endregion

    #region ACTIVE_CHECK

    /// <summary>
    /// VolumeComponentが有効かどうかを判定する。
    /// 
    /// IPostProcessComponentを実装している場合は IsActive() を使う。
    /// 実装していない場合は active を見る。
    /// </summary>
    public static bool IsVolumeComponentActive(VolumeComponent component)
    {
        if (component == null)
        {
            return false;
        }

        if (!component.active)
        {
            return false;
        }

        if (component is IPostProcessComponent postProcessComponent)
        {
            return postProcessComponent.IsActive();
        }

        return true;
    }

    #endregion

    #region APPLY_PARAMETERS

    /// <summary>
    /// VolumeComponent内のVolumeParameterをMaterialへ自動で渡す。
    /// 
    /// 例:
    /// public ClampedFloatParameter intensity;
    ///     ↓
    /// Shader property:
    /// _Intensity
    /// 
    /// public ClampedFloatParameter noiseScale;
    ///     ↓
    /// Shader property:
    /// _NoiseScale
    /// </summary>
    public static void ApplyVolumeParametersToMaterial(
        VolumeComponent component,
        Material material,
        bool logWarnings
    )
    {
        if (component == null || material == null)
        {
            return;
        }

        Type componentType = component.GetType();

        FieldInfo[] fields = componentType.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];

            if (!typeof(VolumeParameter).IsAssignableFrom(field.FieldType))
            {
                continue;
            }

            object fieldValue = field.GetValue(component);

            if (!(fieldValue is VolumeParameter parameter))
            {
                continue;
            }

            string shaderPropertyName =
                ConvertFieldNameToShaderPropertyName(field.Name);

            ApplyParameterToMaterial(
                material,
                shaderPropertyName,
                parameter,
                logWarnings
            );
        }
    }

    /// <summary>
    /// C#側のフィールド名をShader側のプロパティ名へ変換する。
    /// 
    /// intensity              -> _Intensity
    /// noiseScale             -> _NoiseScale
    /// distortionStrength     -> _DistortionStrength
    /// rgbShiftStrength       -> _RgbShiftStrength
    /// 
    /// ルール:
    /// ・先頭に _ を付ける。
    /// ・先頭文字だけ大文字にする。
    /// ・camelCaseはそのまま維持する。
    /// </summary>
    private static string ConvertFieldNameToShaderPropertyName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return string.Empty;
        }

        char firstChar = char.ToUpperInvariant(fieldName[0]);

        if (fieldName.Length == 1)
        {
            return "_" + firstChar;
        }

        return "_" + firstChar + fieldName.Substring(1);
    }

    /// <summary>
    /// VolumeParameterの型に応じてMaterialへ値を渡す。
    /// </summary>
    private static void ApplyParameterToMaterial(
        Material material,
        string propertyName,
        VolumeParameter parameter,
        bool logWarnings
    )
    {
        if (material == null || parameter == null)
        {
            return;
        }

        int propertyId = Shader.PropertyToID(propertyName);

        if (!material.HasProperty(propertyId))
        {
            if (logWarnings)
            {
                Debug.LogWarning(
                    $"[VolumeComponentUtility] MaterialにShaderPropertyがありません: {propertyName}"
                );
            }

            return;
        }

        if (parameter is FloatParameter floatParameter)
        {
            material.SetFloat(propertyId, floatParameter.value);
            return;
        }

        if (parameter is IntParameter intParameter)
        {
            material.SetInt(propertyId, intParameter.value);
            return;
        }

        if (parameter is BoolParameter boolParameter)
        {
            material.SetFloat(propertyId, boolParameter.value ? 1.0f : 0.0f);
            return;
        }

        if (parameter is ColorParameter colorParameter)
        {
            material.SetColor(propertyId, colorParameter.value);
            return;
        }

        if (parameter is Vector2Parameter vector2Parameter)
        {
            material.SetVector(propertyId, vector2Parameter.value);
            return;
        }

        if (parameter is Vector3Parameter vector3Parameter)
        {
            material.SetVector(propertyId, vector3Parameter.value);
            return;
        }

        if (parameter is Vector4Parameter vector4Parameter)
        {
            material.SetVector(propertyId, vector4Parameter.value);
            return;
        }

        if (logWarnings)
        {
            Debug.LogWarning(
                $"[VolumeComponentUtility] 未対応のVolumeParameter型です: {parameter.GetType().Name}"
            );
        }
    }

    #endregion
}