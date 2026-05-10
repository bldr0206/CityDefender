using UnityEngine;
public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private bool _is180Rotation = true;

    private void Update()
    {
        LookAt(Camera.main.transform);
    }

    private void OnDrawGizmos()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        LookAt(mainCamera.transform);
    }

    private void LookAt(Transform cameraTransform)
    {
        transform.rotation = Quaternion.LookRotation(cameraTransform.forward, Vector3.up);
        if (_is180Rotation)
            transform.Rotate(0f, 180f, 0f);
    }
}