using System.Runtime.CompilerServices;
using UnityEngine;

public class MissionClearCheakManager : MonoBehaviour
{
    public MissionClearCheakManager Instance { get; private set; }
    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

}
