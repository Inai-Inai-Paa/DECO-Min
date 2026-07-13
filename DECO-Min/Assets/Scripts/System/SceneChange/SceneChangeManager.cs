using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeManager : MonoBehaviour
{
    public static SceneChangeManager Instance { get; private set; }
    private bool _istransitioning = false;

    private void Awake()
    {
        // シングルトン化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SceneChange(SceneData sceneData)
    {
        if (_istransitioning) return;                   // シーン遷移中
        if (FadeManager.Instance == null) return;       // フェードマネージャーが無い
        if (FadeManager.Instance.isFading) return;      // フェード中             
                                                        // は return

        StartCoroutine(SceneChangeRoutine(sceneData));
    }

    private IEnumerator SceneChangeRoutine(SceneData sceneData)
    {
        _istransitioning = true;

        FadeManager.Instance.FadeOut();
        yield return new WaitForSeconds(1.0f);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneData.sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
    
}
