using UnityEngine;

[CreateAssetMenu(menuName = "Mission/MissionTargetObject")]
public class MissionTargetObject : ScriptableObject
{
    [SerializeField]
    public GameObject targetObject;
    public string targetName;
}
