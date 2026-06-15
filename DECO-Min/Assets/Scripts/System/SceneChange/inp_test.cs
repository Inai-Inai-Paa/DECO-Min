using UnityEngine;
using UnityEngine.InputSystem;

public class inp_test : MonoBehaviour
{
    [SerializeField] private SceneData sceneData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.enterKey.isPressed)
        {
            SceneChangeManager.Instance.SceneChange(sceneData);
        }
    }
}
