using UnityEngine;

public class BillboardRotate : MonoBehaviour
{
    Camera mainCamera;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        mainCamera = Camera.main;
	}

    // Update is called once per frame
    void Update()
    {
		// Make the object face the camera
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
            mainCamera.transform.rotation * Vector3.up);
	}
}
