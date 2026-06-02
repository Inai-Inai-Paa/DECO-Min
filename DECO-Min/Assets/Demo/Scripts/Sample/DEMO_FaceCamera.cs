using UnityEngine;

/// <summary>
/// Makes this GameObject's forward direction always face the main camera.
/// Used by floating 3D text indicators to stay readable from any angle.
/// </summary>
public class DEMO_FaceCamera : MonoBehaviour
{
    private Transform cam;

    private void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main != null ? Camera.main.transform : null;
            return;
        }
        // Mirror camera forward so text reads correctly
        transform.forward = cam.forward;
    }
}
