using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    [SerializeField] private Image _fadeImage;
    [SerializeField] private float _fadeTime = 1f;

    public static FadeManager Instance { get; private set; }

    public bool isFading { get; private set; }
    public bool isBlackScreen { get; private set; }

    private Coroutine _fadeCoroutine;
    private void Awake()
    {
        // シングルトン化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Color alpha = _fadeImage.color;
        alpha.a = 1f;
        _fadeImage.color = alpha;

    }

    private void Start()
    {
        // 仮
        FadeIn();
    }

    public void FadeIn()
    {
        StartFade(1, 0);
    }
    public void FadeOut()
    {
        StartFade(0, 1);
    }

    private void StartFade(float startAlpha,float endAlpha)
    {
        // Fadeルーチンが呼ばれていたら一旦止める
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeRoutine(startAlpha, endAlpha));

    }
    private IEnumerator FadeRoutine(float startAlpha,float endAlpha)
    {
        isFading = true;

        // Fade開始のalphaにする
        Color alpha = _fadeImage.color;
        alpha.a = startAlpha;
        _fadeImage.color = alpha;

        float time = 0f;

        // 設定した時間の間フェードする
        while (time < _fadeTime)
        {
            // TimeScaleに依存しない個別のTime
            time += Time.unscaledDeltaTime;

            // Fade時間の割合で透明度を変更
            alpha.a = Mathf.Lerp(startAlpha, endAlpha, time / _fadeTime);
            _fadeImage.color = alpha;

            yield return null;
        }
        alpha.a = endAlpha;
        _fadeImage.color = alpha;
        isFading = false;
    }
}
