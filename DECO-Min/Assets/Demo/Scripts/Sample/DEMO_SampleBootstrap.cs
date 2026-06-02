using System.Collections;
using UnityEngine;

/// <summary>
/// Creates sticker data and starts exploration.
/// Uses a coroutine (WaitForEndOfFrame x2) instead of Invoke to guarantee
/// ALL other Start() methods have completed before StartExploration is called.
/// </summary>
public class DEMO_SampleBootstrap : MonoBehaviour
{
    [SerializeField] private int  heartCount   = 12;
    [SerializeField] private int  starCount    = 6;
    [SerializeField] private int  rainbowCount = 3;
    [SerializeField] private bool autoStartExploration = true;

    private void Start()
    {
        // Create sticker types
        var heart   = Make("Heart",   new Color(1.00f, 0.25f, 0.25f), StickerRarity.Common,   1, 12f, 1.5f, "A red heart sticker.");
        var star    = Make("Star",    new Color(1.00f, 0.85f, 0.10f), StickerRarity.Uncommon, 2, 14f, 2.0f, "A gold star sticker.");
        var rainbow = Make("Rainbow", new Color(0.25f, 0.75f, 1.00f), StickerRarity.Rare,     3, 16f, 2.5f, "A rare rainbow sticker.");

        // Fill StickerBook
        var sb = DEMO_StickerBook.Instance;
        if (sb == null) { Debug.LogError("[Bootstrap] StickerBook not found!"); return; }
        sb.AddSticker(heart,   heartCount);
        sb.AddSticker(star,    starCount);
        sb.AddSticker(rainbow, rainbowCount);
        sb.SelectSticker(heart);

        // Configure FieldObjects
        var rewards = new[] { heart, star, rainbow };
        var fos = FindObjectsByType<DEMO_FieldObject>(FindObjectsSortMode.None);
        for (int i = 0; i < fos.Length; i++)
        {
            fos[i].RuntimeSetup(rewards[i % rewards.Length], count: 2);
            if (fos[i].GetComponent<DEMO_FieldObjectIndicator>() == null)
                fos[i].gameObject.AddComponent<DEMO_FieldObjectIndicator>();
        }

        Debug.Log($"[Bootstrap] Ready — {fos.Length} FieldObjects, {sb.TotalCount} stickers.");

        if (autoStartExploration)
            StartCoroutine(DelayedStart());
    }

    // Wait two frames so every other Start() has definitely run
    private IEnumerator DelayedStart()
    {
        yield return null; // frame 1
        yield return null; // frame 2
        var gm = DEMO_GameManager.Instance;
        if (gm == null) { Debug.LogError("[Bootstrap] GameManager not found!"); yield break; }
        Debug.Log($"[Bootstrap] Starting exploration. Current phase: {gm.CurrentPhase}");
        gm.StartExploration();
        Debug.Log($"[Bootstrap] Phase after start: {gm.CurrentPhase}");
    }

    private static DEMO_StickerData Make(string name, Color color, StickerRarity rarity,
                                    int power, float force, float arc, string desc)
    {
        var d = ScriptableObject.CreateInstance<DEMO_StickerData>();
        d.stickerName = name; d.description = desc;
        d.primaryColor = color; d.rarity = rarity;
        d.stickerPower = power; d.throwForce = force;
        d.arcHeight = arc; d.collisionRadius = 0.22f;
        return d;
    }
}
