using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/EnemyData")]
public class EnemyData : ScriptableObject
{
    public List<GameObject> Enemys;
    public string GroupName;
}
