using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeManager : MonoBehaviour
{
    public static SceneChangeManager Instance { get; private set; }
    private bool _istransitioning = false;

    private void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“‰»
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SceneChange(SceneData sceneData)
    {
        if (_istransitioning) return;
        if (FadeManager.Instance == null) return;
        if (FadeManager.Instance.isFading) return;


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
