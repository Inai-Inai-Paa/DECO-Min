using UnityEngine;

[System.Serializable]
public class CharacterStatus : ScriptableObject
{
    public float maxHealth = 100.0f;        // Å‘å‘Ì—Í
    public float currentHealth = 100.0f;    // Œ»İ‚Ì‘Ì—Í

    public float MoveSpeedMultiplier = 1.0f;    // ˆÚ“®—ÍŒW”
    public float AttackPowerMultiplier = 1.0f;  // UŒ‚—ÍŒW”
}
