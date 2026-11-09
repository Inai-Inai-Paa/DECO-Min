using UnityEngine;

public class BalloonGimickCol : MonoBehaviour
{
    [SerializeField] private Balloon _balloon;

    private void Start()
    {
        if(_balloon == null)
        {
            Debug.LogError("Balloon reference is not set in BalloonGimickCol.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _balloon.SetOnPlate(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _balloon.SetOnPlate(false);
        }
    }
}
