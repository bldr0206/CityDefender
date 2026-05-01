using UnityEngine;
public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private bool _is180Rotation = true;
    private void Update()
    {
        Transform cameraTransform = Camera.main.transform;
        transform.rotation = Quaternion.LookRotation(cameraTransform.forward, Vector3.up);
        if (_is180Rotation)
            transform.Rotate(0f, 180f, 0f);
    }
}