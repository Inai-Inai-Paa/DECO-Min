using UnityEngine;
using UnityEditor;

[CreateAssetMenu(menuName = "SceneData")]
public class SceneData : ScriptableObject
{
#if UNITY_EDITOR
    [Header("シーンアセット")]
    [Tooltip("対応するシーンを入れる")]
    [SerializeField] private SceneAsset _sceneAsset;
#endif
    [HideInInspector]
    public string sceneName;



#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_sceneAsset != null)
        {
            sceneName = _sceneAsset.name;
        }
    }
#endif

}
