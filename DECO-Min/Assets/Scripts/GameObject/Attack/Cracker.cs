using UnityEngine;

public class Cracker : MonoBehaviour
{
    [SerializeField] private float _maxTime = 3.0f;
    private float _time;
    void Start()
    {
        
    }

    void Update()
    {
        _time += Time.deltaTime;

        if(_time > _maxTime)
            Destroy(gameObject);
    }
}
