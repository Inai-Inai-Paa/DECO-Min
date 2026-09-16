using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤー自身のHPをハートアイコンで表示するUI。
/// </summary>
public class UIPlayerHealthGauge : UIPlayerBase
{
    [SerializeField] private UIHealthHeart[] _hearts;

    [Header("Wave")]
    [SerializeField] private float _waveDelay = 0.05f;
    [SerializeField] private bool _loop = false;
    [SerializeField] private float _loopInterval = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool _debugHealth = false;

    private int _previousHalfHeartCount = -1;
    private Coroutine _waveCoroutine;

    protected override void Start()
    {
        base.Start();

        if (_hearts == null || _hearts.Length == 0)
        {
            Debug.LogWarning($"{nameof(UIPlayerHealthGauge)} : ハートUIが設定されていません。");
            return;
        }

        if (_loop)
        {
            _waveCoroutine = StartCoroutine(
                PlayWave()
            );
        }
    }

    private void Update()
    {
        if (!IsValidPlayerStatus())
        {
            return;
        }

#if UNITY_EDITOR
        if (_debugHealth)
        {
            DebugHealth();
        }
#endif

        if (_hearts == null || _hearts.Length == 0)
        {
            return;
        }

        float healthRate = 0.0f;

        if (_playerStatus.maxHealth > 0.0f)
        {
            healthRate = Mathf.Clamp01(
                _playerStatus.currentHealth / _playerStatus.maxHealth
            );
        }

        int halfHeartCount = Mathf.FloorToInt(
            healthRate * _hearts.Length * 2.0f
        );

        if (_previousHalfHeartCount == halfHeartCount)
        {
            return;
        }

        for (int i = 0; i < _hearts.Length; i++)
        {
            int heartHalfCount = Mathf.Clamp(
                halfHeartCount - i * 2,
                0,
                2
            );

            _hearts[i].SetHealth(
                heartHalfCount * 0.5f
            );
        }

        if (!_loop && _previousHalfHeartCount >= 0)
        {
            if (_waveCoroutine != null)
            {
                StopCoroutine(_waveCoroutine);
            }

            _waveCoroutine = StartCoroutine(
                PlayWave()
            );
        }

        _previousHalfHeartCount = halfHeartCount;
    }
#if UNITY_EDITOR
    private void DebugHealth()
    {
        if (Mouse.current == null)
        {
            return;
        }

        float halfHeartHealth =
            _playerStatus.maxHealth / (_hearts.Length * 2.0f);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _playerStatus.currentHealth -= halfHeartHealth;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            _playerStatus.currentHealth -= halfHeartHealth * 2.0f;
        }

        if (Mouse.current.scroll.ReadValue().y > 0.0f)
        {
            _playerStatus.currentHealth += halfHeartHealth;
        }

        _playerStatus.currentHealth = Mathf.Clamp(
            _playerStatus.currentHealth,
            0.0f,
            _playerStatus.maxHealth
        );
    }
#endif

    private IEnumerator PlayWave()
    {
        do
        {
            for (int i = 0; i < _hearts.Length; i++)
            {
                _hearts[i].PlayAnimation();

                yield return new WaitForSeconds(
                    _waveDelay
                );
            }

            if (_loop && _loopInterval > 0.0f)
            {
                yield return new WaitForSeconds(
                    _loopInterval
                );
            }
        }
        while (_loop);

        _waveCoroutine = null;
    }
}