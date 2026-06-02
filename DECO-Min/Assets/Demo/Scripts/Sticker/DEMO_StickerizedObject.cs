using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Added to a FieldObject when it becomes fully stickered.
/// Player holds E near it → peel animation → reward stickers acquired.
/// The stickers applied TO the object stay there permanently.
/// </summary>
public class DEMO_StickerizedObject : MonoBehaviour
{
    [Header("Peel settings")]
    [SerializeField] private float peelDuration       = 1.0f;
    [SerializeField] private float peelCancelDistance = 4.0f;

    [Header("Effect")]
    [SerializeField] private GameObject peelCompleteEffectPrefab;

    [HideInInspector] public UnityEvent OnPeelStarted   = new();
    [HideInInspector] public UnityEvent OnPeelCancelled = new();
    [HideInInspector] public UnityEvent OnPeelCompleted = new();

    public bool  IsBeingPeeled { get; private set; }
    public float PeelProgress  { get; private set; }

    private DEMO_StickerData rewardSticker;
    private int         rewardCount;
    private string      displayName = "Object";
    private Coroutine   peelCoroutine;
    private Vector3     originalScale;

    public void Initialize(DEMO_StickerData reward, int count, string name)
    {
        rewardSticker = reward;
        rewardCount   = count;
        displayName   = name;
        originalScale = transform.localScale;
        Debug.Log($"[StickerizedObject] Init '{name}'  reward={reward?.stickerName ?? "NULL"} x{count}");
    }

    public void StartPeeling(DEMO_PlayerController player)
    {
        if (IsBeingPeeled) return;
        Debug.Log($"[StickerizedObject] Peel started: '{displayName}'");
        IsBeingPeeled = true;
        PeelProgress  = 0f;
        peelCoroutine = StartCoroutine(PeelRoutine(player));
        OnPeelStarted.Invoke();
    }

    private IEnumerator PeelRoutine(DEMO_PlayerController player)
    {
        float elapsed = 0f;
        while (elapsed < peelDuration)
        {
            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist > peelCancelDistance) { CancelPeel(); yield break; }

            elapsed     += Time.deltaTime;
            PeelProgress = elapsed / peelDuration;

            float wobble = Mathf.Sin(elapsed * 20f) * 0.05f * PeelProgress;
            transform.localScale = originalScale * (1f + wobble);

            DEMO_UIManager.Instance?.NotifyPeelProgress(PeelProgress, displayName);
            yield return null;
        }
        CompletePeel();
    }

    private void CompletePeel()
    {
        IsBeingPeeled        = false;
        PeelProgress         = 1f;
        transform.localScale = originalScale;

        var sb = DEMO_StickerBook.Instance;
        if (sb != null && rewardSticker != null)
        {
            sb.AddSticker(rewardSticker, rewardCount);
            Debug.Log($"[StickerizedObject] Got reward: {rewardSticker.stickerName} x{rewardCount}");
        }

        if (peelCompleteEffectPrefab != null)
            Instantiate(peelCompleteEffectPrefab, transform.position, Quaternion.identity);

        DEMO_UIManager.Instance?.NotifyPeelProgress(-1f, "");
        OnPeelCompleted.Invoke();
        Destroy(gameObject);
    }

    public void CancelPeel()
    {
        if (peelCoroutine != null) StopCoroutine(peelCoroutine);
        IsBeingPeeled        = false;
        PeelProgress         = 0f;
        transform.localScale = originalScale;
        DEMO_UIManager.Instance?.NotifyPeelProgress(-1f, "");
        OnPeelCancelled.Invoke();
    }
}
