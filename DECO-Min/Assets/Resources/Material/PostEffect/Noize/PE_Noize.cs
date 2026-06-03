using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// PE_Noize用のVolumeComponent。
/// 
/// Global Volume / Local Volume のProfileに追加して使う。
/// CustomPostProcessEffectRendererFeature側は、このVolumeの値を読んでShaderへ渡す。
/// 
/// URPの知識が少ない場合の見方:
/// ・このクラスは「Inspectorに出る設定項目」
/// ・RendererFeatureは「その設定を読んで描画処理を登録する場所」
/// ・Shaderは「実際に画面の色を変える処理」
/// 
/// Shader側のプロパティ名は、このクラスのフィールド名から自動変換される。
/// 
/// 例:
/// intensity              -> _Intensity
/// noiseScale             -> _NoiseScale
/// distortionStrength     -> _DistortionStrength
/// rgbShiftStrength       -> _RgbShiftStrength
/// </summary>
[VolumeComponentMenu("Post-processing Custom/PE_Noize")]
[VolumeRequiresRendererFeatures(typeof(CustomPostProcessEffectRendererFeature))]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
[DisplayInfo(name = "PE_Noize")]
public sealed class PE_Noize : VolumeComponent, IPostProcessComponent
{
    #region MAIN_PARAMETERS

    [Header("Main")]

    /// <summary>
    /// エフェクト全体の強さ。
    /// 
    /// 0の場合はRendererFeature側でPass自体を追加しない。
    /// まず問題が出たら、この値を0にして画面が戻るか確認する。
    /// 
    /// Shader側:
    /// _Intensity
    /// </summary>
    [Tooltip("エフェクト全体の強さ。0なら処理しない。")]
    public ClampedFloatParameter intensity =
        new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

    /// <summary>
    /// 横ラインノイズの細かさ。
    /// 
    /// 大きいほど細かくブレる。
    /// 小さいほど大きな帯としてブレやすい。
    /// 
    /// Shader側:
    /// _NoiseScale
    /// </summary>
    [Tooltip("ノイズの細かさ。大きいほど細かくなる。")]
    public ClampedFloatParameter noiseScale =
        new ClampedFloatParameter(120.0f, 1.0f, 500.0f);

    /// <summary>
    /// ノイズや歪みの動く速さ。
    /// 
    /// 0にすると時間変化しない。
    /// 
    /// Shader側:
    /// _NoiseSpeed
    /// </summary>
    [Tooltip("ノイズの動く速さ。")]
    public ClampedFloatParameter noiseSpeed =
        new ClampedFloatParameter(15.0f, 0.0f, 100.0f);

    #endregion

    #region DISTORTION_PARAMETERS

    [Header("Distortion")]

    /// <summary>
    /// 歪み全体の強さ。
    /// 
    /// 今回欲しい「画面が横にぐにゃっと壊れる感じ」の主役。
    /// 
    /// Shader側:
    /// _DistortionStrength
    /// </summary>
    [Tooltip("歪み全体の強さ。")]
    public ClampedFloatParameter distortionStrength =
        new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

    /// <summary>
    /// なめらかな波歪みの強さ。
    /// 
    /// 画面全体がゆっくり曲がるような歪み。
    /// 
    /// Shader側:
    /// _WaveStrength
    /// </summary>
    [Tooltip("なめらかな波歪みの強さ。")]
    public ClampedFloatParameter waveStrength =
        new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

    /// <summary>
    /// 横ラインごとの細かいズレの強さ。
    /// 
    /// テレビの横同期が崩れるようなブレ。
    /// 
    /// Shader側:
    /// _RowJitterStrength
    /// </summary>
    [Tooltip("横ラインごとのズレの強さ。")]
    public ClampedFloatParameter rowJitterStrength =
        new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

    /// <summary>
    /// 太い帯が横にズレる強さ。
    /// 
    /// グリッチ感を出す主役。
    /// 上げすぎると画面外を読みやすくなる。
    /// 
    /// Shader側:
    /// _BandStrength
    /// </summary>
    [Tooltip("太い帯ズレの強さ。")]
    public ClampedFloatParameter bandStrength =
        new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

    #endregion

    #region COLOR_NOISE_PARAMETERS

    [Header("Color / Noise")]

    /// <summary>
    /// RGBチャンネルのズレ量。
    /// 
    /// 色収差、色ズレの強さ。
    /// 
    /// Shader側:
    /// _RgbShiftStrength
    /// </summary>
    [Tooltip("RGB色ズレの強さ。")]
    public ClampedFloatParameter rgbShiftStrength =
        new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

    /// <summary>
    /// 粒ノイズの強さ。
    /// 
    /// 上げすぎると、歪みではなくただの砂嵐に見えやすい。
    /// 
    /// Shader側:
    /// _GrainStrength
    /// </summary>
    [Tooltip("粒ノイズの強さ。")]
    public ClampedFloatParameter grainStrength =
        new ClampedFloatParameter(0.5f, 0.0f, 5.0f);

    /// <summary>
    /// 走査線の強さ。
    /// 
    /// 横線感を出す。
    /// 
    /// Shader側:
    /// _ScanlineStrength
    /// </summary>
    [Tooltip("走査線の強さ。")]
    public ClampedFloatParameter scanlineStrength =
        new ClampedFloatParameter(0.5f, 0.0f, 5.0f);

    #endregion

    #region POST_PROCESS_COMPONENT

    /// <summary>
    /// このVolumeが有効かどうか。
    /// 
    /// activeはVolumeComponent自体の有効チェック。
    /// intensityが0以下なら、処理自体を走らせない。
    /// </summary>
    public bool IsActive()
    {
        return active && intensity.value > 0.0f;
    }

    /// <summary>
    /// タイル互換かどうか。
    /// 
    /// 画面全体のUVを歪ませる処理なのでfalse。
    /// </summary>
    public bool IsTileCompatible()
    {
        return false;
    }

    #endregion
}