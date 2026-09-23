using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/CollectData")]
public class CollectData : ScriptableObject
{
    public List<GameObject> Collects;
    public string GroupName;
}
